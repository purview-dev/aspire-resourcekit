using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies that a kit declaring <c>ProjectPath</c>/<c>ResourcePath</c> without a <c>BuildResource</c>
/// override gets one generated, so the path is stated once, and that a hand-written or inherited override is
/// never replaced.
/// </summary>
/// <remarks>
/// The JavaScript types are declared in the test source, as in <see cref="ResourcePathDefinitionTests"/>,
/// because the generator resolves them by metadata name.
/// </remarks>
public sealed class GeneratedBuildResourceTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string GeneratedDotnetCall =
		"global::Aspire.Hosting.DotnetProjectHostingExtensions.AddDotnetProject(builder, Name, ";

	const string JavaScriptStubs = """
		namespace Aspire.Hosting.JavaScript
		{
			public class JavaScriptAppResource : global::Aspire.Hosting.ApplicationModel.IResource
			{
				public string Name => "app";

				public global::Aspire.Hosting.ApplicationModel.ResourceAnnotationCollection Annotations => [];
			}

			public class ViteAppResource : JavaScriptAppResource;

			public class NodeAppResource : JavaScriptAppResource;
		}

		namespace Aspire.Hosting
		{
			public static class JavaScriptHostingExtensions
			{
				public static global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> AddViteApp(
					this global::Aspire.Hosting.IDistributedApplicationBuilder builder,
					string name,
					string appDirectory,
					string runScriptName = "dev") => throw new global::System.NotImplementedException();

				public static global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.JavaScript.NodeAppResource> AddNodeApp(
					this global::Aspire.Hosting.IDistributedApplicationBuilder builder,
					string name,
					string appDirectory,
					string scriptPath) => throw new global::System.NotImplementedException();
			}
		}

		""";

	[Test]
	public async Task Generate_GivenProjectPathWithoutBuildResource_GeneratesAddDotnetProject(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<global::Aspire.Hosting.Dotnet.DotnetProjectResource>("api", ProjectPath = "../Api/Api.csproj")]
				sealed partial class ApiKit;
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch)
			.Because("the generated BuildResource adds the declared path");

		var generated = result.GetSource();
		await Assert
			.That(generated)
			.Contains(
				"protected override global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.Dotnet.DotnetProjectResource> BuildResource("
			);
		await Assert.That(generated).Contains(GeneratedDotnetCall + "\"../Api/Api.csproj\")");
	}

	[Test]
	public async Task Generate_GivenConstantProjectPath_GeneratesTheConstantValue(CancellationToken cancellationToken)
	{
		// Arrange — the shape AppHostProjects produces.
		const string source = """
			namespace Testing
			{
				internal static class AppHostProjects
				{
					internal static class Api
					{
						public const string Path = "../Api/Api.csproj";
					}
				}

				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<global::Aspire.Hosting.Dotnet.DotnetProjectResource>("api", ProjectPath = AppHostProjects.Api.Path)]
				sealed partial class ApiKit;
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.GetSource()).Contains(GeneratedDotnetCall + "\"../Api/Api.csproj\")");
	}

	[Test]
	public async Task Generate_GivenResourcePathOnDotnetProject_GeneratesFromResourcePath(
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
				sealed partial class ApiKit;
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.GetSource()).Contains(GeneratedDotnetCall + "\"../Api/Api.csproj\")");
	}

	[Test]
	public async Task Generate_GivenExplicitDotnetProjectBase_GeneratesAddDotnetProject(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition("api", ProjectPath = "../Api/Api.csproj")]
				sealed partial class ApiKit : ResourceKitBase<global::Aspire.Hosting.Dotnet.DotnetProjectResource>;
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.GetSource()).Contains(GeneratedDotnetCall + "\"../Api/Api.csproj\")");
	}

	[Test]
	public async Task Generate_GivenHandWrittenBuildResource_DoesNotGenerateOne(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<global::Aspire.Hosting.Dotnet.DotnetProjectResource>("api", ProjectPath = "../Api/Api.csproj")]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<global::Aspire.Hosting.Dotnet.DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddDotnetProject(Name, "../Api/Api.csproj").WithHttpEndpoint();
				}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert
			.That(result)
			.HasNoErrorDiagnostics()
			.Because("a generated override alongside the hand-written one would be CS0111");
		await Assert.That(result.GetSource()).DoesNotContain(GeneratedDotnetCall);
	}

	[Test]
	public async Task Generate_GivenViteAppResourcePath_GeneratesAddViteApp(CancellationToken cancellationToken)
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
					sealed partial class WebKit;
				}
				""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch);
		await Assert
			.That(result.GetSource())
			.Contains("global::Aspire.Hosting.JavaScriptHostingExtensions.AddViteApp(builder, Name, \"../web\")")
			.Because("the run script is left at its \"dev\" default");
	}

	[Test]
	public async Task Generate_GivenNodeAppResourcePath_DoesNotGenerateBuildResource(
		CancellationToken cancellationToken
	)
	{
		// Arrange — AddNodeApp needs a script path the attribute cannot supply.
		var source =
			JavaScriptStubs
			+ """
				namespace Testing
				{
					[HostKit]
					partial class TestingHostKit;

					[ResourceDefinition<global::Aspire.Hosting.JavaScript.NodeAppResource>("node", ResourcePath = "../node")]
					sealed partial class NodeKit
					{
						protected override IResourceBuilder<global::Aspire.Hosting.JavaScript.NodeAppResource> BuildResource(IDistributedApplicationBuilder builder) =>
							builder.AddNodeApp(Name, "../node", "server.js");
					}
				}
				""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.GetSource()).DoesNotContain("JavaScriptHostingExtensions.AddNodeApp(builder");
	}

	protected override ResourceKitSourceGeneratorTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		ResourceKitSourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	)
	{
		return base.OnBeforeRun(
			sources,
			options.WithAdditionalAssemblyTypes(typeof(global::Aspire.Hosting.Dotnet.DotnetProjectResource)),
			cancellationToken
		);
	}
}
