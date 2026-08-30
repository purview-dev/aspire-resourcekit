using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

public sealed class GeneratorCachingTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	[Test]
	public async Task GeneratedText_QuoteLiteral_EscapesCSharpCharacters()
	{
		// Arrange
		const string value = "quote \" slash \\ newline\n";

		// Act
		var escaped = GeneratedText.QuoteLiteral(value);

		// Assert
		await Assert.That(escaped).IsEqualTo("\"quote \\\" slash \\\\ newline\\n\"");
	}

	[Test]
	public async Task HintNameHelper_ForHost_DistinguishesNestedAndGenericIdentities()
	{
		// Arrange
		const string nestedType = "Testing.Outer+Inner`1";
		const string genericType = "Testing.Outer_Inner_1";

		// Act
		var nestedHint = HintNameHelper.ForHost(nestedType);
		var genericHint = HintNameHelper.ForHost(genericType);

		// Assert
		await Assert.That(nestedHint).IsNotEqualTo(genericHint);
		await Assert.That(nestedHint).Contains("Testing.Outer_Inner_1");
		await Assert.That(nestedHint).EndsWith(".g.cs");
	}

	[Test]
	public async Task Generate_GivenUnrelatedSourceChange_HostKitOutputIsReusedFromCache(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var sources = TestHelper.GenerateSources().ToArray();
		var harnessCompilation = (await GenerateAsync(sources, cancellationToken)).CompilationResult.Compilation;

		var parseOptions = (CSharpParseOptions)harnessCompilation.SyntaxTrees.First().Options;
		const string Usings =
			"using Purview.Aspire.ResourceKit;\n"
			+ "using Aspire.Hosting.ApplicationModel;\n"
			+ "using Aspire.Hosting;\n"
			+ "using Purview.Aspire.ResourceKit.SourceGeneration.Infrastructure;\n"
			+ "using Microsoft.Extensions.DependencyInjection;\n"
			+ "using Microsoft.Extensions.Options;\n"
			+ "using Microsoft.Extensions.Configuration;\n";
		var userTrees = sources
			.Select(
				(source, index) => CSharpSyntaxTree.ParseText(Usings + source, parseOptions, path: $"Source{index}.cs")
			)
			.ToArray();
		var cleanCompilation = CSharpCompilation.Create(
			harnessCompilation.AssemblyName,
			userTrees,
			harnessCompilation.References,
			(CSharpCompilationOptions)harnessCompilation.Options
		);
		var driver = CreateTrackingDriver(parseOptions);

		// Act
		var updatedDriver = RunGenerator(driver, cleanCompilation, cancellationToken);
		var initialResult = updatedDriver.GetRunResult();
		var hostKitHint = FindHostKitHintName(initialResult);
		var initialSource = GetGeneratedSource(initialResult, hostKitHint);

		// An unrelated change that does not touch any attributed type.
		var unrelatedTree = CSharpSyntaxTree.ParseText(
			"namespace Testing; public sealed class UnrelatedToGeneration { }",
			path: "Unrelated.cs",
			options: parseOptions,
			cancellationToken: cancellationToken
		);
		var cachedResult = RunGenerator(
				updatedDriver,
				cleanCompilation.AddSyntaxTrees(unrelatedTree),
				cancellationToken
			)
			.GetRunResult();

		// Assert
		var cachedReason = GetOutputReason(cachedResult);
		await Assert
			.That(cachedReason)
			.IsEqualTo(IncrementalStepRunReason.Unchanged)
			.Or.IsEqualTo(IncrementalStepRunReason.Cached);
		await Assert.That(GetGeneratedSource(cachedResult, hostKitHint)).IsEqualTo(initialSource);
	}

	static CSharpGeneratorDriver CreateTrackingDriver(CSharpParseOptions parseOptions) =>
		CSharpGeneratorDriver.Create(
			[new HostKitGenerator().AsSourceGenerator()],
			parseOptions: parseOptions,
			driverOptions: new GeneratorDriverOptions(
				IncrementalGeneratorOutputKind.None,
				trackIncrementalGeneratorSteps: true
			)
		);

	static GeneratorDriver RunGenerator(
		GeneratorDriver driver,
		Compilation compilation,
		CancellationToken cancellationToken
	) => driver.RunGenerators(compilation, cancellationToken);

	static string FindHostKitHintName(GeneratorDriverRunResult runResult) =>
		runResult
			.Results[0]
			.GeneratedSources.First(source => source.HintName.Contains("AspireResourceKit.", StringComparison.Ordinal))
			.HintName;

	static string GetGeneratedSource(GeneratorDriverRunResult runResult, string hintName) =>
		runResult.Results[0].GeneratedSources.First(source => source.HintName == hintName).SourceText.ToString();

	static IncrementalStepRunReason GetOutputReason(GeneratorDriverRunResult runResult) =>
		// There is a single RegisterSourceOutput step; the host kit source is its only output.
		runResult.Results[0].TrackedOutputSteps.TryGetValue("SourceOutput", out var steps)
			? steps.SelectMany(step => step.Outputs).Select(output => output.Reason).FirstOrDefault()
			: IncrementalStepRunReason.New;
}
