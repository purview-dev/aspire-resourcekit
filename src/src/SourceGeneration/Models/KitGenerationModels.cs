using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

// This is the context for overal generation, it contains the compilation and settings, as well as some cached information about the compilation.
sealed record KitGenerationCapabilities(bool HasIServiceCollection, bool HasConfigurationBinder)
	: IGenerationCapabilities;

// This is the model that is passed to the code generation helpers to build the generation model. It contains the host kit symbol and the resource kit symbols.
sealed record KitGenerationModel(
	GenerationContext<KitGenerationCapabilities> Context,
	EquatableArray<GeneratorResult<HostKitModel>> HostKits,
	ImmutableArray<DiagnosticInfo> Diagnostics
)
{
	public bool HasHostKit => !HostKit.IsEmpty;

	public bool HasResourceKits => !ResourceKits.IsEmpty;

	public GeneratorResult<HostKitModel> HostKit { get; init; }

	public ImmutableDictionary<string, ImmutableArray<GeneratorResult<ResourceKitModel>>> ResourceKits { get; init; } =
		ImmutableDictionary<string, ImmutableArray<GeneratorResult<ResourceKitModel>>>.Empty;

	public (bool IsFatal, ImmutableArray<DiagnosticInfo> Diagnostics) GetAllDiagnostics()
	{
		var allDiagnostics = Diagnostics
			.Concat(
				HostKits
					.SelectMany(m => m.Diagnostics)
					.Concat(ResourceKits.SelectMany(r => r.Value.SelectMany(d => d.Diagnostics)))
			)
			.ToImmutableArray();

		var isFatal = HostKits.Any(h => !h.ShouldProcess) || ResourceKits.Any(r => r.Value.Any(d => !d.ShouldProcess));
		if (ResourceKits.IsEmpty && HasHostKit)
		{
			allDiagnostics = allDiagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.NoResourceKitsDefined,
					HostKit.Value.SyntaxRef,
					HostKit.Value.HostKitType.Name
				)
			);
		}

		return (isFatal, allDiagnostics);
	}
}

sealed record OutputContext(
	KitGenerationModel Model,
	ImmutableDictionary<string, ImmutableArray<ResourceKitModel>> ResourceKits
) : ISourceGenLogger
{
	public HostKitModel HostKit => Model.HostKit.Value;

	public int ResourceKitCount => ResourceKits.Sum(r => r.Value.Length);

	public bool HasResourceKits => ResourceKitCount > 0;

	public CodeWriter Writer { get; set; } = Model.Context.CreateCodeWriter();

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
		Model.Context.Log(level, indentation, message, args);
}

readonly record struct HostKitModel(
	TypeIdentity HostKitType,
	TypeIdentity OptionsType,
	TypeIdentity ResourceKitBaseType,
	TypeDeclarationAccessibility? Accessibility,
	string ExtensionMethodName,
	SyntaxReference SyntaxRef
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
	SyntaxReference SyntaxRef
);
