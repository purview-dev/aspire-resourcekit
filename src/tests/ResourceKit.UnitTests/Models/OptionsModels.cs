namespace Purview.Aspire.ResourceKit.Models;

public sealed class AContainerForNestedClasses
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	public sealed class TestOptionsSettings
	{
		public const string SectionName = "TestOptions";

		public bool EnableFeatureA { get; set; } = true;

		public bool EnableFeatureB { get; set; }

		public bool EnableFeatureC { get; set; }

		public AContainerForNestedMoreClasses.MoreTestOptions MoreOptions { get; set; } = new();
	}
}

public sealed class AContainerForNestedMoreClasses
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	public sealed class MoreTestOptions
	{
		public bool EnableFeatureX { get; set; } = true;

		public bool EnableFeatureY { get; set; } = true;

		public bool EnableFeatureZ { get; set; } = true;

		public AContainerForNestedEvenMoreClasses.EvenMoreTestingOptions EvenMore { get; set; } = new();
	}
}

public sealed class AContainerForNestedEvenMoreClasses
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	public sealed class EvenMoreTestingOptions
	{
		public string EndOfTheLine { get; set; } = "This is the end of the line.";
	}
}

sealed class HostKitOptions
{
	public RedisOptions Redis { get; set; } = new();

	public ApiOptions Api { get; set; } = new();
}

sealed class ApiOptions
{
	public string Name { get; set; } = string.Empty;
}

sealed class ServiceOptions
{
	public RedisOptions Redis { get; set; } = new();
}

sealed class ServiceSettings
{
	public RedisOptions Redis { get; set; } = new();
}

sealed class ServiceConfiguration
{
	public RedisOptions Redis { get; set; } = new();
}

sealed class ServiceConfig
{
	public RedisOptions Redis { get; set; } = new();
}

sealed class PrivateSectionOptions
{
#pragma warning disable CA1823 // Avoid unused field warning; this const is intentionally discovered via reflection.
#pragma warning disable IDE0040 // Keep explicit accessibility for clarity in reflection-focused test type.
#pragma warning disable IDE0051 // Remove unused private members
	private const string SectionName = "PrivateSection";
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0040
#pragma warning restore CA1823

	public RedisOptions Redis { get; set; } = new();
}

sealed class Options
{
	public RedisOptions Redis { get; set; } = new();
}

sealed class DeepOptions
{
	public Level1Options Level1 { get; set; } = new();
}

sealed class Level1Options
{
	public Level2Options Level2 { get; set; } = new();
}

sealed class Level2Options
{
	public Level3Options Level3 { get; set; } = new();
}

sealed class Level3Options
{
	public Level4Options Level4 { get; set; } = new();
}

sealed class Level4Options
{
	public string Name { get; set; } = string.Empty;
}

sealed class RedisOptions
{
	public string Name { get; set; } = string.Empty;

	public bool IsEnabled { get; set; }
}

sealed class SampleStoreOptions
{
	public string CurrentKey { get; set; } = "default-key";

	public int Count { get; set; } = 42;

	public NestedSampleOptions Nested { get; set; } = new();
}

sealed class NestedSampleOptions
{
	public string Value { get; set; } = "nested-default";
}
