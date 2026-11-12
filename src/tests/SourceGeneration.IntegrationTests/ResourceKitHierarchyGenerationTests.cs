using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies what <see cref="HostKitGenerator"/> emits for parent/child resource kits and the hierarchy
/// diagnostics reported by the analyzer and generator (SG0032-SG0038).
/// </summary>
public sealed class ResourceKitHierarchyGenerationTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string Kit = """
		protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
			throw new global::System.NotImplementedException();
		""";

	[Test]
	public async Task Generate_GivenParentAndChild_GeneratesTypedParentChildAndNestedOptions(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("postgres")]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("db", PropertyName = "Database", Parent = typeof(PostgresKit))]
			sealed partial class DatabaseKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("public global::Testing.PostgresKit Postgres");
		await Assert.That(generated).Contains("public global::Testing.DatabaseKit Database");
		await Assert.That(generated).Contains("public global::Testing.PostgresKit Parent { get; }");
		await Assert.That(generated).Contains("Database = new(this, Postgres, Options.Postgres.Database);");
		await Assert.That(generated).Contains("AddResource(Postgres.Database);");
		await Assert.That(generated).Contains("Database { get; init; } = new();");
	}

	[Test]
	public async Task Generate_GivenParentAndChild_DoesNotExposeChildAsAHostOptionsProperty(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("postgres")]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("db", Parent = typeof(PostgresKit))]
			sealed partial class DatabaseKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("public global::Testing.PostgresKit.PostgresKitOptions Postgres");
		await Assert.That(generated).DoesNotContain("new(this, Options.Database)");
		await Assert.That(generated).Contains("Database = new(this, Postgres, Options.Postgres.Database);");
	}

	[Test]
	public async Task Generate_GivenOneParentWithSeveralChildren_GeneratesEachChildProperty(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("vnet")]
			sealed partial class VnetKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("snet-1", PropertyName = "Subnet1", Parent = typeof(VnetKit))]
			sealed partial class Subnet1Kit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("snet-2", PropertyName = "Subnet2", Parent = typeof(VnetKit))]
			sealed partial class Subnet2Kit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("snet-3", PropertyName = "Subnet3", Parent = typeof(VnetKit))]
			sealed partial class Subnet3Kit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("public global::Testing.Subnet1Kit Subnet1");
		await Assert.That(generated).Contains("public global::Testing.Subnet2Kit Subnet2");
		await Assert.That(generated).Contains("public global::Testing.Subnet3Kit Subnet3");
		await Assert.That(generated).Contains("Vnet.Subnet1 = new(this, Vnet, Options.Vnet.Subnet1);");
		await Assert.That(generated).Contains("Vnet.Subnet2 = new(this, Vnet, Options.Vnet.Subnet2);");
		await Assert.That(generated).Contains("Vnet.Subnet3 = new(this, Vnet, Options.Vnet.Subnet3);");
	}

	[Test]
	public async Task Generate_GivenFiveLevelHierarchy_ConstructsEachParentBeforeItsChild(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("level1")]
			sealed partial class Level1Kit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("level2", Parent = typeof(Level1Kit))]
			sealed partial class Level2Kit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("level3", Parent = typeof(Level2Kit))]
			sealed partial class Level3Kit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("level4", Parent = typeof(Level3Kit))]
			sealed partial class Level4Kit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("level5", Parent = typeof(Level4Kit))]
			sealed partial class Level5Kit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		var level1 = generated.IndexOf("AddResource(Level1);", StringComparison.Ordinal);
		var level2 = generated.IndexOf("AddResource(Level1.Level2);", StringComparison.Ordinal);
		var level3 = generated.IndexOf("AddResource(Level1.Level2.Level3);", StringComparison.Ordinal);
		var level4 = generated.IndexOf("AddResource(Level1.Level2.Level3.Level4);", StringComparison.Ordinal);
		var level5 = generated.IndexOf("AddResource(Level1.Level2.Level3.Level4.Level5);", StringComparison.Ordinal);

		await Assert.That(level1).IsGreaterThan(-1);
		await Assert.That(level2).IsGreaterThan(level1);
		await Assert.That(level3).IsGreaterThan(level2);
		await Assert.That(level4).IsGreaterThan(level3);
		await Assert.That(level5).IsGreaterThan(level4);
	}

	[Test]
	public async Task Generate_GivenChildInAnotherNamespace_CompilesFullyQualifiedParentAndChild()
	{
		// Arrange
		var source = $$"""
			namespace ParentNamespace
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<DefaultAspireResource>("postgres")]
				sealed partial class PostgresKit
				{
					{{Kit}}
				}
			}

			namespace ChildNamespace
			{
				[ResourceDefinition<DefaultAspireResource>("db", PropertyName = "Database", Parent = typeof(ParentNamespace.PostgresKit))]
				sealed partial class DatabaseKit
				{
					{{Kit}}
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, CancellationToken.None);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("global::ChildNamespace.DatabaseKit Database");
		await Assert.That(generated).Contains("global::ParentNamespace.PostgresKit Parent");
	}

	[Test]
	public async Task Generate_GivenNonGenericChildWithExplicitBase_GeneratesParentAwareConstructor()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("postgres")]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}

			[ResourceDefinition("db", PropertyName = "Database", Parent = typeof(PostgresKit))]
			sealed partial class DatabaseKit : ResourceKitBase<DefaultAspireResource>
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, CancellationToken.None);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("public DatabaseKit(");
		await Assert.That(generated).Contains("global::Testing.PostgresKit parent");
		await Assert.That(generated).Contains("ParentKit = Parent;");
	}

	[Test]
	public async Task Generate_GivenGenerateOptionsDisabled_UsesExplicitResourceNameForChild()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit(GenerateOptions = false)]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("postgres")]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("db", Parent = typeof(PostgresKit))]
			sealed partial class DatabaseKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, CancellationToken.None);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("Database = new(this, Postgres, \"db\");");
		await Assert.That(generated).DoesNotContain("Options.Postgres.Database");
	}

	[Test]
	public async Task Generate_GivenIdenticalShortNamesUnderDifferentParents_DoesNotReportADuplicate()
	{
		// Arrange
		var source = $$"""
			namespace First
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<DefaultAspireResource>("first-parent")]
				sealed partial class FirstParentKit
				{
					{{Kit}}
				}

				[ResourceDefinition<DefaultAspireResource>("first-db", PropertyName = "Database", Parent = typeof(FirstParentKit))]
				sealed partial class FirstDatabaseKit
				{
					{{Kit}}
				}
			}

			namespace Second
			{
				[ResourceDefinition<DefaultAspireResource>("second-parent")]
				sealed partial class SecondParentKit
				{
					{{Kit}}
				}

				[ResourceDefinition<DefaultAspireResource>("second-db", PropertyName = "Database", Parent = typeof(SecondParentKit))]
				sealed partial class SecondDatabaseKit
				{
					{{Kit}}
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, CancellationToken.None);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.DuplicateResourceKitChildPropertyName);
	}

	[Test]
	public async Task Generate_GivenParentsWithTheSameShortNameInDifferentNamespaces_KeepsTypeIdentity()
	{
		// Arrange
		var source = $$"""
			namespace First
			{
				[ResourceDefinition<DefaultAspireResource>("first-parent", PropertyName = "FirstParent")]
				sealed partial class ParentKit
				{
					{{Kit}}
				}

				[ResourceDefinition<DefaultAspireResource>("first-child", Parent = typeof(ParentKit))]
				sealed partial class ChildKit
				{
					{{Kit}}
				}
			}

			namespace Second
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<DefaultAspireResource>("second-parent", PropertyName = "SecondParent")]
				sealed partial class ParentKit
				{
					{{Kit}}
				}

				[ResourceDefinition<DefaultAspireResource>("second-child", Parent = typeof(ParentKit))]
				sealed partial class ChildKit
				{
					{{Kit}}
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, CancellationToken.None);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("global::First.ParentKit Parent");
		await Assert.That(generated).Contains("global::Second.ParentKit Parent");
	}

	[Test]
	public async Task Generate_RegardlessOfSourceOrder_ProducesTheSameOutput(CancellationToken cancellationToken)
	{
		// Arrange
		var parentSource = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("postgres")]
			sealed partial class PostgresKit
			{
				{{Kit}}
			}
			""";
		var childSource = $$"""
			namespace Testing;

			[ResourceDefinition<DefaultAspireResource>("db", Parent = typeof(PostgresKit))]
			sealed partial class DatabaseKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var forward = await GenerateAsync([parentSource, childSource], cancellationToken);
		using var reverse = await GenerateAsync([childSource, parentSource], cancellationToken);

		// Assert
		await Assert.That(forward.GetSource()).IsEqualTo(reverse.GetSource());
	}

	[Test]
	public async Task Generate_GivenSelfParent_ReportsCannotParentItself()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("self", Parent = typeof(SelfKit))]
			sealed partial class SelfKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			CancellationToken.None
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceKitCannotParentItself);
	}

	[Test]
	public async Task Generate_GivenTwoNodeCycle_ReportsCycleWithPath()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("a", Parent = typeof(BKit))]
			sealed partial class AKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("b", Parent = typeof(AKit))]
			sealed partial class BKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			CancellationToken.None
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceKitParentCycle);
	}

	[Test]
	public async Task Generate_GivenThreeNodeCycle_ReportsCycle()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("a", Parent = typeof(BKit))]
			sealed partial class AKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("b", Parent = typeof(CKit))]
			sealed partial class BKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("c", Parent = typeof(AKit))]
			sealed partial class CKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			CancellationToken.None
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceKitParentCycle);
	}

	[Test]
	public async Task Generate_GivenParentThatIsNotAResourceKit_ReportsNotAResourceKit()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			public sealed class NotAKit;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("child", Parent = typeof(NotAKit))]
			sealed partial class ChildKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			CancellationToken.None
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceKitParentNotResourceKit);
	}

	[Test]
	public async Task Generate_GivenDuplicateChildPropertyNameUnderOneParent_ReportsDuplicate()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("parent")]
			sealed partial class ParentKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("one", PropertyName = "Database", Parent = typeof(ParentKit))]
			sealed partial class OneKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("two", PropertyName = "Database", Parent = typeof(ParentKit))]
			sealed partial class TwoKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			CancellationToken.None
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.DuplicateResourceKitChildPropertyName);
	}

	[Test]
	public async Task Generate_GivenChildPropertyCollidingWithParentMember_ReportsCollision()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("parent")]
			sealed partial class ParentKit
			{
				public string Database { get; } = "existing";

				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("db", PropertyName = "Database", Parent = typeof(ParentKit))]
			sealed partial class DatabaseKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			CancellationToken.None
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceKitChildPropertyCollision);
	}

	[Test]
	public async Task Generate_GivenUserAuthoredParentMember_ReportsParentCollision()
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("parent")]
			sealed partial class ParentKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("db", Parent = typeof(ParentKit))]
			sealed partial class DatabaseKit
			{
				public ParentKit Parent { get; } = null!;

				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			CancellationToken.None
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceKitParentMemberCollision);
	}
}
