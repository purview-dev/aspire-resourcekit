namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Infrastructure;

public readonly record struct GenerationDriverContext(
	bool IncludeSystemNamespaces = true,
	bool IncludeSourceGeneratorNamespaces = true,
	bool IncludeServiceTimelifeNamespace = true
)
{
	public static readonly GenerationDriverContext Default;
}
