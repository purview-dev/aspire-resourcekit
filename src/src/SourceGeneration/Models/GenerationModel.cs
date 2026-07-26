using System.Collections.Immutable;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

sealed record GenerationModel(
	bool IsSourceGeneratorEnabled,
	GenerationContext GenerationContext,
	GeneratorResult<TargetSymbolDescriptor> HostApp,
	ImmutableArray<GeneratorResult<TargetSymbolDescriptor>> AppResources,
	ImmutableArray<DiagnosticInfo> Diagnostics
);
