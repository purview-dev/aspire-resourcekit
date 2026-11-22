using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies SG0044: a <c>BuildResource</c> override that only repeats the registration ResourceKit generates
/// is reported, and one that does anything more is not.
/// </summary>
public sealed class RedundantBuildResourceAnalyzerTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string DotnetProject = "global::Aspire.Hosting.Dotnet.DotnetProjectResource";

	const string ViteStubs = """
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
			public static class JavaScriptHostingExtensions
			{
				public static global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> AddViteApp(
					this global::Aspire.Hosting.IDistributedApplicationBuilder builder,
					string name,
					string appDirectory,
					string runScriptName = "dev") => throw new global::System.NotImplementedException();
			}
		}

		""";

	static ResourceKitSourceGeneratorTestOptions CreateOptions()
	{
		ResourceKitSourceGeneratorTestOptions baseOptions = new();
		return baseOptions with
		{
			AnalyzerTypes = [typeof(ResourceKitDiagnosticAnalyzer), typeof(RedundantBuildResourceAnalyzer)],
			AdditionalAssemblyTypes = baseOptions.AdditionalAssemblyTypes.Add(
				typeof(global::Aspire.Hosting.Dotnet.DotnetProjectResource)
			),
		};
	}

	static string DotnetKit(string attributeArguments, string body) => $$"""
		namespace Testing
		{
			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<{{DotnetProject}}>({{attributeArguments}})]
			sealed partial class ApiKit
			{
				{{body}}
			}
		}
		""";

	[Test]
	public async Task Analyze_GivenOverrideRepeatingProjectPath_ReportsRedundantBuildResource(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = DotnetKit(
			"\"api\", ProjectPath = \"../Api/Api.csproj\"",
			$"protected override IResourceBuilder<{DotnetProject}> BuildResource(IDistributedApplicationBuilder builder) => builder.AddDotnetProject(Name, \"../Api/Api.csproj\");"
		);

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.RedundantBuildResource);
		await Assert.That(result).HasNoErrorDiagnostics();
	}

	[Test]
	public async Task Analyze_GivenOverrideWithoutDeclaredPath_ReportsRedundantBuildResource(
		CancellationToken cancellationToken
	)
	{
		// Arrange — the path lives only in the call; the fix moves it to the attribute.
		var source = DotnetKit(
			"\"api\"",
			$$"""
			protected override IResourceBuilder<{{DotnetProject}}> BuildResource(IDistributedApplicationBuilder builder)
			{
				return builder.AddDotnetProject(Name, "../Api/Api.csproj");
			}
			"""
		);

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.RedundantBuildResource);
	}

	[Test]
	public async Task Analyze_GivenOverrideThatConfiguresTheResource_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		var source = DotnetKit(
			"\"api\", ProjectPath = \"../Api/Api.csproj\"",
			$"protected override IResourceBuilder<{DotnetProject}> BuildResource(IDistributedApplicationBuilder builder) => builder.AddDotnetProject(Name, \"../Api/Api.csproj\").WithHttpEndpoint();"
		);

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RedundantBuildResource);
	}

	[Test]
	public async Task Analyze_GivenOverrideWithLiteralName_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange — a literal name could differ from Name once options rename the resource.
		var source = DotnetKit(
			"\"api\", ProjectPath = \"../Api/Api.csproj\"",
			$"protected override IResourceBuilder<{DotnetProject}> BuildResource(IDistributedApplicationBuilder builder) => builder.AddDotnetProject(\"api\", \"../Api/Api.csproj\");"
		);

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RedundantBuildResource);
	}

	[Test]
	public async Task Analyze_GivenOverrideWithDifferentPath_ReportsMismatchNotRedundancy(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = DotnetKit(
			"\"api\", ProjectPath = \"../Api/Api.csproj\"",
			$"protected override IResourceBuilder<{DotnetProject}> BuildResource(IDistributedApplicationBuilder builder) => builder.AddDotnetProject(Name, \"../Other/Other.csproj\");"
		);

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ProjectDefinitionMismatch);
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RedundantBuildResource);
	}

	[Test]
	public async Task Analyze_GivenComputedPath_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		var source = DotnetKit(
			"\"api\"",
			$$"""
			protected override IResourceBuilder<{{DotnetProject}}> BuildResource(IDistributedApplicationBuilder builder) =>
				builder.AddDotnetProject(Name, global::System.IO.Path.Combine("..", "Api", "Api.csproj"));
			"""
		);

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RedundantBuildResource);
	}

	[Test]
	public async Task Analyze_GivenGeneratedBuildResource_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = $$"""
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<{{DotnetProject}}>("api", ProjectPath = "../Api/Api.csproj")]
				sealed partial class ApiKit;
			}
			""";

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RedundantBuildResource);
		await Assert.That(result).HasNoErrorDiagnostics();
	}

	[Test]
	public async Task Analyze_GivenAddViteAppWithDefaultScript_ReportsRedundantBuildResource(
		CancellationToken cancellationToken
	)
	{
		// Arrange — passing the default explicitly is still the generated call.
		var source =
			ViteStubs
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
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.RedundantBuildResource);
	}

	[Test]
	public async Task Analyze_GivenAddViteAppWithCustomScript_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		var source =
			ViteStubs
			+ """
				namespace Testing
				{
					[HostKit]
					partial class TestingHostKit;

					[ResourceDefinition<global::Aspire.Hosting.JavaScript.ViteAppResource>("web", ResourcePath = "../web")]
					sealed partial class WebKit
					{
						protected override IResourceBuilder<global::Aspire.Hosting.JavaScript.ViteAppResource> BuildResource(IDistributedApplicationBuilder builder) =>
							builder.AddViteApp(Name, "../web", "start");
					}
				}
				""";

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RedundantBuildResource);
	}
}
