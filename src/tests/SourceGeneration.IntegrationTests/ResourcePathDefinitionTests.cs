using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies that <c>ResourcePath</c> validates the directory a JavaScript app resource is added from,
/// and that the path arguments keep their existing rules: <c>ProjectPath</c> stays .NET-only, a
/// conflicting pair is reported, and <c>ResourcePath</c> on a non-path resource is reported.
/// </summary>
/// <remarks>
/// The JavaScript types are declared in the test source rather than referenced from
/// <c>Aspire.Hosting.JavaScript</c>, because the generator resolves them by metadata name and a consumer
/// without the package must keep compiling.
/// </remarks>
public sealed class ResourcePathDefinitionTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string JavaScriptStubs = """
		namespace Aspire.Hosting.JavaScript
		{
			public class JavaScriptAppResource : global::Aspire.Hosting.ApplicationModel.IResource
			{
				public string Name => "app";

				public global::Aspire.Hosting.ApplicationModel.ResourceAnnotationCollection Annotations => [];
			}

			public class ViteAppResource : JavaScriptAppResource;
		}

		namespace Aspire.Hosting
		{
			public static class JavaScriptTestExtensions
			{
				public static global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> AddViteApp(
					this global::Aspire.Hosting.IDistributedApplicationBuilder builder,
					string name,
					string appDirectory,
					string runScriptName) => throw new global::System.NotImplementedException();
			}
		}

		""";

	[Test]
	public async Task Generate_GivenJavaScriptAppWithMatchingAddViteApp_GeneratesTheAppResourceBase(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			JavaScriptStubs
			+ """
				namespace Testing
				{
					[HostKit]
					partial class TestingHostKit;

					[ResourceDefinition<global::Aspire.Hosting.JavaScript.ViteAppResource>("web", ResourcePath = "../web")]
					sealed partial class WebKit
					{
						protected override IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> BuildResource(IDistributedApplicationBuilder builder) =>
							builder.AddViteApp(Name, "../web", "dev");
					}
				}
				""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch);
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ResourcePathRequiresPathResource);

		var generated = result.GetSource();
		await Assert
			.That(generated)
			.Contains(
				$"partial class WebKit : {TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase}<global::Aspire.Hosting.JavaScript.ViteAppResource>"
			)
			.Because("the declared Vite app type is the generated base type argument");
	}

	[Test]
	public async Task Generate_GivenNonGenericJavaScriptAppKit_ValidatesAddViteApp(CancellationToken cancellationToken)
	{
		// Arrange
		var source =
			JavaScriptStubs
			+ """
				namespace Testing
				{
					[HostKit]
					partial class TestingHostKit;

					[ResourceDefinition("web", ResourcePath = "../web")]
					sealed partial class WebKit : ResourceKitBase<global::Aspire.Hosting.JavaScript.ViteAppResource>
					{
						protected override IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> BuildResource(IDistributedApplicationBuilder builder) =>
							builder.AddViteApp(Name, "../web", "dev");
					}
				}
				""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch);
	}

	[Test]
	public async Task Generate_GivenAddViteAppWithMismatchedPath_ReportsProjectDefinitionMismatch(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			JavaScriptStubs
			+ """
				namespace Testing
				{
					[HostKit]
					partial class TestingHostKit;

					[ResourceDefinition<global::Aspire.Hosting.JavaScript.ViteAppResource>("web", ResourcePath = "../web")]
					sealed partial class WebKit
					{
						protected override IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> BuildResource(IDistributedApplicationBuilder builder) =>
							builder.AddViteApp(Name, "../other", "dev");
					}
				}
				""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch);
		await Assert.That(result).HasNoErrorDiagnostics();
	}

	[Test]
	public async Task Generate_GivenComputedAddViteAppPath_IsAccepted(CancellationToken cancellationToken)
	{
		// Arrange
		var source =
			JavaScriptStubs
			+ """
				namespace Testing
				{
					[HostKit]
					partial class TestingHostKit;

					[ResourceDefinition<global::Aspire.Hosting.JavaScript.ViteAppResource>("web", ResourcePath = "../web")]
					sealed partial class WebKit
					{
						protected override IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> BuildResource(IDistributedApplicationBuilder builder)
						{
							var path = "../web";
							return builder.AddViteApp(Name, path, "dev");
						}
					}
				}
				""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch)
			.Because("a computed path cannot be compared at compile time, so it must not warn");
	}

	[Test]
	public async Task Generate_GivenProjectPathOnJavaScriptApp_ReportsProjectPathRequiresDotnetProjectResource(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			JavaScriptStubs
			+ """
				namespace Testing
				{
					[HostKit]
					partial class TestingHostKit;

					[ResourceDefinition<global::Aspire.Hosting.JavaScript.ViteAppResource>("web", ProjectPath = "../web")]
					sealed partial class WebKit
					{
						protected override IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> BuildResource(IDistributedApplicationBuilder builder) =>
							builder.AddViteApp(Name, "../web", "dev");
					}
				}
				""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ProjectPathRequiresDotnetProjectResource);
		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch)
			.Because("an ignored ProjectPath is not validated as a ResourcePath");
	}

	[Test]
	public async Task Generate_GivenResourcePathOnNonPathResource_ReportsResourcePathRequiresPathResource(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<global::Purview.Aspire.ResourceKit.DefaultAspireResource>("cache", ResourcePath = "../cache")]
				sealed partial class CacheKit
				{
					protected override IResourceBuilder<global::Purview.Aspire.ResourceKit.DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
						throw new global::System.NotImplementedException();
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourcePathRequiresPathResource);
		await Assert.That(result).HasNoErrorDiagnostics();
	}

	[Test]
	public async Task Generate_GivenResourcePathOnLegacyProjectType_ReportsResourcePathRequiresPathResource(
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

				[ResourceDefinition<Projects.Example_Service>(ResourcePath = "../Api/Api.csproj")]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddProject<Projects.Example_Service>(Name);
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourcePathRequiresPathResource);
		await Assert.That(result).HasNoErrorDiagnostics();
	}

	[Test]
	public async Task Generate_GivenDotnetProjectWithOnlyResourcePath_ValidatesAddDotnetProject(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<global::Aspire.Hosting.Dotnet.DotnetProjectResource>("api", ResourcePath = "../Api/Api.csproj")]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<global::Aspire.Hosting.Dotnet.DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddDotnetProject(Name, "../Other/Other.csproj");
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch);
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ResourcePathRequiresPathResource);
	}

	[Test]
	public async Task Generate_GivenMatchingResourcePathOnDotnetProject_DoesNotReport(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<global::Aspire.Hosting.Dotnet.DotnetProjectResource>("api", ResourcePath = "../Api/Api.csproj")]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<global::Aspire.Hosting.Dotnet.DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddDotnetProject(Name, "../Api/Api.csproj");
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch);
		await Assert.That(result).HasNoErrorDiagnostics();
	}

	[Test]
	public async Task Generate_GivenConflictingProjectPathAndResourcePath_ReportsProjectPathAndResourcePathConflict(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<global::Aspire.Hosting.Dotnet.DotnetProjectResource>(
					"api",
					ProjectPath = "../Old/Old.csproj",
					ResourcePath = "../Api/Api.csproj")]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<global::Aspire.Hosting.Dotnet.DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddDotnetProject(Name, "../Api/Api.csproj");
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ProjectPathAndResourcePathConflict);
		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch)
			.Because("ResourcePath wins, so its matching AddDotnetProject call satisfies SG0018");
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
			options.WithAdditionalAssemblyTypes(
				typeof(global::Aspire.Hosting.Dotnet.DotnetProjectResource),
				typeof(DefaultAspireResource)
			),
			cancellationToken
		);
	}
}
