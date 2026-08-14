using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

sealed record KitGenerationModel(
	bool IsSourceGeneratorEnabled,
	KitGenerationContext GenerationContext
)
{
	public GeneratorResult<KitTargetDescriptor> HostKit { get; set; }

	public ImmutableArray<GeneratorResult<KitTargetDescriptor>> ResourceKits { get; set; }

	public ImmutableArray<DiagnosticInfo> Diagnostics { get; set; } = [];
}

sealed record class KitGenerationContext : GenerationContext
{
	public KitGenerationContext(
		Compilation compilation,
		string generatorName,
		string generatorVersion
	)
		: base(compilation, generatorName: generatorName, generatorVersion: generatorVersion)
	{
		IServiceCollection = GetTypeByMetadataName(TypeLibrary.IServiceCollection)!;
		ConfigurationBinder = GetTypeByMetadataName(TypeLibrary.ConfigurationBinder)!;
		HostKitAttribute = GetTypeByMetadataName(TypeLibrary.HostKitAttribute)!;
		ResourceDefinitionAttribute = GetTypeByMetadataName(
			TypeLibrary.ResourceDefinitionAttribute
		)!;
		GenericResourceDefinitionAttribute = GetTypeByMetadataName(
			TypeLibrary.GenericResourceDefinitionAttribute
		)!;
	}

	public INamedTypeSymbol? IServiceCollection { get; }

	public INamedTypeSymbol? ConfigurationBinder { get; }

	public INamedTypeSymbol HostKitAttribute { get; }

	public INamedTypeSymbol ResourceDefinitionAttribute { get; }

	public INamedTypeSymbol GenericResourceDefinitionAttribute { get; }
}

readonly record struct HostKitInfo(
	KitTargetDescriptor SymbolDescriptor,
	TypeValueObject HostKitType,
	TypeValueObject HostKitOptionsType,
	TypeValueObject HostKitResourceKitBaseType,
	TypeDeclarationAccessibility AccessibilityModifier,
	string ExtensionMethodName,
	ImmutableArray<ResourceKitInfo> ResourceKits
)
{
	public bool ShouldGenerateOptions => HostKitOptionsType != TypeValueObject.Empty;

	public bool HasResourceKits => !ResourceKits.IsDefaultOrEmpty;
}

readonly record struct ResourceKitInfo(
	KitTargetDescriptor SymbolDescriptor,
	TypeValueObject ResourceKitType,
	TypeValueObject ResourceKitOptionsType,
	TypeValueObject AspireResourceType,
	TypeDeclarationAccessibility AccessibilityModifier,
	string PropertyName,
	string? ResourceName,
	bool HasExplicitBaseType
);

sealed record class KitTargetDescriptor(
	TargetSymbolDescriptor Target,
	bool IsHostKit,
	string? Name,
	string? PropertyName,
	string? ExtensionName,
	bool GenerateOptions,
	bool IsGenericResourceDefinition,
	INamedTypeSymbol? AspireResourceTypeSymbol
);
