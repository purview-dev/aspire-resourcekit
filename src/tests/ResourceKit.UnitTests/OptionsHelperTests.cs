namespace Purview.Aspire.ResourceKit;

public sealed class OptionsHelperTests
{
	[Test]
	public async Task For_GivenExplicitSectionName_UsesSectionOverride()
	{
		// Arrange
		const string sectionName = "SectionNameGoesHere";

		// Act
		var args = OptionsHelper.For<HostKitOptions>(
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
		var args = OptionsHelper.For<PrivateSectionOptions>(c => c.Redis.Name = "PIES");

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--PrivateSection:Redis:Name=PIES");
	}

	[Test]
	public async Task For_GivenNoConstSection_RemovesKnownSuffix()
	{
		// Arrange

		// Act
		var fromOptions = OptionsHelper.For<ServiceOptions>(c => c.Redis.Name = "PIES");
		var fromSettings = OptionsHelper.For<ServiceSettings>(c => c.Redis.Name = "PIES");
		var fromConfiguration = OptionsHelper.For<ServiceConfiguration>(c => c.Redis.Name = "PIES");
		var fromConfig = OptionsHelper.For<ServiceConfig>(c => c.Redis.Name = "PIES");

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
		var args = OptionsHelper.For<Options>(c => c.Redis.Name = "PIES");

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--Options:Redis:Name=PIES");
	}

	[Test]
	public async Task For_GivenDeepAssignment_ProducesColonSeparatedInfiniteDepthPath()
	{
		// Arrange

		// Act
		var args = OptionsHelper.For<DeepOptions>(c => c.Level1.Level2.Level3.Level4.Name = "PIES");

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--Deep:Level1:Level2:Level3:Level4:Name=PIES");
	}

	[Test]
	public async Task For_GivenThreeAssignments_UsingParams_ProducesThreeArgs()
	{
		// Arrange

		// Act
		var args = OptionsHelper.For<HostKitOptions>(
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
		var args = OptionsHelper.For("CustomSection", assignments);

		// Assert
		await Assert.That(args.Length).IsEqualTo(3);
		await Assert.That(args[0]).IsEqualTo("--CustomSection:Redis:IsEnabled=false");
		await Assert.That(args[1]).IsEqualTo("--CustomSection:Redis:Name=PIES");
		await Assert.That(args[2]).IsEqualTo("--CustomSection:Api:Name=my-api");
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
}
