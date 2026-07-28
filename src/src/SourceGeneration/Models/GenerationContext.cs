using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

/// <summary>
///
/// </summary>
/// <param name="Writer"></param>
/// <param name="HostKitAttribute"></param>
/// <param name="ResourceDefinitionAttribute"></param>
/// <param name="GenericResourceDefinitionAttribute"></param>
/// <param name="IServiceCollection"></param>
/// <param name="ConfigurationBinder">Required for IConfiguration.Get&lt;T&gt; options binding support.</param>
/// <param name="Logger"></param>
sealed record class GenerationContext(
	CodeWriter Writer,
	// Purview.Aspire.ResourceKit symbols
	INamedTypeSymbol? HostKitAttribute,
	INamedTypeSymbol? ResourceDefinitionAttribute,
	INamedTypeSymbol? GenericResourceDefinitionAttribute,
	// Required symbols
	INamedTypeSymbol? IServiceCollection,
	INamedTypeSymbol? ConfigurationBinder,
	// Debugging support
	GenerationLogger? Logger
)
{
	public static GenerationContext Create(
		Compilation compilation,
		GenerationLogger? logging,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		return new(
			Writer: new(),
			HostKitAttribute: compilation.GetTypeByMetadataName(TypeHelpers.HostKitAttribute.SymbolFullName),
			ResourceDefinitionAttribute: compilation.GetTypeByMetadataName(
				TypeHelpers.ResourceDefinitionAttribute.SymbolFullName
			),
			GenericResourceDefinitionAttribute: compilation.GetTypeByMetadataName(
				TypeHelpers.GenericResourceDefinitionAttribute.SymbolFullName
			),
			// Required symbols
			IServiceCollection: compilation.GetTypeByMetadataName(TypeHelpers.IServiceCollection.SymbolFullName),
			ConfigurationBinder: compilation.GetTypeByMetadataName(TypeHelpers.ConfigurationBinder.SymbolFullName),
			// Debugging support
			Logger: logging
		);
	}

	public IEnumerable<string> GetDebugInfo()
	{
		yield return GetState(HostKitAttribute, nameof(HostKitAttribute));
		yield return GetState(ResourceDefinitionAttribute, nameof(ResourceDefinitionAttribute));
		yield return GetState(GenericResourceDefinitionAttribute, nameof(GenericResourceDefinitionAttribute));
		yield return GetState(IServiceCollection, nameof(IServiceCollection));
		yield return GetState(ConfigurationBinder, nameof(ConfigurationBinder));
	}

	static string GetState(INamedTypeSymbol? symbol, string propertyName) =>
		$"{propertyName} is {(symbol is null ? "missing" : "available")}";
}
