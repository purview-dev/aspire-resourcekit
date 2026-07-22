using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

/// <summary>
///
/// </summary>
/// <param name="Writer"></param>
/// <param name="HostAppAttribute"></param>
/// <param name="AppResourceAttribute"></param>
/// <param name="ServiceLifetime"></param>
/// <param name="Options">We actually need IOptions&lt;&gt;, but Options isn't generic and int he same assembly so easier to lookup.</param>
/// <param name="OptionsServiceCollectionExtensions">Required for AddOptions</param>
/// <param name="OptionsBuilderConfigurationExtensions">Required for BindConfiguration</param>
/// <param name="SystemCANotNull"></param>
/// <param name="Logger"></param>
sealed record class GenerationContext(
	CodeWriter Writer,
	// Purview.Aspire.ResourceKit symbols
	INamedTypeSymbol? HostAppAttribute,
	INamedTypeSymbol? AppResourceAttribute,
	// Required symbols
	INamedTypeSymbol? ServiceLifetime,
	INamedTypeSymbol? Options,
	INamedTypeSymbol? OptionsServiceCollectionExtensions,
	INamedTypeSymbol? OptionsBuilderConfigurationExtensions,
	// Helper symbols
	INamedTypeSymbol? SystemCANotNull,
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
			HostAppAttribute: compilation.GetTypeByMetadataName(TypeHelpers.HostAppAttribute.SymbolFullName),
			AppResourceAttribute: compilation.GetTypeByMetadataName(TypeHelpers.AppResourceAttribute.SymbolFullName),
			// Required symbols
			ServiceLifetime: compilation.GetTypeByMetadataName(TypeHelpers.ServiceLifetime.SymbolFullName),
			Options: compilation.GetTypeByMetadataName(TypeHelpers.Options.SymbolFullName),
			OptionsServiceCollectionExtensions: compilation.GetTypeByMetadataName(
				TypeHelpers.OptionsServiceCollectionExtensions.SymbolFullName
			),
			OptionsBuilderConfigurationExtensions: compilation.GetTypeByMetadataName(
				TypeHelpers.OptionsBuilderConfigurationExtensions.SymbolFullName
			),
			// Helper symbols
			SystemCANotNull: compilation.GetTypeByMetadataName(TypeHelpers.NotNullAttribute.SymbolFullName),
			// Debugging support
			Logger: logging
		);
	}

	public IEnumerable<string> GetDebugInfo()
	{
		yield return GetState(HostAppAttribute, nameof(HostAppAttribute));
		yield return GetState(AppResourceAttribute, nameof(AppResourceAttribute));
		yield return GetState(ServiceLifetime, nameof(ServiceLifetime));
		yield return GetState(Options, nameof(Options));
		yield return GetState(OptionsServiceCollectionExtensions, nameof(OptionsServiceCollectionExtensions));
		yield return GetState(OptionsBuilderConfigurationExtensions, nameof(OptionsBuilderConfigurationExtensions));
		yield return GetState(SystemCANotNull, nameof(SystemCANotNull));
	}

	static string GetState(INamedTypeSymbol? symbol, string propertyName) =>
		$"{propertyName} is {(symbol is null ? "missing" : "available")}";
}
