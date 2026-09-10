using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies <see cref="ResourceKitDiagnosticSuppressor"/> suppresses <c>CS8618</c> for non-nullable
/// <c>IResourceBuilder&lt;T&gt;</c> properties on resource kits while leaving other <c>CS8618</c>
/// diagnostics untouched.
/// </summary>
public class ResourceKitDiagnosticSuppressorTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	[Test]
	public async Task Analyze_GivenResourceKitWithNonNullableResourceBuilderProperty_SuppressesCS8618(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class RedisResourceKit
			{
				public IResourceBuilder<DefaultAspireResource> Cache { get; private set; }

				protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
					throw new global::System.NotImplementedException();
			}

			public class NotAResourceKit
			{
				public IResourceBuilder<DefaultAspireResource> Cache { get; private set; }
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var diagnostics = await RunSuppressorAsync(result, cancellationToken);

		// Assert
		var suppressed = diagnostics
			.Where(diagnostic => diagnostic.Id == "CS8618" && diagnostic.IsSuppressed)
			.ToArray();
		var notSuppressed = diagnostics
			.Where(diagnostic => diagnostic.Id == "CS8618" && !diagnostic.IsSuppressed)
			.ToArray();

		await Assert.That(suppressed).IsNotEmpty();
		await Assert
			.That(suppressed.All(static diagnostic => GetEnclosingTypeName(diagnostic) == "RedisResourceKit"))
			.IsTrue();
		await Assert.That(notSuppressed).IsNotEmpty();
		await Assert
			.That(notSuppressed.All(static diagnostic => GetEnclosingTypeName(diagnostic) == "NotAResourceKit"))
			.IsTrue();
	}

	[Test]
	public async Task Analyze_GivenResourceKitWithNullableResourceBuilderProperty_DoesNotSuppressCS8618(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class RedisResourceKit
			{
				public IResourceBuilder<DefaultAspireResource>? Cache { get; private set; }

				protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
					throw new global::System.NotImplementedException();
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var diagnostics = await RunSuppressorAsync(result, cancellationToken);

		// Assert
		var cs8618InKit = diagnostics
			.Where(diagnostic => diagnostic.Id == "CS8618" && GetEnclosingTypeName(diagnostic) == "RedisResourceKit")
			.ToArray();
		await Assert.That(cs8618InKit).IsEmpty();
	}

	[Test]
	public async Task Analyze_GivenProjectResourceKitWithNonNullableResourceBuilderProperty_SuppressesCS8618(
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
					public IResourceBuilder<ProjectResource> Api { get; private set; }

					protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddProject<Projects.Example_Service>(Name);
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var diagnostics = await RunSuppressorAsync(result, cancellationToken);

		// Assert
		var suppressedInKit = diagnostics
			.Where(diagnostic =>
				diagnostic.Id == "CS8618" && diagnostic.IsSuppressed && GetEnclosingTypeName(diagnostic) == "ApiKit"
			)
			.ToArray();
		await Assert.That(suppressedInKit).IsNotEmpty();
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

	static async Task<ImmutableArray<Diagnostic>> RunSuppressorAsync(
		DriverRunResult result,
		CancellationToken cancellationToken
	)
	{
		var compilation = result.CompilationResult.Compilation;
		var compilationWithAnalyzers = compilation.WithAnalyzers(
			[new ResourceKitDiagnosticSuppressor()],
			new CompilationWithAnalyzersOptions(
				options: new AnalyzerOptions([]),
				onAnalyzerException: null,
				concurrentAnalysis: false,
				logAnalyzerExecutionTime: false,
				reportSuppressedDiagnostics: true
			)
		);

		return await compilationWithAnalyzers.GetAllDiagnosticsAsync(cancellationToken);
	}

	static string? GetEnclosingTypeName(Diagnostic diagnostic) =>
		diagnostic.Location.SourceTree is not { } tree
			? null
			: tree.GetRoot()
				.FindNode(diagnostic.Location.SourceSpan)
				.Ancestors()
				.OfType<ClassDeclarationSyntax>()
				.FirstOrDefault()
				?.Identifier.ValueText;
}
