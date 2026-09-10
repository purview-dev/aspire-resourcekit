using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies that <c>ResourceDefinition&lt;TResource&gt;</c> accepts Aspire project reference types
/// (types implementing <c>IProjectMetadata</c>, such as the generated <c>Projects.*</c> types), maps them
/// to <c>ProjectResource</c> for the generated base class, and reports SG0018/SG0019 when the declared
/// project is inconsistent with the build/configure wiring.
/// </summary>
public class ProjectResourceDefinitionTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	[Test]
	public async Task Generate_GivenProjectReferenceTypeAndMatchingAddProject_GeneratesProjectResourceBase(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Projects
			{
				public class Example_Service : global::Aspire.Hosting.IProjectMetadata
				{
					public string ProjectPath => "";
					public bool SuppressBuild => true;
				}
			}

			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<Projects.Example_Service>]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddProject<Projects.Example_Service>(Name);
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert
			.That(generated)
			.Contains(
				$"{TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase}<{TypeLibrary.Aspire.Hosting.ApplicationModel.ProjectResource}>"
			);
	}

	[Test]
	public async Task Generate_GivenProjectReferenceTypeWithDifferentAddProject_ReportsProjectDefinitionMismatch(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Projects
			{
				public class Example_Service : global::Aspire.Hosting.IProjectMetadata
				{
					public string ProjectPath => "";
					public bool SuppressBuild => true;
				}

				public class Other_Service : global::Aspire.Hosting.IProjectMetadata
				{
					public string ProjectPath => "";
					public bool SuppressBuild => true;
				}
			}

			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<Projects.Example_Service>]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddProject<Projects.Other_Service>(Name);
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch);
	}

	[Test]
	public async Task Generate_GivenProjectReferenceTypeWithExplicitNonProjectResourceBase_ReportsProjectResourceKitBaseMismatch(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Projects
			{
				public class Example_Service : global::Aspire.Hosting.IProjectMetadata
				{
					public string ProjectPath => "";
					public bool SuppressBuild => true;
				}
			}

			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<Projects.Example_Service>]
				sealed partial class ApiKit : ResourceKitBase<DefaultAspireResource>
				{
					protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
						throw new global::System.NotImplementedException();
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ProjectResourceKitBaseMismatch);
	}

	[Test]
	public async Task Generate_GivenTypeArgumentThatIsNeitherResourceNorProject_ReportsNoAspireResourceFound(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				public class SomePlainClass { }

				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<SomePlainClass>]
				sealed partial class ApiKit;
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.NoAspireResourceFound);
	}

	[Test]
	public async Task Generate_GivenConcreteProjectResourceType_RemainsSupported(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			namespace Projects
			{
				public class Example_Service : global::Aspire.Hosting.IProjectMetadata
				{
					public string ProjectPath => "";
					public bool SuppressBuild => true;
				}
			}

			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<ProjectResource>]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddProject<Projects.Example_Service>(Name);
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
	}

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
