using System.Collections.Immutable;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

/// <summary>
/// Resolved model for a single host app and its associated host resources,
/// ready for source emission.
/// </summary>
sealed record GeneratedHostAppModel(TargetSymbolDescriptor HostApp, ImmutableArray<TargetSymbolDescriptor> Resources);
