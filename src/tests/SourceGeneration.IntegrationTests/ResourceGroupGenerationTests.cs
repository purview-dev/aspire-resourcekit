using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies what <see cref="HostKitGenerator"/> emits for <c>[ResourceGroup]</c> and <c>[InGroup&lt;TGroup&gt;]</c>,
/// and the group diagnostics <see cref="ResourceKitDiagnosticAnalyzer"/> reports (SG0021-SG0025).
/// </summary>
public sealed class ResourceGroupGenerationTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string Kit = """
		protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
			throw new global::System.NotImplementedException();
		""";

	[Test]
	public async Task Generate_GivenKitInSeveralGroups_RegistersItWithPrimaryGroupFirst(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			partial class DataGroup;

			[ResourceGroup]
			partial class BackupGroup;

			[ResourceDefinition<DefaultAspireResource>]
			[InGroup<DataGroup>]
			[InGroup<BackupGroup>(Primary = true)]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.GetSource()).Contains("AddResource(Postgres, Groups.Backup, Groups.Data);");
	}

	[Test]
	public async Task Generate_GivenChildDeclaredBeforeParent_CreatesParentFirst(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup(Parent = typeof(PlatformGroup))]
			partial class DataGroup;

			[ResourceGroup]
			partial class PlatformGroup;

			[ResourceDefinition<DefaultAspireResource>]
			[InGroup<DataGroup>]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		var platform = generated.IndexOf("Platform = new(options.Groups.Platform, null);", StringComparison.Ordinal);
		var data = generated.IndexOf("Data = new(options.Groups.Data, Platform);", StringComparison.Ordinal);
		await Assert.That(platform).IsGreaterThan(-1);
		await Assert.That(data).IsGreaterThan(platform);
	}

	[Test]
	public async Task Generate_GivenGroup_DerivesItFromResourceKitGroupAndBindsOptions(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup("storage", ShowInDashboard = false)]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert
			.That(generated)
			.Contains(
				"partial class DataGroup : global::Purview.Aspire.ResourceKit.ResourceKitGroup<global::Testing.TestingHostKit>"
			);
		await Assert.That(generated).Contains("base(\"storage\", parent)");
		await Assert.That(generated).Contains("public bool ShowInDashboard { get; set; } = false;");
		await Assert.That(generated).Contains("ResourceKitGroupsOptions Groups { get; init; } = new();");
	}

	[Test]
	public async Task Generate_GivenGenerateOptionsDisabled_SetsGroupDefaultsFromAttribute(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit(GenerateOptions = false)]
			partial class TestingHostKit;

			[ResourceGroup(ShowInDashboard = false, Mode = {{TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode}}.Publish)]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert
			.That(generated)
			.Contains($"Mode = {TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode}.Publish;");
		await Assert.That(generated).Contains("ShowInDashboard = false;");
		await Assert.That(generated).Contains("Data = new(null);");
	}

	[Test]
	public async Task Generate_GivenInGroupOfNonGroupType_ReportsGroupTypeNotResourceGroup(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			sealed class NotAGroup;

			[ResourceDefinition<DefaultAspireResource>]
			[InGroup<NotAGroup>]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.GroupTypeNotResourceGroup);
	}

	[Test]
	public async Task Generate_GivenParentOfNonGroupType_ReportsGroupTypeNotResourceGroup(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			sealed class NotAGroup;

			[ResourceGroup(Parent = typeof(NotAGroup))]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.GroupTypeNotResourceGroup);
	}

	[Test]
	public async Task Generate_GivenParentCycle_ReportsResourceGroupCycle(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup(Parent = typeof(BGroup))]
			partial class AGroup;

			[ResourceGroup(Parent = typeof(AGroup))]
			partial class BGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceGroupCycle);
	}

	[Test]
	public async Task Generate_GivenTwoGroupsWithTheSameName_ReportsDuplicateResourceGroupName(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup("data")]
			partial class DataGroup;

			[ResourceGroup("Data")]
			partial class StorageGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.DuplicateResourceGroupName);
	}

	[Test]
	public async Task Generate_GivenTwoPrimaryGroups_ReportsMultiplePrimaryGroupsAndStillGenerates(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			partial class DataGroup;

			[ResourceGroup]
			partial class BackupGroup;

			[ResourceDefinition<DefaultAspireResource>]
			[InGroup<DataGroup>(Primary = true)]
			[InGroup<BackupGroup>(Primary = true)]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.MultiplePrimaryGroups);
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.GetSource()).Contains("AddResource(Postgres, Groups.Data, Groups.Backup);");
	}

	[Test]
	public async Task Generate_GivenGroupWithExplicitBase_ReportsResourceGroupCannotHaveExplicitBase(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			class SomeBase;

			[ResourceGroup]
			partial class DataGroup : SomeBase;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceGroupCannotHaveExplicitBase);
	}

	[Test]
	public async Task Generate_GivenGroupThatIsNotPartial_ReportsClassMustBePartial(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			sealed class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ClassMustBePartial);
	}

	[Test]
	public async Task Generate_GivenGenericResourceGroup_NestsTheGroupUnderItsTypeArgument(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			partial class PlatformGroup;

			[ResourceGroup<PlatformGroup>("storage")]
			partial class DataGroup;

			[ResourceDefinition<DefaultAspireResource>]
			[InGroup<DataGroup>]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("Data = new(options.Groups.Data, Platform);");
		await Assert.That(generated).Contains("base(\"storage\", parent)");
	}

	[Test]
	[Timeout(CycleTimeoutMilliseconds)]
	public async Task Generate_GivenGroupThatIsItsOwnParentThroughParent_ReportsResourceGroupCycle(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup(Parent = typeof(AGroup))]
			partial class AGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(Reporting(result, DiagnosticLibrary.ResourceGroupCycle)).IsEquivalentTo(["AGroup"]);
	}

	[Test]
	[Timeout(CycleTimeoutMilliseconds)]
	public async Task Generate_GivenGroupThatIsItsOwnParentThroughTypeArgument_ReportsResourceGroupCycle(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup<AGroup>]
			partial class AGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(Reporting(result, DiagnosticLibrary.ResourceGroupCycle)).IsEquivalentTo(["AGroup"]);
	}

	[Test]
	[Timeout(CycleTimeoutMilliseconds)]
	public async Task Generate_GivenThreeGroupCycle_ReportsResourceGroupCycleOnEveryMember(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup<BGroup>]
			partial class AGroup;

			[ResourceGroup(Parent = typeof(CGroup))]
			partial class BGroup;

			[ResourceGroup<AGroup>]
			partial class CGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert
			.That(Reporting(result, DiagnosticLibrary.ResourceGroupCycle))
			.IsEquivalentTo(["AGroup", "BGroup", "CGroup"]);
	}

	[Test]
	[Timeout(CycleTimeoutMilliseconds)]
	public async Task Generate_GivenGroupLeadingIntoCycleItIsNotPartOf_ReportsOnlyTheCycleMembers(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// AGroup -> BGroup -> CGroup -> BGroup: the walk from AGroup must stop when it revisits BGroup.
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup<BGroup>]
			partial class AGroup;

			[ResourceGroup<CGroup>]
			partial class BGroup;

			[ResourceGroup<BGroup>]
			partial class CGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(Reporting(result, DiagnosticLibrary.ResourceGroupCycle)).IsEquivalentTo(["BGroup", "CGroup"]);
		await Assert.That(result.DriverResult.GeneratedTrees.Length).IsEqualTo(ExpectedGeneratedFileCount);
	}

	[Test]
	[Timeout(CycleTimeoutMilliseconds)]
	public async Task Generate_GivenGroupParentedToConstructedFormOfItself_ReportsResourceGroupCannotBeGeneric(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Without normalising to the original definition, G<T> -> G<G<T>> -> ... never revisits a type.
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup<G<G<T>>>]
			partial class G<T>;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceGroupCannotBeGeneric);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceGroupCycle);
	}

	[Test]
	public async Task Generate_GivenParentAsTypeArgumentAndProperty_ReportsMultipleResourceGroupParents(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			partial class PlatformGroup;

			[ResourceGroup]
			partial class SharedGroup;

			[ResourceGroup<PlatformGroup>(Parent = typeof(SharedGroup))]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert
			.That(Reporting(result, DiagnosticLibrary.MultipleResourceGroupParents))
			.IsEquivalentTo(["DataGroup"]);
	}

	[Test]
	public async Task Generate_GivenBothGroupAttributes_ReportsMultipleResourceGroupParents(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			partial class PlatformGroup;

			[ResourceGroup]
			[ResourceGroup<PlatformGroup>]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert
			.That(Reporting(result, DiagnosticLibrary.MultipleResourceGroupParents))
			.IsEquivalentTo(["DataGroup"]);
	}

	[Test]
	public async Task Generate_GivenGenericGroupClass_ReportsResourceGroupCannotBeGeneric(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			partial class DataGroup<T>;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceGroupCannotBeGeneric);
	}

	[Test]
	public async Task Generate_GivenGroupInItself_ReportsResourceGroupCannotBeResourceKit(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			[InGroup<DataGroup>]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert
			.That(Reporting(result, DiagnosticLibrary.ResourceGroupCannotBeResourceKit))
			.IsEquivalentTo(["DataGroup"]);
	}

	[Test]
	public async Task Generate_GivenClassThatIsBothGroupAndKit_ReportsResourceGroupCannotBeResourceKit(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceGroupCannotBeResourceKit);
	}

	[Test]
	public async Task Generate_GivenKitInItself_ReportsGroupTypeNotResourceGroup(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			[InGroup<PostgresKit>]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.GroupTypeNotResourceGroup);
	}

	// A cycle that is not guarded hangs rather than fails; the timeout turns a hang into a failure.
	const int CycleTimeoutMilliseconds = 60_000;

	// The source text each diagnostic points at, which for a group rule is the group's identifier.
	static string[] Reporting(DriverRunResult result, DiagnosticDescriptor descriptor) =>
		[
			.. (result.AnalyzerResult?.Diagnostics ?? [])
				.Where(d => d.Id == descriptor.Id && d.Location.SourceTree is not null)
				.Select(static d => d.Location.SourceTree!.GetText().ToString(d.Location.SourceSpan))
				.Order(StringComparer.Ordinal),
		];

	protected override ResourceKitSourceGeneratorTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		ResourceKitSourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	)
	{
		return base.OnBeforeRun(
			sources,
			options
				.WithAdditionalAssemblyTypes(typeof(DefaultAspireResource))
				.WithAdditionalNamespaces(TestingTypeLibrary.Purview.Aspire.ResourceKit.DefaultAspireResource),
			cancellationToken
		);
	}
}
