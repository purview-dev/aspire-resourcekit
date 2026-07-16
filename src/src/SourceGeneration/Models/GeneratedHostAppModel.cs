using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

/// <summary>
/// Resolved model for a single host app and its associated host resources,
/// ready for source emission.
/// </summary>
sealed record GeneratedHostAppModel(
	INamedTypeSymbol HostAppType,
	ImmutableArray<INamedTypeSymbol> Resources
);
