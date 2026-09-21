using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Purview.Aspire.ResourceKit;

static class OptionsEnvironmentHelper
{
	static readonly HashSet<Type> LeafTypes =
	[
		typeof(string),
		typeof(bool),
		typeof(byte),
		typeof(sbyte),
		typeof(short),
		typeof(ushort),
		typeof(int),
		typeof(uint),
		typeof(long),
		typeof(ulong),
		typeof(float),
		typeof(double),
		typeof(decimal),
		typeof(char),
		typeof(Guid),
		typeof(DateTime),
		typeof(DateOnly),
		typeof(TimeOnly),
		typeof(TimeSpan),
		typeof(DateTimeOffset),
		typeof(Uri),
	];

	internal static IReadOnlyDictionary<string, string> BuildEnvironmentVariables<TOptions>(
		TOptions options,
		string? sectionNameOverride = null,
		IEnumerable<Action<TOptions>>? overrides = null,
		IEnumerable<Expression<Func<TOptions, object?>>>? ignores = null
	)
	{
		ArgumentNullException.ThrowIfNull(options);

		var sectionName = string.IsNullOrWhiteSpace(sectionNameOverride)
			? OptionsHelper.SectionNameFor<TOptions>()
			: sectionNameOverride;

		Dictionary<string, string> values = [];
		Flatten(options, sectionName, values, [new ReferenceComparer()]);

		if (overrides is not null)
		{
			foreach (var assignment in overrides)
			{
				ArgumentNullException.ThrowIfNull(assignment);
				var overrideValues = OptionsHelper.Assign(sectionName, assignment).AsEnvironmentVariables().Build();
				foreach (var (key, value) in overrideValues)
					values[key] = value;
			}
		}

		if (ignores is not null)
		{
			foreach (var selector in ignores)
			{
				ArgumentNullException.ThrowIfNull(selector);
				var path = $"{sectionName}__{OptionsHelper.PathFor(selector).Replace(".", "__", StringComparison.Ordinal)}";
				RemovePath(values, path);
			}
		}

		return values;
	}

	static void Flatten(object? value, string path, Dictionary<string, string> values, HashSet<object> stack)
	{
		if (value is null)
		{
			values[ToEnvironmentKey(path)] = string.Empty;
			return;
		}

		var type = value.GetType();
		if (IsLeaf(type))
		{
			values[ToEnvironmentKey(path)] = ToInvariantString(value);
			return;
		}

		if (!stack.Add(value))
			return;

		try
		{
			if (value is IDictionary dictionary)
			{
				foreach (DictionaryEntry entry in dictionary)
					Flatten(entry.Value, Combine(path, Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty), values, stack);

				return;
			}

			if (value is IEnumerable enumerable)
			{
				var index = 0;
				foreach (var item in enumerable)
				{
					Flatten(item, Combine(path, index.ToString(CultureInfo.InvariantCulture)), values, stack);
					index++;
				}

				return;
			}

			foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (!property.CanRead || property.GetIndexParameters().Length != 0)
					continue;

				Flatten(property.GetValue(value), Combine(path, property.Name), values, stack);
			}
		}
		finally
		{
			stack.Remove(value);
		}
	}

	static void RemovePath(Dictionary<string, string> values, string prefix)
	{
		var keys = values.Keys.Where(key => key == prefix || key.StartsWith(prefix + "__", StringComparison.Ordinal)).ToArray();
		foreach (var key in keys)
			values.Remove(key);
	}

	static bool IsLeaf(Type type)
	{
		var underlying = Nullable.GetUnderlyingType(type);
		if (underlying is not null)
			type = underlying;

		return LeafTypes.Contains(type) || type.IsEnum;
	}

	static string ToInvariantString(object value)
	{
		return value switch
		{
			null => string.Empty,
			bool boolValue => boolValue ? "true" : "false",
			DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
			DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
			DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
			TimeOnly timeOnly => timeOnly.ToString("O", CultureInfo.InvariantCulture),
			TimeSpan timeSpan => timeSpan.ToString(),
			Uri uri => uri.ToString(),
			_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
		};
	}

	static string Combine(string prefix, string segment) => string.IsNullOrEmpty(prefix) ? segment : $"{prefix}:{segment}";

	static string ToEnvironmentKey(string path) => path.Replace(":", "__", StringComparison.Ordinal);

	sealed class ReferenceComparer : IEqualityComparer<object>
	{
		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

		public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
	}
}

sealed class OptionsEnvironmentBuilder<TOptions> : IOptionsEnvironmentBuilder<TOptions>
{
	readonly List<Action<TOptions>> _overrides = [];
	readonly List<Expression<Func<TOptions, object?>>> _ignores = [];
	readonly TOptions _options;
	readonly string? _sectionNameOverride;

	public OptionsEnvironmentBuilder(TOptions options, string? sectionNameOverride = null)
	{
		_options = options;
		_sectionNameOverride = sectionNameOverride;
	}

	public IOptionsEnvironmentBuilder<TOptions> Override(params Action<TOptions>[] assignments)
	{
		ArgumentNullException.ThrowIfNull(assignments);
		if (assignments.Length == 0)
			throw new ArgumentException("At least one assignment action is required.", nameof(assignments));

		_overrides.AddRange(assignments);
		return this;
	}

	public IOptionsEnvironmentBuilder<TOptions> Ignore(params Expression<Func<TOptions, object?>>[] selectors)
	{
		ArgumentNullException.ThrowIfNull(selectors);
		if (selectors.Length == 0)
			throw new ArgumentException("At least one selector is required.", nameof(selectors));

		_ignores.AddRange(selectors);
		return this;
	}

	public IReadOnlyDictionary<string, string> Build()
	{
		return OptionsEnvironmentHelper.BuildEnvironmentVariables(
			_options,
			_sectionNameOverride,
			_overrides,
			_ignores
		);
	}
}
