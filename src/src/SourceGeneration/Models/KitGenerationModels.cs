using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using System.Collections.Immutable;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

sealed class KitGenerationContext(Compilation compilation, GenerationSettings settings, ISourceGenLogger? logger) : GenerationContext(compilation, settings, logger)
{
	public bool HasIServiceCollection { get; } = compilation.GetTypeByMetadataName(TypeLibrary.IServiceCollection) != null;

	public bool HasConfigurationBinder { get; } = compilation.GetTypeByMetadataName(TypeLibrary.ConfigurationBinder) != null;
}

sealed record KitGenerationCollectionResults(KitGenerationContext Context, EquatableArray<GeneratorResult<KitTargetDescriptor>> HostKits) : ISourceGenLogger
{
	public GeneratorResult<KitTargetDescriptor> HostKit => HostKits.FirstOrDefault();

	public ImmutableDictionary<string, ImmutableArray<GeneratorResult<KitTargetDescriptor>>> ResourceKits { get; init; } = ImmutableDictionary<string, ImmutableArray<GeneratorResult<KitTargetDescriptor>>>.Empty;

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) => Context.Log(level, indentation, message, args);
}

readonly record struct HostKitGenerationModel(
	KitTargetDescriptor SymbolDescriptor,
	TypeIdentity HostKitType,
	TypeIdentity HostKitOptionsType,
	TypeIdentity ResourceKitBaseType,
	TypeDeclarationAccessibility AccessibilityModifier,
	string ExtensionMethodName,
	EquatableArray<ResourceKitInfo> ResourceKits
)
{
	public bool ShouldGenerateOptions => HostKitOptionsType != TypeIdentity.Empty;

	public bool HasResourceKits => !ResourceKits.IsEmpty;
}

readonly record struct ResourceKitInfo(
	KitTargetDescriptor SymbolDescriptor,
	TypeIdentity ResourceKitType,
	TypeIdentity ResourceKitOptionsType,
	TypeIdentity AspireResourceType,
	TypeDeclarationAccessibility AccessibilityModifier,
	string PropertyName,
	string? ResourceName,
	bool HasExplicitBaseType
);

sealed record KitTargetDescriptor(
	TypeReference Target,
	bool IsHostKit,
	string? Name,
	string? PropertyName,
	string? ExtensionName,
	bool GenerateOptions,
	bool IsGenericResourceDefinition,
	TypeReference? AspireResourceType,
	bool HasExplicitBaseType,
	bool IsDerivedFromExpectedBase
);
