using System.Collections.Immutable;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

sealed record GenerationModel(
	bool IsSourceGeneratorEnabled,
	GenerationContext GenerationContext,
	GeneratorResult<TargetSymbolDescriptor> HostKit,
	ImmutableArray<GeneratorResult<TargetSymbolDescriptor>> ResourceKits,
	ImmutableArray<DiagnosticInfo> Diagnostics
);
