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
	EquatableArray<ReportableDiagnostic> Diagnostics
)
{
	public bool HasHostKit => !HostKit.IsEmpty;

	public bool HasResourceKits => !ResourceKits.IsEmpty;

	public GeneratorResult<HostKitModel> HostKit { get; init; }

	public EquatableArray<ResourceKitGroup> ResourceKits { get; init; } = EquatableArray<ResourceKitGroup>.Empty;

	public EquatableArray<GeneratorResult<ResourceGroupModel>> ResourceGroups { get; init; } =
		EquatableArray<GeneratorResult<ResourceGroupModel>>.Empty;

	public (bool IsFatal, EquatableArray<ReportableDiagnostic> Diagnostics) GetAllDiagnostics()
	{
		var allDiagnostics = Diagnostics
			.Concat(
				HostKits
					.AsImmutableArray()
					.SelectMany(static m => m.Diagnostics)
					.Concat(
						ResourceKits
							.AsImmutableArray()
							.SelectMany(static r => r.Items.AsImmutableArray().SelectMany(static d => d.Diagnostics))
					)
					.Concat(ResourceGroups.AsImmutableArray().SelectMany(static g => g.Diagnostics))
			)
			.ToImmutableArray();

		var isFatal =
			HostKits.AsImmutableArray().Any(static h => !h.ShouldProcess)
			|| ResourceKits
				.AsImmutableArray()
				.Any(static r => r.Items.AsImmutableArray().Any(static d => !d.ShouldProcess))
			|| ResourceGroups.AsImmutableArray().Any(static g => !g.ShouldProcess);

		return (isFatal, EquatableArray<ReportableDiagnostic>.Create([.. allDiagnostics]));
	}
}

sealed record OutputContext(
	KitGenerationModel Model,
	EquatableArray<ResourceKitModelGroup> ResourceKits,
	// Ordered parents before their children.
	EquatableArray<ResourceGroupModel> ResourceGroups,
	GenerationContext<KitGenerationCapabilities> Context
) : ISourceGenLogger
{
	public HostKitModel HostKit => Model.HostKit.Value;

	public int ResourceKitCount => ResourceKits.AsImmutableArray().Sum(static r => r.Items.Count);

	public bool HasResourceKits => ResourceKitCount > 0;

	public bool HasResourceGroups => !ResourceGroups.IsEmpty;

	public CodeWriter Writer { get; } = Context.CreateCodeWriter();

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
		Context.Log(level, indentation, message, args);
}

readonly record struct HostKitModel(
	TypeIdentity HostKitType,
	TypeIdentity OptionsType,
	TypeIdentity ResourceKitBaseType,
	TypeDeclarationAccessibility? Accessibility,
	string ExtensionMethodName,
	string? Mode
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
	bool HasExplicitBaseType,
	string? Mode,
	// The groups the kit belongs to, primary group first.
	EquatableArray<TypeIdentity> Groups,
	EquatableArray<UrlModel> Urls
);

readonly record struct ResourceGroupModel(
	TypeIdentity GroupType,
	TypeIdentity OptionsType,
	// TypeIdentity.Empty for a root group.
	TypeIdentity ParentType,
	TypeDeclarationAccessibility? Accessibility,
	string Name,
	string PropertyName,
	bool ShowInDashboard,
	string? Mode,
	EquatableArray<UrlModel> Urls
)
{
	public bool HasParent => ParentType != TypeIdentity.Empty;
}

// A [ResourceUrl] (Url set) or an [EndpointUrl] (Url null). PropertyName is the generated options member.
readonly record struct UrlModel(
	string PropertyName,
	string? Url,
	string? DisplayText,
	string? Endpoint,
	string? DisplayLocation
);
