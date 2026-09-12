using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Builds configuration arguments or environment variables for options objects by using assignment expressions.
/// </summary>
public static class OptionsHelper
{
	static readonly string[] SectionNameSuffixes = ["Options", "Settings", "Configuration", "Config"];

	sealed class ReferenceComparer : IEqualityComparer<object>
	{
		public static readonly ReferenceComparer Instance = new();

		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

		public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
	}

	/// <summary>
	/// Starts building entries from assignment expressions for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="assignments">One or more property assignment actions.</param>
	/// <returns>A builder that can be extended or built.</returns>
	public static IOptionsBuilder Assign<TOptions>(params Action<TOptions>[] assignments)
	{
		ArgumentNullException.ThrowIfNull(assignments);

		return new OptionsBuilder().Assign(assignments);
	}

	/// <summary>
	/// Starts building entries from assignment expressions for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="assignments">One or more property assignment actions.</param>
	/// <returns>A builder that can be extended or built.</returns>
	public static IOptionsBuilder Assign<TOptions>(string sectionName, params Action<TOptions>[] assignments)
	{
		ArgumentNullException.ThrowIfNull(assignments);

		return new OptionsBuilder().Assign(sectionName, assignments);
	}

	/// <summary>
	/// Gets the dot-separated member path for a property selector on the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="selector">A member selector expression.</param>
	/// <returns>The dot-separated member path (for example, <c>Level1.Level2.Name</c>).</returns>
	public static string PathFor<TOptions>(Expression<Func<TOptions, object?>> selector)
	{
		ArgumentNullException.ThrowIfNull(selector);

		return GetMemberPath(selector);
	}

	static string ResolveSectionName<TOptions>(string? sectionNameOverride)
	{
		if (!string.IsNullOrWhiteSpace(sectionNameOverride))
			return sectionNameOverride;

		var sectionNameField = typeof(TOptions).GetField(
			"SectionName",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy
		);

		if (
			sectionNameField is { FieldType: not null }
			&& sectionNameField.FieldType == typeof(string)
			&& sectionNameField.IsLiteral
			&& !sectionNameField.IsInitOnly
			&& sectionNameField.GetRawConstantValue() is string constantValue
			&& !string.IsNullOrWhiteSpace(constantValue)
		)
			return constantValue;

		var typeName = typeof(TOptions).Name;
		foreach (var suffix in SectionNameSuffixes)
		{
			if (!typeName.EndsWith(suffix, StringComparison.Ordinal))
				continue;

			var trimmed = typeName[..^suffix.Length];
			return string.IsNullOrEmpty(trimmed) ? typeName : trimmed;
		}

		return typeName;
	}

	static TOptions CreateRootOptionsInstance<TOptions>()
	{
		var type = typeof(TOptions);

		object? instance;
		try
		{
			instance = Activator.CreateInstance(type, nonPublic: true);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(
				$"Unable to create an instance of '{type.FullName}' for options assignment evaluation.",
				ex
			);
		}

		return instance is null
			? throw new InvalidOperationException($"Unable to create an instance of '{type.FullName}'.")
			: (TOptions)instance;
	}

	static string GetMemberPath<TOptions>(Expression<Func<TOptions, object?>> selector)
	{
		ArgumentNullException.ThrowIfNull(selector);

		var body = selector.Body;

#pragma warning disable format
		while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
			body = unary.Operand;
#pragma warning restore format

		List<string> segments = [];
		while (body is MemberExpression member)
		{
			segments.Add(member.Member.Name);
			body = member.Expression;

			if (body is ParameterExpression parameter)
			{
				if (parameter != selector.Parameters[0])
					throw new ArgumentException(
						"Selector must be a simple member access expression on the options parameter.",
						nameof(selector)
					);

				break;
			}
		}

		if (segments.Count == 0 || body is not ParameterExpression)
			throw new ArgumentException(
				"Selector must be a simple member access expression on the options parameter.",
				nameof(selector)
			);

		segments.Reverse();
		return string.Join('.', segments);
	}

	static object CreateInstance(Type type)
	{
		return type.IsValueType
			? Activator.CreateInstance(type)!
			: Activator.CreateInstance(type, nonPublic: true)
				?? throw new InvalidOperationException($"Unable to create an instance of '{type.FullName}'.");
	}

	static string ToCommandLineValue(object? value)
	{
		return value switch
		{
			null => string.Empty,
			_ => value switch
			{
				bool boolValue => boolValue ? "true" : "false",
				_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
			},
		};
	}

	static bool TryGetScalarSentinel(Type type, int seed, out object? value)
	{
		var underlying = Nullable.GetUnderlyingType(type);
		if (underlying is not null)
		{
			if (seed % 2 != 0)
			{
				value = null;
				return true;
			}

			if (!TryGetScalarSentinel(underlying, seed + 11, out var inner))
			{
				value = null;
				return false;
			}

			value = inner;
			return true;
		}

		if (type == typeof(string))
		{
			value = $"__rk_sentinel_{seed}__";
			return true;
		}

		if (type == typeof(bool))
		{
			value = seed % 2 == 0;
			return true;
		}

		if (type == typeof(char))
		{
			value = (char)('A' + (seed % 26));
			return true;
		}

		if (type.IsEnum)
		{
			var values = Enum.GetValues(type);
			value = values.Length == 0 ? Activator.CreateInstance(type) : values.GetValue(seed % values.Length);
			return true;
		}

		if (TryGetNumericSentinel(type, seed, out value))
			return true;

		if (TryGetDateTimeSentinel(type, seed, out value))
			return true;

		value = null;
		return false;
	}

	static bool TryGetNumericSentinel(Type type, int seed, out object? value)
	{
		if (type == typeof(byte))
		{
			value = (byte)(seed % 255);
			return true;
		}

		if (type == typeof(sbyte))
		{
			value = (sbyte)(seed % 120);
			return true;
		}

		if (type == typeof(short))
		{
			value = (short)(seed * 17);
			return true;
		}

		if (type == typeof(ushort))
		{
			value = (ushort)(seed * 19);
			return true;
		}

		if (type == typeof(int))
		{
			value = seed * 7919;
			return true;
		}

		if (type == typeof(uint))
		{
			value = (uint)(seed * 7907);
			return true;
		}

		if (type == typeof(long))
		{
			value = (long)seed * 104729;
			return true;
		}

		if (type == typeof(ulong))
		{
			value = (ulong)seed * 130363;
			return true;
		}

		if (type == typeof(float))
		{
			value = seed + 0.125f;
			return true;
		}

		if (type == typeof(double))
		{
			value = seed + 0.625d;
			return true;
		}

		if (type == typeof(decimal))
		{
			value = seed + 0.875m;
			return true;
		}

		value = null;
		return false;
	}

	static bool TryGetDateTimeSentinel(Type type, int seed, out object? value)
	{
		if (type == typeof(Guid))
		{
			Span<byte> bytes = stackalloc byte[16];
			bytes.Fill((byte)(seed % 256));
			value = new Guid(bytes);
			return true;
		}

		if (type == typeof(DateTime))
		{
			value = new DateTime(2000, 1, 1).AddDays(seed);
			return true;
		}

		if (type == typeof(DateOnly))
		{
			value = new DateOnly(2000, 1, 1).AddDays(seed);
			return true;
		}

		if (type == typeof(TimeOnly))
		{
			value = new TimeOnly(seed * 3 % 23, seed * 7 % 59, seed * 11 % 59);
			return true;
		}

		if (type == typeof(TimeSpan))
		{
			value = TimeSpan.FromMinutes(seed * 7);
			return true;
		}

		if (type == typeof(DateTimeOffset))
		{
			value = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(seed);
			return true;
		}

		value = null;
		return false;
	}

	static bool CanInstantiate(Type type)
	{
		if (type == typeof(string))
			return false;

		if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
			return false;

		if (type.IsAbstract || type.IsInterface)
			return false;

		if (type.IsPrimitive || type.IsEnum)
			return false;

		if (type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan))
			return false;

		// If we get to here then we can assume it's a class or struct that we can instantiate (or at least try to).
		return true;
	}

	static void PopulateSentinels(object root, int seed, HashSet<object> visited)
	{
		if (!visited.Add(root))
			return;

		foreach (
			var property in root.GetType()
				.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		)
		{
			if (!property.CanRead || property.GetIndexParameters().Length != 0)
				continue;

			var propertyType = property.PropertyType;
			if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
				continue;

			if (TryGetScalarSentinel(propertyType, seed, out var scalarValue))
			{
				if (property.SetMethod is not null)
					property.SetValue(root, scalarValue);

				continue;
			}

			if (propertyType == typeof(string) || propertyType == typeof(object))
				continue;

			if (!CanInstantiate(propertyType))
				continue;

			var value = property.GetValue(root);
			if (value is null)
			{
				if (property.SetMethod is null)
					continue;

				value = CreateInstance(propertyType);
				property.SetValue(root, value);
			}

			PopulateSentinels(value, seed + 1, visited);
		}
	}

	static void CollectEqualLeafPaths(
		object? a,
		object? b,
		string prefix,
		List<(string Path, object? Value)> candidates,
		HashSet<object> visitedA
	)
	{
		if (a is null || b is null)
			return;

		if (!visitedA.Add(a))
			return;

		foreach (
			var property in a.GetType()
				.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		)
		{
			if (!property.CanRead || property.GetIndexParameters().Length != 0)
				continue;

			var propertyType = property.PropertyType;

			// Collections are not ordinary configuration-object branches.
			if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
			{
				continue;
			}

			var segment = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
			var av = property.GetValue(a);
			var bv = property.GetValue(b);

			if (TryGetScalarSentinel(property.PropertyType, 1, out _))
			{
				if (Equals(av, bv))
					candidates.Add((segment, av));

				continue;
			}

			if (property.PropertyType == typeof(string) || property.PropertyType == typeof(object))
			{
				if (Equals(av, bv))
					candidates.Add((segment, av));

				continue;
			}

			if (av is not null && bv is not null)
				CollectEqualLeafPaths(av, bv, segment, candidates, visitedA);
		}
	}

	static (string Path, object? Value) InferSingleAssignment<TOptions>(Action<TOptions> assignment)
	{
		var a = CreateRootOptionsInstance<TOptions>();
		var b = CreateRootOptionsInstance<TOptions>();

		ArgumentNullException.ThrowIfNull(a);
		ArgumentNullException.ThrowIfNull(b);

		PopulateSentinels(a, seed: 1, [with(ReferenceComparer.Instance)]);
		PopulateSentinels(b, seed: 2, [with(ReferenceComparer.Instance)]);

		assignment(a);
		assignment(b);

		List<(string Path, object? Value)> candidates = [];
		CollectEqualLeafPaths(a, b, string.Empty, candidates, [with(ReferenceComparer.Instance)]);

		return candidates.Count switch
		{
			0 => throw new InvalidOperationException(
				"The assignment action did not modify any detectable property path. Assign a value that differs from the property's current value, or verify the property has a setter."
			),
			1 => candidates[0],
			_ => throw new InvalidOperationException(
				$"Each assignment action must set exactly one property path. Found {candidates.Count} candidate paths: {string.Join(", ", candidates.Select(static c => c.Path))}. Split each property into its own assignment, for example Assign<TOptions>(o => o.Path1 = ..., o => o.Path2 = ...)."
			),
		};
	}

	static OptionsEntry[] BuildEntriesFromActions<TOptions>(string? sectionNameOverride, Action<TOptions>[] assignments)
	{
		var resolvedSectionName = ResolveSectionName<TOptions>(sectionNameOverride);
		var entries = new OptionsEntry[assignments.Length];

		for (var i = 0; i < assignments.Length; i++)
		{
			ArgumentNullException.ThrowIfNull(assignments[i]);
			var (path, value) = InferSingleAssignment(assignments[i]);
			entries[i] = new OptionsEntry(resolvedSectionName, path, ToCommandLineValue(value));
		}

		return entries;
	}

	sealed record OptionsEntry(string SectionName, string KeyPath, string? Value);

	sealed class OptionsBuilder : IOptionsBuilder
	{
		readonly List<OptionsEntry> _entries = [];

		public IOptionsBuilder Assign<TOptions>(params Action<TOptions>[] assignments)
		{
			ArgumentNullException.ThrowIfNull(assignments);

			if (assignments.Length == 0)
				throw new ArgumentException("At least one assignment action is required.", nameof(assignments));

			_entries.AddRange(BuildEntriesFromActions(sectionNameOverride: null, assignments));
			return this;
		}

		public IOptionsBuilder Assign<TOptions>(string sectionName, params Action<TOptions>[] assignments)
		{
			ArgumentNullException.ThrowIfNull(assignments);

			if (assignments.Length == 0)
				throw new ArgumentException("At least one assignment action is required.", nameof(assignments));

			_entries.AddRange(BuildEntriesFromActions(sectionName, assignments));
			return this;
		}

		public string[] Build()
		{
			var args = new string[_entries.Count];
			for (var i = 0; i < _entries.Count; i++)
			{
				var entry = _entries[i];
				args[i] = $"--{entry.SectionName}:{entry.KeyPath}={entry.Value ?? string.Empty}";
			}

			return args;
		}

		public IEnvironmentVariablesBuilder AsEnvironmentVariables() => new EnvironmentVariablesBuilder([.. _entries]);
	}

	sealed class EnvironmentVariablesBuilder(List<OptionsEntry> entries) : IEnvironmentVariablesBuilder
	{
		public IReadOnlyDictionary<string, string> Build()
		{
			Dictionary<string, string> result = [];
			foreach (var entry in entries)
			{
				var key = $"{entry.SectionName}__{entry.KeyPath}".Replace(":", "__", StringComparison.Ordinal);
				result[key] = entry.Value ?? string.Empty;
			}

			return result;
		}
	}
}
