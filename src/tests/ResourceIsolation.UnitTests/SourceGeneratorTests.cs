using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Aspire.ResourceIsolation.SourceGeneration;

namespace Purview.Aspire.ResourceIsolation;

public sealed class SourceGeneratorTests
{
	[Test]
	public async Task Generator_EmitsHostAppMembersForAssociatedResources()
	{
		const string source =
			"""
			namespace Purview.Aspire.ResourceIsolation;

			public interface IHostAppResource<THostApp> where THostApp : class
			{
			}

			public abstract class HostAppResource<THostApp, TResource> : IHostAppResource<THostApp>
				where THostApp : class
				where TResource : class
			{
			}

			public sealed class ProjectResource
			{
			}

			namespace Demo;

			[HostApp]
			public sealed partial class DemoHostApp
			{
			}

			[HostResource]
			public sealed partial class ExampleApiAppResource : HostAppResource<DemoHostApp, ProjectResource>
			{
			}

			[HostResource]
			public sealed partial class RedisAppResource : HostAppResource<DemoHostApp, ProjectResource>
			{
			}
			""";

		var runResult = RunGenerator(source);
		var generatedSources = runResult
			.Results.Single()
			.GeneratedSources.Select(x => x.SourceText.ToString())
			.ToArray();
		var hostResourceSource = generatedSources.Single(x => x.Contains("partial class DemoHostApp", StringComparison.Ordinal));

		await Assert.That(generatedSources.Length >= 1).IsTrue();
		await Assert.That(hostResourceSource).Contains("ExampleApiAppResource ExampleApi");
		await Assert.That(hostResourceSource).Contains("RedisAppResource Redis");
		await Assert.That(runResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
	}

	[Test]
	public async Task Generator_ReportsErrorForNonPartialHostApp()
	{
		const string source =
			"""
			namespace Demo;

			[Purview.Aspire.ResourceIsolation.HostApp]
			public sealed class DemoHostApp
			{
			}
			""";

		var runResult = RunGenerator(source);
		var diagnostics = runResult.Diagnostics.Concat(runResult.Results.Single().Diagnostics).ToArray();

		await Assert.That(diagnostics.Any(d => d.Id == "SG0001")).IsTrue();
	}

	[Test]
	public async Task Generator_ReportsErrorWhenMultipleHostAppsAreDeclared()
	{
		const string source =
			"""
			namespace Purview.Aspire.ResourceIsolation;

			public interface IHostAppResource<THostApp> where THostApp : class
			{
			}

			public abstract class HostAppResource<THostApp, TResource> : IHostAppResource<THostApp>
				where THostApp : class
				where TResource : class
			{
			}

			public sealed class ProjectResource
			{
			}

			namespace Demo;

			[HostApp]
			public sealed partial class HostA
			{
			}

			[HostApp]
			public sealed partial class HostB
			{
			}

			[HostResource]
			public sealed partial class ApiAppResource : HostAppResource<HostA, ProjectResource>
			{
			}
			""";

		var runResult = RunGenerator(source);
		var diagnostics = runResult.Diagnostics.Concat(runResult.Results.Single().Diagnostics).ToArray();

		await Assert.That(diagnostics.Any(d => d.Id == "SG0004")).IsTrue();
	}

	[Test]
	public async Task Generator_ReportsInfoWhenHostAppAttributeIsMissing()
	{
		const string source =
			"""
			namespace Purview.Aspire.ResourceIsolation;

			public interface IHostAppResource<THostApp> where THostApp : class
			{
			}

			public abstract class HostAppResource<THostApp, TResource> : IHostAppResource<THostApp>
				where THostApp : class
				where TResource : class
			{
			}

			public sealed class ProjectResource
			{
			}

			namespace Demo;

			public sealed partial class DemoHostApp
			{
			}

			[HostResource]
			public sealed partial class ApiAppResource : HostAppResource<DemoHostApp, ProjectResource>
			{
			}
			""";

		var runResult = RunGenerator(source);
		var diagnostics = runResult.Diagnostics.Concat(runResult.Results.Single().Diagnostics).ToArray();

		await Assert.That(diagnostics.Any(d => d.Id == "SG0002")).IsTrue();
	}

	static GeneratorDriverRunResult RunGenerator(string source)
	{
		var parseOptions = CSharpParseOptions.Default;
		var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
		var compilation = CSharpCompilation.Create(
			assemblyName: "GeneratorTests",
			syntaxTrees: [syntaxTree],
			references: GetMetadataReferences(),
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		IIncrementalGenerator generator = new HostAppGenerator();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			generators: [generator.AsSourceGenerator()],
			parseOptions: parseOptions
		);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
		return driver.GetRunResult();
	}

	static ImmutableArray<MetadataReference> GetMetadataReferences()
	{
		var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
			?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES was not available.");

		var references = tpa
			.Split(Path.PathSeparator)
			.Where(path =>
				path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
				&& !Path.GetFileName(path).StartsWith("System.Private.", StringComparison.OrdinalIgnoreCase)
			)
			.Select(path => MetadataReference.CreateFromFile(path));

		return [.. references];
	}
}
