using Purview.Aspire.ResourceKit.Models;

namespace Purview.Aspire.ResourceKit;

public sealed class OptionsHelperTests
{
	readonly string _aTestingValue = $"This is a test value - {Guid.NewGuid()}";

	[Test]
	public async Task Assign_GivenExplicitSectionName_UsesSectionOverride()
	{
		// Arrange
		const string sectionName = "SectionNameGoesHere";

		// Act
		var args = OptionsHelper
			.Assign<HostKitOptions>(
				sectionName,
				static c => c.Redis.IsEnabled = false,
				static c => c.Redis.Name = "PIES"
			)
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--SectionNameGoesHere:Redis:IsEnabled=false");
		await Assert.That(args[1]).IsEqualTo("--SectionNameGoesHere:Redis:Name=PIES");
	}

	[Test]
	public async Task Assign_WithNestedClasses_GeneratesCorrectSet()
	{
		// Act
		var args = OptionsHelper
			.Assign<AContainerForNestedClasses.TestOptionsSettings>(
				static c => c.EnableFeatureA = false,
				static c => c.MoreOptions.EnableFeatureZ = false,
				static c => c.MoreOptions.EvenMore.EndOfTheLine = "PIES"
			)
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(3);
		await Assert.That(args[0]).IsEqualTo("--TestOptions:EnableFeatureA=false");
		await Assert.That(args[1]).IsEqualTo("--TestOptions:MoreOptions:EnableFeatureZ=false");
		await Assert.That(args[2]).IsEqualTo("--TestOptions:MoreOptions:EvenMore:EndOfTheLine=PIES");
	}

	[Test]
	public async Task Assign_WithVariables_GeneratesCorrectSet()
	{
		// Arrange
		const bool featureAEnabled = false;

		// Act
		var args = OptionsHelper
			.Assign<AContainerForNestedClasses.TestOptionsSettings>(
				c => c.EnableFeatureA = featureAEnabled,
				c => c.MoreOptions.EnableFeatureZ = false,
				c => c.MoreOptions.EvenMore.EndOfTheLine = _aTestingValue
			)
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(3);
		await Assert.That(args[0]).IsEqualTo("--TestOptions:EnableFeatureA=false");
		await Assert.That(args[1]).IsEqualTo("--TestOptions:MoreOptions:EnableFeatureZ=false");
		await Assert.That(args[2]).IsEqualTo($"--TestOptions:MoreOptions:EvenMore:EndOfTheLine={_aTestingValue}");
	}

	[Test]
	public async Task Assign_GivenNoSectionOverride_UsesSectionNameConstValue()
	{
		// Arrange

		// Act
		var args = OptionsHelper.Assign<PrivateSectionOptions>(static c => c.Redis.Name = "PIES").Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--PrivateSection:Redis:Name=PIES");
	}

	[Test]
	public async Task Assign_GivenNoConstSection_RemovesKnownSuffix()
	{
		// Arrange

		// Act
		var fromOptions = OptionsHelper.Assign<ServiceOptions>(static c => c.Redis.Name = "PIES").Build();
		var fromSettings = OptionsHelper.Assign<ServiceSettings>(static c => c.Redis.Name = "PIES").Build();
		var fromConfiguration = OptionsHelper.Assign<ServiceConfiguration>(static c => c.Redis.Name = "PIES").Build();
		var fromConfig = OptionsHelper.Assign<ServiceConfig>(static c => c.Redis.Name = "PIES").Build();

		// Assert
		await Assert.That(fromOptions[0]).IsEqualTo("--Service:Redis:Name=PIES");
		await Assert.That(fromSettings[0]).IsEqualTo("--Service:Redis:Name=PIES");
		await Assert.That(fromConfiguration[0]).IsEqualTo("--Service:Redis:Name=PIES");
		await Assert.That(fromConfig[0]).IsEqualTo("--Service:Redis:Name=PIES");
	}

	[Test]
	public async Task Assign_GivenTypeNameOnlySuffix_UsesOriginalTypeName()
	{
		// Arrange

		// Act
		var args = OptionsHelper.Assign<Options>(static c => c.Redis.Name = "PIES").Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--Options:Redis:Name=PIES");
	}

	[Test]
	public async Task Assign_GivenDeepAssignment_ProducesColonSeparatedInfiniteDepthPath()
	{
		// Arrange

		// Act
		var args = OptionsHelper.Assign<DeepOptions>(static c => c.Level1.Level2.Level3.Level4.Name = "PIES").Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--Deep:Level1:Level2:Level3:Level4:Name=PIES");
	}

	[Test]
	public async Task Assign_GivenThreeAssignments_UsingParams_ProducesThreeArgs()
	{
		// Arrange

		// Act
		var args = OptionsHelper
			.Assign<HostKitOptions>(
				static c => c.Redis.IsEnabled = false,
				static c => c.Redis.Name = "PIES",
				static c => c.API.Name = "my-api"
			)
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(3);
		await Assert.That(args[0]).IsEqualTo("--HostKit:Redis:IsEnabled=false");
		await Assert.That(args[1]).IsEqualTo("--HostKit:Redis:Name=PIES");
		await Assert.That(args[2]).IsEqualTo("--HostKit:API:Name=my-api");
	}

	[Test]
	public async Task Assign_GivenAssignmentsArray_UsesArrayAndSectionOverride()
	{
		// Arrange
		Action<HostKitOptions>[] assignments =
		[
			static c => c.Redis.IsEnabled = false,
			static c => c.Redis.Name = "PIES",
			static c => c.API.Name = "my-api",
		];

		// Act
		var args = OptionsHelper.Assign("CustomSection", assignments).Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(3);
		await Assert.That(args[0]).IsEqualTo("--CustomSection:Redis:IsEnabled=false");
		await Assert.That(args[1]).IsEqualTo("--CustomSection:Redis:Name=PIES");
		await Assert.That(args[2]).IsEqualTo("--CustomSection:API:Name=my-api");
	}

	[Test]
	public async Task Assign_ChainedMultipleTimes_CollectsAllEntries()
	{
		// Arrange

		// Act
		var args = OptionsHelper
			.Assign<HostKitOptions>(static c => c.Redis.Name = "redis-a")
			.Assign<HostKitOptions>(static c => c.API.Name = "api-a")
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--HostKit:Redis:Name=redis-a");
		await Assert.That(args[1]).IsEqualTo("--HostKit:API:Name=api-a");
	}

	[Test]
	public async Task Assign_GivenSingleActionSettingMultiplePaths_Throws()
	{
		// Arrange

		// Act
		var exception = await Assert
			.That(static () =>
				OptionsHelper
					.Assign<HostKitOptions>(static c =>
					{
						c.Redis.Name = "PIES";
						c.API.Name = "api-a";
					})
					.Build()
			)
			.Throws<InvalidOperationException>();

		// Assert
		await Assert.That(exception!.Message).Contains("Found 2 candidate paths");
		await Assert.That(exception!.Message).Contains("Redis:Name");
		await Assert.That(exception!.Message).Contains("API:Name");
	}

	[Test]
	public async Task PathFor_GivenSimpleSelector_ReturnsMemberPath()
	{
		// Arrange

		// Act
		var path = OptionsHelper.PathFor<SampleStoreOptions>(static f => f.CurrentKey);

		// Assert
		await Assert.That(path).IsEqualTo("CurrentKey");
	}

	[Test]
	public async Task PathFor_GivenNestedSelector_ReturnsDotSeparatedPath()
	{
		// Arrange

		// Act
		var path = OptionsHelper.PathFor<SampleStoreOptions>(static f => f.Nested.Value);

		// Assert
		await Assert.That(path).IsEqualTo("Nested.Value");
	}

	[Test]
	public async Task PathFor_GivenNestedSelectorWithNullForgivingOperator_ReturnsDotSeparatedPath()
	{
		// Arrange

		// Act
		var path = OptionsHelper.PathFor<SampleStoreOptions>(static f => f.Nested!.Value);

		// Assert
		await Assert.That(path).IsEqualTo("Nested.Value");
	}

	[Test]
	public async Task PathFor_GivenDeepSelector_ReturnsDotSeparatedPath()
	{
		// Arrange

		// Act
		var path = OptionsHelper.PathFor<DeepOptions>(static f => f.Level1.Level2.Level3.Level4.Name);

		// Assert
		await Assert.That(path).IsEqualTo("Level1.Level2.Level3.Level4.Name");
	}

	[Test]
	public async Task PathFor_GivenInvalidSelector_Throws()
	{
		// Arrange

		// Act
		var exception = await Assert
			.That(static () => OptionsHelper.PathFor<SampleStoreOptions>(static f => f.Count + 1))
			.Throws<ArgumentException>();

		// Assert
		await Assert.That(exception!.Message).Contains("member access expression");
	}

	[Test]
	public async Task AsEnvironmentVariables_GivenEntries_ReturnsDictionaryWithDoubleUnderscoreKeys()
	{
		// Arrange

		// Act
		var envVars = OptionsHelper
			.Assign<HostKitOptions>(static c => c.Redis.Name = "PIES")
			.AsEnvironmentVariables()
			.Build();

		// Assert
		await Assert.That(envVars).ContainsKey("HostKit__Redis__Name");
		await Assert.That(envVars["HostKit__Redis__Name"]).IsEqualTo("PIES");
	}

	[Test]
	public async Task AsEnvironmentVariables_GivenMultipleEntries_ReturnsDictionaryWithAllEntries()
	{
		// Arrange

		// Act
		var envVars = OptionsHelper
			.Assign<HostKitOptions>(
				static c => c.Redis.IsEnabled = false,
				static c => c.Redis.Name = "PIES",
				static c => c.API.Name = "my-api"
			)
			.AsEnvironmentVariables()
			.Build();

		// Assert
		await Assert.That(envVars.Count).IsEqualTo(3);
		await Assert.That(envVars["HostKit__Redis__IsEnabled"]).IsEqualTo("false");
		await Assert.That(envVars["HostKit__Redis__Name"]).IsEqualTo("PIES");
		await Assert.That(envVars["HostKit__API__Name"]).IsEqualTo("my-api");
	}

	[Test]
	public async Task AsEnvironmentVariables_GivenExplicitSectionName_ReturnsDictionaryWithOverrideKey()
	{
		// Arrange
		const string sectionName = "CustomSection";

		// Act
		var envVars = OptionsHelper
			.Assign<HostKitOptions>(sectionName, static c => c.Redis.Name = "PIES")
			.AsEnvironmentVariables()
			.Build();

		// Assert
		await Assert.That(envVars).ContainsKey("CustomSection__Redis__Name");
		await Assert.That(envVars["CustomSection__Redis__Name"]).IsEqualTo("PIES");
	}
}
