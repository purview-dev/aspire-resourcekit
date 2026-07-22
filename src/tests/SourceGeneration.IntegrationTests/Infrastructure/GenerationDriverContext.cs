namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Infrastructure;

public sealed record GenerationDriverContext(
	bool IncludeSystemNamespaces = true,
	bool IncludeSourceGeneratorNamespaces = true,
	bool IncludeServiceTimelifeNamespace = true,
	bool ThrowOnGenerationException = true
)
{
	public static readonly GenerationDriverContext Default = new();

	public static readonly GenerationDriverContext DoNotThrowOnGenerationException = new(
		ThrowOnGenerationException: false
	);
}
