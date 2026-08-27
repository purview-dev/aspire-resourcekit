using Purview.Aspire.ResourceKit.Models;

namespace Purview.Aspire.ResourceKit;

public sealed class OptionsHelperTests
{
	readonly string _aTestingValue = $"This is a test value - {Guid.NewGuid()}";

	[Test]
	public async Task For_GivenExplicitSectionName_UsesSectionOverride()
	{
		// Arrange
		const string sectionName = "SectionNameGoesHere";

		// Act
		var args = OptionsHelper
			.ForSet<HostKitOptions>(sectionName, c => c.Redis.IsEnabled = false, c => c.Redis.Name = "PIES")
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--SectionNameGoesHere:Redis:IsEnabled=false");
		await Assert.That(args[1]).IsEqualTo("--SectionNameGoesHere:Redis:Name=PIES");
	}

	[Test]
	public async Task ForSet_WithNestedClasses_GeneratesCorrectSet()
	{
		// Act
		var args = OptionsHelper
			.ForSet<AContainerForNestedClasses.TestOptionsSettings>(
				c => c.EnableFeatureA = false,
				c => c.MoreOptions.EnableFeatureZ = false,
				c => c.MoreOptions.EvenMore.EndOfTheLine = "PIES"
			)
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(3);
		await Assert.That(args[0]).IsEqualTo("--TestOptions:EnableFeatureA=false");
		await Assert.That(args[1]).IsEqualTo("--TestOptions:MoreOptions:EnableFeatureZ=false");
		await Assert.That(args[2]).IsEqualTo("--TestOptions:MoreOptions:EvenMore:EndOfTheLine=PIES");
	}

	[Test]
	public async Task ForSet_WithVariables_GeneratesCorrectSet()
	{
		// Arrange
		const bool featureAEnabled = false;

		// Act
		var args = OptionsHelper
			.ForSet<AContainerForNestedClasses.TestOptionsSettings>(
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
	public async Task For_GivenNoSectionOverride_UsesSectionNameConstValue()
	{
		// Arrange

		// Act
		var args = OptionsHelper.ForSet<PrivateSectionOptions>(c => c.Redis.Name = "PIES").Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--PrivateSection:Redis:Name=PIES");
	}

	[Test]
	public async Task For_GivenNoConstSection_RemovesKnownSuffix()
	{
		// Arrange

		// Act
		var fromOptions = OptionsHelper.ForSet<ServiceOptions>(c => c.Redis.Name = "PIES").Build();
		var fromSettings = OptionsHelper.ForSet<ServiceSettings>(c => c.Redis.Name = "PIES").Build();
		var fromConfiguration = OptionsHelper.ForSet<ServiceConfiguration>(c => c.Redis.Name = "PIES").Build();
		var fromConfig = OptionsHelper.ForSet<ServiceConfig>(c => c.Redis.Name = "PIES").Build();

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
		var args = OptionsHelper.ForSet<Options>(c => c.Redis.Name = "PIES").Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--Options:Redis:Name=PIES");
	}

	[Test]
	public async Task For_GivenDeepAssignment_ProducesColonSeparatedInfiniteDepthPath()
	{
		// Arrange

		// Act
		var args = OptionsHelper.ForSet<DeepOptions>(c => c.Level1.Level2.Level3.Level4.Name = "PIES").Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--Deep:Level1:Level2:Level3:Level4:Name=PIES");
	}

	[Test]
	public async Task For_GivenThreeAssignments_UsingParams_ProducesThreeArgs()
	{
		// Arrange

		// Act
		var args = OptionsHelper
			.ForSet<HostKitOptions>(
				c => c.Redis.IsEnabled = false,
				c => c.Redis.Name = "PIES",
				c => c.Api.Name = "my-api"
			)
			.Build();

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
		var args = OptionsHelper.ForSet("CustomSection", assignments).Build();

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
		var arg = OptionsHelper.ForOne<SampleStoreOptions>(f => f.CurrentKey).Build()[0];

		// Assert
		await Assert.That(arg).IsEqualTo("--SampleStore:CurrentKey=default-key");
	}

	[Test]
	public async Task ForOne_GivenSelectorExpressionWithValueType_ReturnsArgumentWithDefaultValue()
	{
		// Arrange

		// Act
		var arg = OptionsHelper.ForOne<SampleStoreOptions>(f => f.Count).Build()[0];

		// Assert
		await Assert.That(arg).IsEqualTo("--SampleStore:Count=42");
	}

	[Test]
	public async Task ForOne_GivenSectionNameAndSelectorExpression_ReturnsArgumentWithOverride()
	{
		// Arrange
		const string sectionName = "MySection";

		// Act
		var arg = OptionsHelper.ForOne<SampleStoreOptions>(sectionName, f => f.CurrentKey).Build()[0];

		// Assert
		await Assert.That(arg).IsEqualTo("--MySection:CurrentKey=default-key");
	}

	[Test]
	public async Task ForOne_GivenNestedSelectorExpression_ReturnsArgumentWithNestedDefaultValue()
	{
		// Arrange

		// Act
		var arg = OptionsHelper.ForOne<SampleStoreOptions>(f => f.Nested!.Value).Build()[0];

		// Assert
		await Assert.That(arg).IsEqualTo("--SampleStore:Nested:Value=nested-default");
	}

	[Test]
	public async Task For_GivenSelectorExpressions_ReturnsArgumentsWithDefaultValues()
	{
		// Arrange

		// Act
		var args = OptionsHelper.For<SampleStoreOptions>(f => f.CurrentKey, f => f.Count).Build();

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
		var args = OptionsHelper.For<SampleStoreOptions>(sectionName, f => f.CurrentKey, f => f.Count).Build();

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
		var args = OptionsHelper.For<SampleStoreOptions>(f => f.Nested!.Value).Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(1);
		await Assert.That(args[0]).IsEqualTo("--SampleStore:Nested:Value=nested-default");
	}

	[Test]
	public async Task ForSet_ChainedMultipleTimes_CollectsAllEntries()
	{
		// Arrange

		// Act
		var args = OptionsHelper
			.ForSet<HostKitOptions>(c => c.Redis.Name = "redis-a")
			.ForSet<HostKitOptions>(c => c.Api.Name = "api-a")
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--HostKit:Redis:Name=redis-a");
		await Assert.That(args[1]).IsEqualTo("--HostKit:Api:Name=api-a");
	}

	[Test]
	public async Task ForSetOne_ChainedWithForSet_CollectsAllEntries()
	{
		// Arrange

		// Act
		var args = OptionsHelper
			.ForSetOne<HostKitOptions>(c => c.Redis.Name = "redis-b")
			.ForSet<HostKitOptions>(c => c.Api.Name = "api-b")
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--HostKit:Redis:Name=redis-b");
		await Assert.That(args[1]).IsEqualTo("--HostKit:Api:Name=api-b");
	}

	[Test]
	public async Task For_ChainedWithForSet_CollectsAllEntries()
	{
		// Arrange

		// Act
		var args = OptionsHelper
			.For<SampleStoreOptions>(f => f.CurrentKey)
			.ForSet<HostKitOptions>(c => c.Redis.Name = "redis-c")
			.Build();

		// Assert
		await Assert.That(args.Length).IsEqualTo(2);
		await Assert.That(args[0]).IsEqualTo("--SampleStore:CurrentKey=default-key");
		await Assert.That(args[1]).IsEqualTo("--HostKit:Redis:Name=redis-c");
	}

	[Test]
	public async Task AsEnvironmentVariables_GivenEntries_ReturnsDictionaryWithDoubleUnderscoreKeys()
	{
		// Arrange

		// Act
		var envVars = OptionsHelper.ForSet<HostKitOptions>(c => c.Redis.Name = "PIES").AsEnvironmentVariables().Build();

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
			.ForSet<HostKitOptions>(
				c => c.Redis.IsEnabled = false,
				c => c.Redis.Name = "PIES",
				c => c.Api.Name = "my-api"
			)
			.AsEnvironmentVariables()
			.Build();

		// Assert
		await Assert.That(envVars.Count).IsEqualTo(3);
		await Assert.That(envVars["HostKit__Redis__IsEnabled"]).IsEqualTo("false");
		await Assert.That(envVars["HostKit__Redis__Name"]).IsEqualTo("PIES");
		await Assert.That(envVars["HostKit__Api__Name"]).IsEqualTo("my-api");
	}

	[Test]
	public async Task AsEnvironmentVariables_GivenExplicitSectionName_ReturnsDictionaryWithOverrideKey()
	{
		// Arrange
		const string sectionName = "CustomSection";

		// Act
		var envVars = OptionsHelper
			.ForSet<HostKitOptions>(sectionName, c => c.Redis.Name = "PIES")
			.AsEnvironmentVariables()
			.Build();

		// Assert
		await Assert.That(envVars).ContainsKey("CustomSection__Redis__Name");
		await Assert.That(envVars["CustomSection__Redis__Name"]).IsEqualTo("PIES");
	}
}
