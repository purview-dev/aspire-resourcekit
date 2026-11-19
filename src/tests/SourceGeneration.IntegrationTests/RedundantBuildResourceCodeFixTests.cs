using Purview.Aspire.ResourceKit.SourceGeneration.CodeFixes;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// The references the SG0044 code fix tests compile against. The code fix runner does not run the source
/// generator, so the tests declare the <c>[ResourceDefinition]</c> attribute shape themselves.
/// </summary>
public sealed record RedundantBuildResourceCodeFixTestOptions : CodeFixTestOptions
{
	public RedundantBuildResourceCodeFixTestOptions()
	{
		AdditionalAssemblyTypes =
		[
			typeof(ResourceKitBase<,>),
			typeof(IHostKit),
			typeof(global::Aspire.Hosting.ApplicationModel.IResource),
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder),
			typeof(global::Aspire.Hosting.Dotnet.DotnetProjectResource),
		];
		AdditionalNamespaces =
		[
			typeof(ResourceKitBase<,>).Namespace!,
			typeof(global::Aspire.Hosting.ApplicationModel.IResource).Namespace!,
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder).Namespace!,
			typeof(global::Aspire.Hosting.Dotnet.DotnetProjectResource).Namespace!,
		];
	}
}

/// <summary>
/// Verifies that the SG0044 code fix deletes a redundant <c>BuildResource</c>, first moving its path into the
/// attribute when the attribute does not declare one.
/// </summary>
public sealed class RedundantBuildResourceCodeFixTests
	: TUnitCodeFixTestBase<
		RedundantBuildResourceAnalyzer,
		RedundantBuildResourceCodeFixProvider,
		RedundantBuildResourceCodeFixTestOptions
	>
{
	// The generated attribute's shape: the generic form derives from the non-generic one.
	const string Declarations = """
		namespace Purview.Aspire.ResourceKit
		{
			[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
			public class ResourceDefinitionAttribute : global::System.Attribute
			{
				public ResourceDefinitionAttribute() { }

				public ResourceDefinitionAttribute(string name) => Name = name;

				public string? Name { get; set; }

				public string? ProjectPath { get; set; }

				public string? ResourcePath { get; set; }
			}

			public sealed class ResourceDefinitionAttribute<TResource> : ResourceDefinitionAttribute
				where TResource : class
			{
				public ResourceDefinitionAttribute() { }

				public ResourceDefinitionAttribute(string name) : base(name) { }
			}
		}

		namespace Testing
		{
			public sealed class FakeHost : IHostKit
			{
				public void Build(IDistributedApplicationBuilder builder) { }
				public void Configure() { }
			}

			internal static class AppHostProjects
			{
				internal static class Api
				{
					public const string Path = "../Api/Api.csproj";
				}
			}
		}

		""";

	static string Kit(string attributeArguments, string body) =>
		Declarations
		+ $$"""
			namespace Testing
			{
				[ResourceDefinition<DotnetProjectResource>({{attributeArguments}})]
				public sealed class ApiKit : ResourceKitBase<FakeHost, DotnetProjectResource>
				{
					public ApiKit(FakeHost host) : base(host, "api") { }

					{{body}}
				}
			}
			""";

	[Test]
	public async Task ApplyCodeFix_GivenDeclaredProjectPath_RemovesTheOverride(CancellationToken cancellationToken)
	{
		// Arrange
		var source = Kit(
			"\"api\", ProjectPath = AppHostProjects.Api.Path",
			"protected override IResourceBuilder<DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) => builder.AddDotnetProject(Name, AppHostProjects.Api.Path);"
		);

		// Act
		var result = await ApplyCodeFixAsync(source, new RedundantBuildResourceCodeFixTestOptions(), cancellationToken);

		// Assert
		var fixedCode = (await result.FixedCode().Trees.First().GetTextAsync(cancellationToken)).ToString();

		await Assert.That(fixedCode).DoesNotContain("BuildResource");
		await Assert
			.That(fixedCode)
			.Contains("[ResourceDefinition<DotnetProjectResource>(\"api\", ProjectPath = AppHostProjects.Api.Path)]");
	}

	[Test]
	public async Task ApplyCodeFix_GivenConstantPathWithoutDeclaration_MovesTheConstantToProjectPath(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = Kit(
			"\"api\"",
			"protected override IResourceBuilder<DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) => builder.AddDotnetProject(Name, AppHostProjects.Api.Path);"
		);

		// Act
		var result = await ApplyCodeFixAsync(source, new RedundantBuildResourceCodeFixTestOptions(), cancellationToken);

		// Assert
		var fixedCode = (await result.FixedCode().Trees.First().GetTextAsync(cancellationToken)).ToString();

		await Assert.That(fixedCode).DoesNotContain("BuildResource");
		await Assert
			.That(fixedCode)
			.Contains("[ResourceDefinition<DotnetProjectResource>(\"api\", ProjectPath = AppHostProjects.Api.Path)]")
			.Because("the constant is qualified for the attribute scope, then simplified back");
	}

	[Test]
	public async Task ApplyCodeFix_GivenLiteralPathWithoutDeclaration_MovesTheLiteralToProjectPath(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = Kit(
			"\"api\"",
			"""
			protected override IResourceBuilder<DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder)
			{
				return builder.AddDotnetProject(Name, "../Api/Api.csproj");
			}
			"""
		);

		// Act
		var result = await ApplyCodeFixAsync(source, new RedundantBuildResourceCodeFixTestOptions(), cancellationToken);

		// Assert
		var fixedCode = (await result.FixedCode().Trees.First().GetTextAsync(cancellationToken)).ToString();

		await Assert.That(fixedCode).DoesNotContain("BuildResource");
		await Assert
			.That(fixedCode)
			.Contains("[ResourceDefinition<DotnetProjectResource>(\"api\", ProjectPath = \"../Api/Api.csproj\")]");
	}

	[Test]
	public async Task ApplyCodeFix_GivenKitConstantPath_QualifiesItForTheAttribute(CancellationToken cancellationToken)
	{
		// Arrange — the attribute may sit in another partial file, so the constant is qualified and the
		// simplifier shortens it as far as the attribute's scope allows.
		var source = Kit(
			"\"api\"",
			"""
			const string ApiPath = "../Api/Api.csproj";

			protected override IResourceBuilder<DotnetProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
				builder.AddDotnetProject(Name, ApiPath);
			"""
		);

		// Act
		var result = await ApplyCodeFixAsync(source, new RedundantBuildResourceCodeFixTestOptions(), cancellationToken);

		// Assert
		var fixedCode = (await result.FixedCode().Trees.First().GetTextAsync(cancellationToken)).ToString();

		await Assert.That(fixedCode).DoesNotContain("BuildResource");
		await Assert
			.That(fixedCode)
			.Contains("[ResourceDefinition<DotnetProjectResource>(\"api\", ProjectPath = ApiPath)]");
	}
}
