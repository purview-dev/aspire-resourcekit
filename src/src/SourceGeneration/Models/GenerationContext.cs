using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

sealed record class GenerationContext(
	CodeWriter Writer,
	// Purview.Aspire.ResourceIsolation symbols
	INamedTypeSymbol? HostAppAttribute,
	INamedTypeSymbol? HostResourceAttribute,
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
			HostResourceAttribute: compilation.GetTypeByMetadataName(TypeHelpers.FullHostResourceAttributeName),
			// Debugging support
			Logger: logging
		);
	}
}
