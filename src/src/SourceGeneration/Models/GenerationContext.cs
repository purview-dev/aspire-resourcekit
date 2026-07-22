using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

sealed record class GenerationContext(
	CodeWriter Writer,
	// Purview.Aspire.ResourceIsolation symbols
	INamedTypeSymbol? HostAppAttribute,
	INamedTypeSymbol? AppResourceAttribute,
	// Required symbols
	INamedTypeSymbol? ServiceLifetime,
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
			HostAppAttribute: compilation.GetTypeByMetadataName(TypeHelpers.FullHostAppAttributeName),
			AppResourceAttribute: compilation.GetTypeByMetadataName(TypeHelpers.FullAppResourceAttributeName),
			ServiceLifetime: compilation.GetTypeByMetadataName(TypeHelpers.FullServiceLifetimeName),
			// Debugging support
			Logger: logging
		);
	}

	public string GetDebugInfo()
	{
		List<string> debugInfo = [];

		debugInfo.Add($"{nameof(HostAppAttribute)} {(HostAppAttribute is null ? "Missing" : "Available")}");
		debugInfo.Add($"{nameof(AppResourceAttribute)} {(AppResourceAttribute is null ? "Missing" : "Available")}");
		debugInfo.Add($"{nameof(ServiceLifetime)} {(ServiceLifetime is null ? "Missing" : "Available")}");

		return string.Join(", ", debugInfo);
	}
}
