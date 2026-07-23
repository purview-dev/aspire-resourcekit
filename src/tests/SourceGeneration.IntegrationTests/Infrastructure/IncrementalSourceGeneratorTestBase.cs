using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Infrastructure;

public abstract class IncrementalSourceGeneratorTestBase<TGenerator>
	where TGenerator : class, IIncrementalGenerator, new()
{
	static readonly string[] TrustedAssemblies = (
		(string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? ""
	).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

	// +1 is for the EmbeddedAttribute
	public static readonly int ExpectedGeneratedFileCount = TypeHelpers.GeneratedTypes.Length + 1;

	public static readonly int ExpectedFileCountPlusGen = ExpectedGeneratedFileCount + 1;

	public const int HintNameHashHexLength = 16;

	public const string GeneratedSourceFileSuffix = ".g.cs";

	protected async Task<(GeneratorDriverRunResult Result, Compilation OutputCompilation)> GenerateAsync(
		string source,
		GenerationDriverContext driverContext,
		CancellationToken cancellationToken
	)
	{
		List<string> namespacesToInclude = [];
		if (driverContext.IncludeSystemNamespaces)
		{
			namespacesToInclude.AddRange([
				"// System namespaces",
				typeof(string).Namespace!,
				typeof(IEnumerable<>).Namespace!,
				typeof(Enumerable).Namespace!,
			]);
		}

		if (driverContext.IncludeSourceGeneratorNamespaces)
			namespacesToInclude.AddRange(["// Source generator namespaces", TypeHelpers.ResourceKitNamespace]);

		if (
			driverContext.IncludeServiceTimelifeReference
			|| driverContext.IncludeOptionsReference
			|| driverContext.IncludeOptionsConfigurationExtensionReference
		)
		{
			namespacesToInclude.Add("// NuGet package namespaces");
		}

		if (driverContext.IncludeOptionsReference)
			namespacesToInclude.Add("Microsoft.Extensions.Options");
		if (driverContext.IncludeOptionsConfigurationExtensionReference)
			namespacesToInclude.Add(TypeHelpers.ConfigurationBinder.Namespace);

		if (namespacesToInclude.Count > 0)
		{
			source =
				string.Join('\n', namespacesToInclude.Select(ns => ns.StartsWith('/') ? ns : $"using {ns};"))
				+ '\n'
				+ source;
		}

		var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
		var references = BuildBclReferences();
		if (driverContext.IncludeServiceTimelifeReference)
		{
			references = references.Add(
				MetadataReference.CreateFromFile(
					typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly.Location
				)
			);
		}

		if (driverContext.IncludeOptionsReference)
		{
			references = references.Add(
				MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Options.IOptions<>).Assembly.Location)
			);
		}

		if (driverContext.IncludeOptionsConfigurationExtensionReference)
		{
			references = references.Add(
				MetadataReference.CreateFromFile(
					typeof(Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions)
						.Assembly
						.Location
				)
			);

			references = references.Add(
				MetadataReference.CreateFromFile(typeof(ConfigurationBinder).Assembly.Location)
			);
		}

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		TGenerator generator = new();

		if (generator is ILogSupport logging && TestContext.Current is not null)
		{
			logging.SetLogOutput(
				(message, outputType) =>
				{
					var prefix = outputType switch
					{
						OutputType.Diagnostic => "DIA",
						OutputType.Debug => "DBG",
						OutputType.Info => "INF",
						OutputType.Warning => "WRN",
						OutputType.Error => "ERR",
						_ => "???",
					};

					TestContext.Current.OutputWriter.WriteLine($"{prefix}: {message}");

					if (driverContext.ThrowOnGenerationException && outputType == OutputType.Error)
						throw new InvalidOperationException($"Generator logged error: {message}");
				}
			);
		}

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		if (driverContext.DisableSourceGenerator is bool disable)
		{
			driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(disable));
		}

		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out var diagnostics,
			cancellationToken
		);

		var result = driver.GetRunResult();

		// No generator exceptions
		if (driverContext.ThrowOnGenerationException)
		{
			var generationExceptions = result.Results.Select(m => m.Exception).Where(e => e != null);

			await Assert.That(generationExceptions).IsEmpty();
		}

		return (result, outputCompilation);
	}

	static ImmutableArray<MetadataReference> BuildBclReferences() =>
		[.. TrustedAssemblies.Select(p => MetadataReference.CreateFromFile(p))];

	protected async Task<(GeneratorDriverRunResult Result, Compilation OutputCompilation)> GenerateAsync(
		string source,
		CancellationToken cancellationToken
	) => await GenerateAsync(source, GenerationDriverContext.Default, cancellationToken);

	protected async Task<Assembly> CompileToAssemblyAsync(string source, CancellationToken cancellationToken)
	{
		var (_, compilation) = await GenerateAsync(source, cancellationToken);
		await using MemoryStream assemblyStream = new();
		var emitResult = compilation.Emit(assemblyStream, cancellationToken: cancellationToken);
		if (!emitResult.Success)
		{
			var diagnostics = string.Join(
				Environment.NewLine,
				emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString())
			);

			throw new InvalidOperationException(diagnostics);
		}

		assemblyStream.Position = 0;
		return System.Reflection.Assembly.Load(assemblyStream.ToArray());
	}

	protected static IEnumerable<SyntaxTree> ExcludeGenAttribs(GeneratorDriverRunResult result)
	{
		return result.GeneratedTrees.Where(tree =>
			!TypeHelpers.GeneratedTypes.Any(attr =>
				tree.FilePath.EndsWith(attr.SymbolFullName + ".g.cs", StringComparison.Ordinal)
				|| tree.FilePath.EndsWith(TypeHelpers.EmbeddedAttribute.TypeName + ".cs", StringComparison.Ordinal)
			)
		);
	}

	/// <summary>
	/// Helper to get the generated source text for the generated content, excluding attributes.
	/// </summary>
	public string GetGeneratedSource(GeneratorDriverRunResult result)
	{
		var genTree = ExcludeGenAttribs(result).FirstOrDefault();

		return genTree?.GetText().ToString() ?? string.Empty;
	}

	public Diagnostic[] GetGeneratorDiagnostics(GeneratorDriverRunResult result) =>
		[.. result.Results.SelectMany(static generatorResult => generatorResult.Diagnostics).OrderBy(static d => d.Id)];
}
