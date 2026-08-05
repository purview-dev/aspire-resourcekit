using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Builds command-line configuration arguments for options objects by using assignment expressions.
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
	/// Builds command-line arguments for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="assignments">One or more property assignment actions.</param>
	/// <returns>The generated command-line arguments.</returns>
	public static string[] ForSet<TOptions>(params Action<TOptions>[] assignments)
	{
		ArgumentNullException.ThrowIfNull(assignments);

		return assignments.Length == 0
			? throw new ArgumentException("At least one assignment action is required.", nameof(assignments))
			: BuildFromActions(sectionNameOverride: null, assignments);
	}

	/// <summary>
	/// Builds command-line arguments for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="assignments">One or more property assignment actions.</param>
	/// <returns>The generated command-line arguments.</returns>
	public static string[] ForSet<TOptions>(string sectionName, params Action<TOptions>[] assignments)
	{
		ArgumentNullException.ThrowIfNull(assignments);

		return assignments.Length == 0
			? throw new ArgumentException("At least one assignment action is required.", nameof(assignments))
			: BuildFromActions(sectionName, assignments);
	}

	/// <summary>
	/// Builds command-line arguments for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="assignment">A property assignment expression.</param>
	/// <param name="assignmentExpression">Captured source text for <paramref name="assignment"/>.</param>
	/// <returns>The generated command-line arguments.</returns>
	public static string[] ForSet<TOptions>(
		Action<TOptions> assignment,
		[CallerArgumentExpression(nameof(assignment))] string assignmentExpression = ""
	)
	{
		ArgumentNullException.ThrowIfNull(assignment);

		return ForSetInternal(sectionNameOverride: null, [(assignment, assignmentExpression)]);
	}

	/// <summary>
	/// Builds command-line argument for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="assignment">A property assignment expression.</param>
	/// <param name="assignmentExpression">Captured source text for <paramref name="assignment"/>.</param>
	/// <returns>The generated command-line argument.</returns>
	public static string ForSetOne<TOptions>(
		Action<TOptions> assignment,
		[CallerArgumentExpression(nameof(assignment))] string assignmentExpression = ""
	) => ForSet(assignment, assignmentExpression)[0];

	/// <summary>
	/// Builds command-line argument for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="assignment">A property assignment expression.</param>
	/// <param name="assignmentExpression">Captured source text for <paramref name="assignment"/>.</param>
	/// <returns>The generated command-line argument.</returns>
	public static string ForSetOne<TOptions>(
		string sectionName,
		Action<TOptions> assignment,
		[CallerArgumentExpression(nameof(assignment))] string assignmentExpression = ""
	) => ForSet(sectionName, assignment, assignmentExpression)[0];

	/// <summary>
	/// Builds command-line argument for the specified options type using a member selector expression.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="selector">A member selector expression.</param>
	/// <returns>The generated command-line argument.</returns>
	public static string ForOne<TOptions>(Expression<Func<TOptions, object?>> selector)
	{
		ArgumentNullException.ThrowIfNull(selector);

		return BuildArgumentFromSelector(selector, ResolveSectionName<TOptions>(sectionNameOverride: null));
	}

	/// <summary>
	/// Builds command-line argument for the specified options type using a member selector expression with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="selector">A member selector expression.</param>
	/// <returns>The generated command-line argument.</returns>
	public static string ForOne<TOptions>(string sectionName, Expression<Func<TOptions, object?>> selector)
	{
		ArgumentNullException.ThrowIfNull(selector);

		return BuildArgumentFromSelector(selector, ResolveSectionName<TOptions>(sectionName));
	}

	/// <summary>
	/// Builds command-line arguments for the specified options type using member selector expressions.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="selector">The first member selector expression.</param>
	/// <param name="selectors">Additional member selector expressions.</param>
	/// <returns>The generated command-line arguments.</returns>
	public static string[] For<TOptions>(
		Expression<Func<TOptions, object?>> selector,
		params Expression<Func<TOptions, object?>>[] selectors
	)
	{
		ArgumentNullException.ThrowIfNull(selector);
		ArgumentNullException.ThrowIfNull(selectors);

		var sectionName = ResolveSectionName<TOptions>(sectionNameOverride: null);
		var args = new string[selectors.Length + 1];

		args[0] = BuildArgumentFromSelector(selector, sectionName);
		for (var i = 0; i < selectors.Length; i++)
		{
			ArgumentNullException.ThrowIfNull(selectors[i]);
			args[i + 1] = BuildArgumentFromSelector(selectors[i], sectionName);
		}

		return args;
	}

	/// <summary>
	/// Builds command-line arguments for the specified options type using member selector expressions with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="selector">The first member selector expression.</param>
	/// <param name="selectors">Additional member selector expressions.</param>
	/// <returns>The generated command-line arguments.</returns>
	public static string[] For<TOptions>(
		string sectionName,
		Expression<Func<TOptions, object?>> selector,
		params Expression<Func<TOptions, object?>>[] selectors
	)
	{
		ArgumentNullException.ThrowIfNull(selector);
		ArgumentNullException.ThrowIfNull(selectors);

		var resolvedSectionName = ResolveSectionName<TOptions>(sectionName);
		var args = new string[selectors.Length + 1];

		args[0] = BuildArgumentFromSelector(selector, resolvedSectionName);
		for (var i = 0; i < selectors.Length; i++)
		{
			ArgumentNullException.ThrowIfNull(selectors[i]);
			args[i + 1] = BuildArgumentFromSelector(selectors[i], resolvedSectionName);
		}

		return args;
	}

	/// <summary>
	/// Builds command-line arguments for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="assignment">A property assignment expression.</param>
	/// <param name="assignmentExpression">Captured source text for <paramref name="assignment"/>.</param>
	/// <returns>The generated command-line arguments.</returns>
	public static string[] ForSet<TOptions>(
		string sectionName,
		Action<TOptions> assignment,
		[CallerArgumentExpression(nameof(assignment))] string assignmentExpression = ""
	)
	{
		ArgumentNullException.ThrowIfNull(assignment);

		return ForSetInternal(sectionNameOverride: sectionName, [(assignment, assignmentExpression)]);
	}

	/// <summary>
	/// Builds command-line arguments for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="assignment1">The first property assignment expression.</param>
	/// <param name="assignment2">The second property assignment expression.</param>
	/// <param name="assignmentExpression1">Captured source text for <paramref name="assignment1"/>.</param>
	/// <param name="assignmentExpression2">Captured source text for <paramref name="assignment2"/>.</param>
	/// <returns>The generated command-line arguments.</returns>
	public static string[] ForSet<TOptions>(
		Action<TOptions> assignment1,
		Action<TOptions> assignment2,
		[CallerArgumentExpression(nameof(assignment1))] string assignmentExpression1 = "",
		[CallerArgumentExpression(nameof(assignment2))] string assignmentExpression2 = ""
	)
	{
		ArgumentNullException.ThrowIfNull(assignment1);
		ArgumentNullException.ThrowIfNull(assignment2);

		return ForSetInternal(
			sectionNameOverride: null,
			[(assignment1, assignmentExpression1), (assignment2, assignmentExpression2)]
		);
	}

	/// <summary>
	/// Builds command-line arguments for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="assignment1">The first property assignment expression.</param>
	/// <param name="assignment2">The second property assignment expression.</param>
	/// <param name="assignmentExpression1">Captured source text for <paramref name="assignment1"/>.</param>
	/// <param name="assignmentExpression2">Captured source text for <paramref name="assignment2"/>.</param>
	/// <returns>The generated command-line arguments.</returns>
	public static string[] ForSet<TOptions>(
		string sectionName,
		Action<TOptions> assignment1,
		Action<TOptions> assignment2,
		[CallerArgumentExpression(nameof(assignment1))] string assignmentExpression1 = "",
		[CallerArgumentExpression(nameof(assignment2))] string assignmentExpression2 = ""
	)
	{
		ArgumentNullException.ThrowIfNull(assignment1);
		ArgumentNullException.ThrowIfNull(assignment2);

		return ForSetInternal(
			sectionNameOverride: sectionName,
			[(assignment1, assignmentExpression1), (assignment2, assignmentExpression2)]
		);
	}

	static string[] ForSetInternal<TOptions>(
		string? sectionNameOverride,
		(IReadOnlyList<Action<TOptions>> Actions, IReadOnlyList<string> Expressions) assignments
	)
	{
		ArgumentNullException.ThrowIfNull(assignments.Actions);
		ArgumentNullException.ThrowIfNull(assignments.Expressions);

		if (assignments.Actions.Count == 0)
			throw new ArgumentException("At least one assignment expression is required.", nameof(assignments));

		if (assignments.Actions.Count != assignments.Expressions.Count)
			throw new ArgumentException("Assignments and expressions count must match.", nameof(assignments));

		var resolvedSectionName = ResolveSectionName<TOptions>(sectionNameOverride);
		var args = new string[assignments.Actions.Count];

		for (var i = 0; i < assignments.Actions.Count; i++)
			args[i] = BuildArgument(assignments.Actions[i], assignments.Expressions[i], resolvedSectionName);

		return args;
	}

	static string[] ForSetInternal<TOptions>(
		string? sectionNameOverride,
		params (Action<TOptions> Action, string Expression)[] assignments
	)
	{
		ArgumentNullException.ThrowIfNull(assignments);

		var actions = assignments.Select(x => x.Action).ToArray();
		var expressions = assignments.Select(x => x.Expression).ToArray();

		return ForSetInternal(sectionNameOverride, (actions, expressions));
	}

	static string BuildArgument<TOptions>(Action<TOptions> assignment, string assignmentExpression, string sectionName)
	{
		ArgumentNullException.ThrowIfNull(assignment);

		var keyPath = GetMemberPath(assignmentExpression);
		var root = CreateRootOptionsInstance<TOptions>();
		ArgumentNullException.ThrowIfNull(root);

		EnsurePathObjectsExist(root, keyPath);
		assignment(root);

		var value = GetPathValue(root, keyPath);
		var valueText = ToCommandLineValue(value);

		return $"--{sectionName}:{keyPath}={valueText}";
	}

	static string BuildArgumentFromSelector<TOptions>(Expression<Func<TOptions, object?>> selector, string sectionName)
	{
		var keyPath = GetMemberPath(selector);
		var root = CreateRootOptionsInstance<TOptions>();
		ArgumentNullException.ThrowIfNull(root);

		EnsurePathObjectsExist(root, keyPath);

		var value = selector.Compile()(root);
		var valueText = ToCommandLineValue(value);

		return $"--{sectionName}:{keyPath}={valueText}";
	}

	static string[] BuildFromActions<TOptions>(string? sectionNameOverride, Action<TOptions>[] assignments)
	{
		var resolvedSectionName = ResolveSectionName<TOptions>(sectionNameOverride);
		var args = new string[assignments.Length];

		for (var i = 0; i < assignments.Length; i++)
		{
			ArgumentNullException.ThrowIfNull(assignments[i]);
			var (path, value) = InferSingleAssignment(assignments[i]);
			args[i] = $"--{resolvedSectionName}:{path}={ToCommandLineValue(value)}";
		}

		return args;
	}

	static (string Path, object? Value) InferSingleAssignment<TOptions>(Action<TOptions> assignment)
	{
		var a = CreateRootOptionsInstance<TOptions>();
		var b = CreateRootOptionsInstance<TOptions>();

		ArgumentNullException.ThrowIfNull(a);
		ArgumentNullException.ThrowIfNull(b);

		PopulateSentinels(a!, seed: 1, [with(ReferenceComparer.Instance)]);
		PopulateSentinels(b!, seed: 2, [with(ReferenceComparer.Instance)]);

		assignment(a);
		assignment(b);

		var candidates = new List<(string Path, object? Value)>();
		CollectEqualLeafPaths(a!, b!, string.Empty, candidates, [with(ReferenceComparer.Instance)]);

		return candidates.Count != 1
			? throw new InvalidOperationException(
				$"Each assignment action must set exactly one property path. Found {candidates.Count} candidate paths."
			)
			: candidates[0];
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
			if (!property.CanRead)
				continue;

			var propertyType = property.PropertyType;
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
			if (!property.CanRead)
				continue;

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

	static bool TryGetScalarSentinel(Type type, int seed, out object? value)
	{
		var underlying = Nullable.GetUnderlyingType(type);
		if (underlying is not null)
		{
			var hasValue = seed % 2 == 0;
			if (!hasValue)
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
			value = new TimeOnly((seed * 3) % 23, (seed * 7) % 59, (seed * 11) % 59);
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

		if (type.IsEnum)
		{
			var values = Enum.GetValues(type);
			if (values.Length == 0)
			{
				value = Activator.CreateInstance(type);
				return true;
			}

			value = values.GetValue(seed % values.Length);
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

		// Success!
		return true;
	}

	static string ResolveSectionName<TOptions>(string? sectionNameOverride)
	{
		if (!string.IsNullOrWhiteSpace(sectionNameOverride))
			return sectionNameOverride!;

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

	static string GetMemberPath(string assignmentExpression)
	{
		if (string.IsNullOrWhiteSpace(assignmentExpression))
			throw new ArgumentException(
				"Assignment expression text could not be captured.",
				nameof(assignmentExpression)
			);

		var arrowIndex = assignmentExpression.IndexOf("=>", StringComparison.Ordinal);
		if (arrowIndex < 0)
			throw new ArgumentException(
				$"Expression '{assignmentExpression}' must be a lambda assignment expression.",
				nameof(assignmentExpression)
			);

		var rhs = assignmentExpression[(arrowIndex + 2)..].Trim();
		if (rhs.StartsWith('{'))
		{
			var statementEnd = rhs.IndexOf(';', StringComparison.Ordinal);
			if (statementEnd > 0)
				rhs = rhs[1..statementEnd].Trim();
		}

		var assignIndex = rhs.IndexOf('=', StringComparison.Ordinal);
		if (assignIndex < 0)
			throw new ArgumentException(
				$"Expression '{assignmentExpression}' must contain an assignment operator.",
				nameof(assignmentExpression)
			);

		var left = rhs[..assignIndex].Trim();
		var firstDot = left.IndexOf('.', StringComparison.Ordinal);
		if (firstDot < 0 || firstDot == left.Length - 1)
			throw new ArgumentException(
				$"Expression '{assignmentExpression}' must assign a member path on the options parameter.",
				nameof(assignmentExpression)
			);

		var path = left[(firstDot + 1)..].Replace("!", string.Empty, StringComparison.Ordinal).Trim();
		return string.IsNullOrWhiteSpace(path)
			? throw new ArgumentException(
				$"Expression '{assignmentExpression}' does not contain a valid member path.",
				nameof(assignmentExpression)
			)
			: path.Replace('.', ':');
	}

	static string GetMemberPath<TOptions>(Expression<Func<TOptions, object?>> selector)
	{
		ArgumentNullException.ThrowIfNull(selector);

		var body = selector.Body;

		while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
			body = unary.Operand;

		var segments = new List<string>();
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
		return string.Join(':', segments);
	}

	static void EnsurePathObjectsExist(object root, string keyPath)
	{
		var segments = keyPath.Split(':', StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length < 2)
			return;

		var current = root;
		for (var i = 0; i < segments.Length - 1; i++)
		{
			var property =
				current
					.GetType()
					.GetProperty(segments[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				?? throw new InvalidOperationException(
					$"Property '{segments[i]}' was not found on '{current.GetType().FullName}'."
				);

			var value = property.GetValue(current);
			if (value is null)
			{
				var instance = CreateInstance(property.PropertyType);
				if (property.SetMethod is null)
					throw new InvalidOperationException(
						$"Property '{property.Name}' on '{current.GetType().FullName}' is null and does not have a setter."
					);

				property.SetValue(current, instance);
				value = instance;
			}

			current = value;
		}
	}

	static object? GetPathValue(object root, string keyPath)
	{
		var segments = keyPath.Split(':', StringSplitOptions.RemoveEmptyEntries);
		object? current = root;

		foreach (var segment in segments)
		{
			if (current is null)
				return null;

			var property =
				current
					.GetType()
					.GetProperty(segment, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				?? throw new InvalidOperationException(
					$"Property '{segment}' was not found on '{current.GetType().FullName}'."
				);

			current = property.GetValue(current);
		}

		return current;
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
}
