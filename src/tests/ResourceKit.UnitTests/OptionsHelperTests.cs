namespace Purview.Aspire.ResourceKit;

public sealed class OptionsHelperTests
{
	[Test]
	public async Task For_GivenExplicitSectionName_UsesSectionOverride()
	{
		// Arrange
		const string sectionName = "SectionNameGoesHere";

		// Act
		var args = OptionsHelper.ForSet<HostKitOptions>(
			sectionName,
			c => c.Redis.IsEnabled = false,
			c => c.Redis.Name = "PIES"
		);

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--SectionNameGoesHere:Redis:IsEnabled=false");
		await Assert.That(args[1]).IsEqualTo("--SectionNameGoesHere:Redis:Name=PIES");
	}

	[Test]
	public async Task For_GivenNoSectionOverride_UsesSectionNameConstValue()
	{
		// Arrange

		// Act
		var args = OptionsHelper.ForSet<PrivateSectionOptions>(c => c.Redis.Name = "PIES");

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--PrivateSection:Redis:Name=PIES");
	}

	[Test]
	public async Task For_GivenNoConstSection_RemovesKnownSuffix()
	{
		// Arrange

		// Act
		var fromOptions = OptionsHelper.ForSet<ServiceOptions>(c => c.Redis.Name = "PIES");
		var fromSettings = OptionsHelper.ForSet<ServiceSettings>(c => c.Redis.Name = "PIES");
		var fromConfiguration = OptionsHelper.ForSet<ServiceConfiguration>(c => c.Redis.Name = "PIES");
		var fromConfig = OptionsHelper.ForSet<ServiceConfig>(c => c.Redis.Name = "PIES");

		// Assert
		await Assert.That(fromOptions[0]).IsEqualTo("--Service:Redis:Name=PIES");
		await Assert.That(fromSettings[0]).IsEqualTo("--Service:Redis:Name=PIES");
		await Assert.That(fromConfiguration[0]).IsEqualTo("--Service:Redis:Name=PIES");
		await Assert.That(fromConfig[0]).IsEqualTo("--Service:Redis:Name=PIES");
	}

	[Test]
	public async Task For_GivenTypeNameOnlySuffix_UsesOriginalTypeName()
	{
		// Arrange

		// Act
		var args = OptionsHelper.ForSet<Options>(c => c.Redis.Name = "PIES");

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--Options:Redis:Name=PIES");
	}

	[Test]
	public async Task For_GivenDeepAssignment_ProducesColonSeparatedInfiniteDepthPath()
	{
		// Arrange

		// Act
		var args = OptionsHelper.ForSet<DeepOptions>(c => c.Level1.Level2.Level3.Level4.Name = "PIES");

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--Deep:Level1:Level2:Level3:Level4:Name=PIES");
	}

	[Test]
	public async Task For_GivenThreeAssignments_UsingParams_ProducesThreeArgs()
	{
		// Arrange

		// Act
		var args = OptionsHelper.ForSet<HostKitOptions>(
			c => c.Redis.IsEnabled = false,
			c => c.Redis.Name = "PIES",
			c => c.Api.Name = "my-api"
		);

		// Assert
		await Assert.That(args.Length).IsEqualTo(3);
		await Assert.That(args[0]).IsEqualTo("--HostKit:Redis:IsEnabled=false");
		await Assert.That(args[1]).IsEqualTo("--HostKit:Redis:Name=PIES");
		await Assert.That(args[2]).IsEqualTo("--HostKit:Api:Name=my-api");
	}

	[Test]
	public async Task For_GivenAssignmentsArray_UsesArrayAndSectionOverride()
	{
		// Arrange
		Action<HostKitOptions>[] assignments =
		[
			c => c.Redis.IsEnabled = false,
			c => c.Redis.Name = "PIES",
			c => c.Api.Name = "my-api",
		];

		// Act
		var args = OptionsHelper.ForSet("CustomSection", assignments);

		// Assert
		await Assert.That(args.Length).IsEqualTo(3);
		await Assert.That(args[0]).IsEqualTo("--CustomSection:Redis:IsEnabled=false");
		await Assert.That(args[1]).IsEqualTo("--CustomSection:Redis:Name=PIES");
		await Assert.That(args[2]).IsEqualTo("--CustomSection:Api:Name=my-api");
	}

	[Test]
	public async Task ForOne_GivenSelectorExpression_ReturnsArgumentWithDefaultValue()
	{
		// Arrange

		// Act
		var arg = OptionsHelper.ForOne<SampleStoreOptions>(f => f.CurrentKey);

		// Assert
		await Assert.That(arg).IsEqualTo("--SampleStore:CurrentKey=default-key");
	}

	[Test]
	public async Task ForOne_GivenSelectorExpressionWithValueType_ReturnsArgumentWithDefaultValue()
	{
		// Arrange

		// Act
		var arg = OptionsHelper.ForOne<SampleStoreOptions>(f => f.Count);

		// Assert
		await Assert.That(arg).IsEqualTo("--SampleStore:Count=42");
	}

	[Test]
	public async Task ForOne_GivenSectionNameAndSelectorExpression_ReturnsArgumentWithOverride()
	{
		// Arrange
		const string sectionName = "MySection";

		// Act
		var arg = OptionsHelper.ForOne<SampleStoreOptions>(sectionName, f => f.CurrentKey);

		// Assert
		await Assert.That(arg).IsEqualTo("--MySection:CurrentKey=default-key");
	}

	[Test]
	public async Task ForOne_GivenNestedSelectorExpression_ReturnsArgumentWithNestedDefaultValue()
	{
		// Arrange

		// Act
		var arg = OptionsHelper.ForOne<SampleStoreOptions>(f => f.Nested!.Value);

		// Assert
		await Assert.That(arg).IsEqualTo("--SampleStore:Nested:Value=nested-default");
	}

	[Test]
	public async Task For_GivenSelectorExpressions_ReturnsArgumentsWithDefaultValues()
	{
		// Arrange

		// Act
		var args = OptionsHelper.For<SampleStoreOptions>(f => f.CurrentKey, f => f.Count);

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--SampleStore:CurrentKey=default-key");
		await Assert.That(args[1]).IsEqualTo("--SampleStore:Count=42");
	}

	[Test]
	public async Task For_GivenSectionNameAndSelectorExpressions_ReturnsArgumentsWithOverride()
	{
		// Arrange
		const string sectionName = "MySection";

		// Act
		var args = OptionsHelper.For<SampleStoreOptions>(sectionName, f => f.CurrentKey, f => f.Count);

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--MySection:CurrentKey=default-key");
		await Assert.That(args[1]).IsEqualTo("--MySection:Count=42");
	}

	[Test]
	public async Task For_GivenNestedSelectorExpression_ReturnsArgumentWithNestedDefaultValue()
	{
		// Arrange

		// Act
		var args = OptionsHelper.For<SampleStoreOptions>(f => f.Nested!.Value);

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--SampleStore:Nested:Value=nested-default");
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
		private const string SectionName = "PrivateSection";
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
}
