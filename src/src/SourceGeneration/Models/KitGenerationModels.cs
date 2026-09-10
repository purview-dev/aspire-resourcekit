using System.Collections.Immutable;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

// This is the context for overal generation, it contains the compilation and settings, as well as some cached information about the compilation.
sealed record KitGenerationCapabilities(bool HasIServiceCollection, bool HasConfigurationBinder)
	: IGenerationCapabilities;

// A deterministic, value-equatable grouping of resource kits that share a target namespace.
sealed record ResourceKitGroup(string Namespace, EquatableArray<GeneratorResult<ResourceKitModel>> Items);

// The unwrapped resource kit group shape used at output time.
sealed record ResourceKitModelGroup(string Namespace, EquatableArray<ResourceKitModel> Items);

// This is the model that is passed to the code generation helpers to build the generation model.
// It is intentionally free of Roslyn objects and GenerationContext so the aggregate value stays
// value-equatable and the downstream source output can be cached across unrelated changes.
sealed record KitGenerationModel(
	EquatableArray<GeneratorResult<HostKitModel>> HostKits,
	EquatableArray<DiagnosticInfo> Diagnostics
)
{
	public bool HasHostKit => !HostKit.IsEmpty;

	public bool HasResourceKits => !ResourceKits.IsEmpty;

	public GeneratorResult<HostKitModel> HostKit { get; init; }

	public EquatableArray<ResourceKitGroup> ResourceKits { get; init; } = EquatableArray<ResourceKitGroup>.Empty;

	public (bool IsFatal, EquatableArray<DiagnosticInfo> Diagnostics) GetAllDiagnostics()
	{
		var allDiagnostics = Diagnostics
			.Concat(
				HostKits
					.AsImmutableArray()
					.SelectMany(m => m.Diagnostics)
					.Concat(
						ResourceKits
							.AsImmutableArray()
							.SelectMany(r => r.Items.AsImmutableArray().SelectMany(d => d.Diagnostics))
					)
			)
			.ToImmutableArray();

		var isFatal =
			HostKits.AsImmutableArray().Any(h => !h.ShouldProcess)
			|| ResourceKits.AsImmutableArray().Any(r => r.Items.AsImmutableArray().Any(d => !d.ShouldProcess));

		return (isFatal, EquatableArray<DiagnosticInfo>.Create([.. allDiagnostics]));
	}
}

sealed record OutputContext(
	KitGenerationModel Model,
	EquatableArray<ResourceKitModelGroup> ResourceKits,
	GenerationContext<KitGenerationCapabilities> Context
) : ISourceGenLogger
{
	public HostKitModel HostKit => Model.HostKit.Value;

	public int ResourceKitCount => ResourceKits.AsImmutableArray().Sum(r => r.Items.Count);

	public bool HasResourceKits => ResourceKitCount > 0;

	public CodeWriter Writer { get; } = Context.CreateCodeWriter();

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
		Context.Log(level, indentation, message, args);
}

readonly record struct HostKitModel(
	TypeIdentity HostKitType,
	TypeIdentity OptionsType,
	TypeIdentity ResourceKitBaseType,
	TypeDeclarationAccessibility? Accessibility,
	string ExtensionMethodName
)
{
	public bool ShouldGenerateOptions => OptionsType != TypeIdentity.Empty;
}

readonly record struct ResourceKitModel(
	TypeIdentity ResourceKitType,
	TypeIdentity OptionsType,
	TypeIdentity AspireResourceType,
	TypeDeclarationAccessibility? Accessibility,
	string PropertyName,
	string ResourceName,
	bool HasExplicitBaseType
);
