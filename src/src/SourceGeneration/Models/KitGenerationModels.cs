namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

sealed record KitGenerationModel(
	bool IsSourceGeneratorEnabled,
	bool HasIServiceCollection,
	bool HasConfigurationBinder
)
{
	public GeneratorResult<KitTargetDescriptor> HostKit { get; init; }

	public EquatableArray<GeneratorResult<KitTargetDescriptor>> ResourceKits { get; init; }

	public EquatableArray<DiagnosticInfo> Diagnostics { get; init; }
}

readonly record struct GenerationCapabilities(
	bool HasIServiceCollection,
	bool HasConfigurationBinder
);

readonly record struct TypeModel(
	string TypeName,
	string Namespace,
	string MetadataFullName,
	string RenderFullName
)
{
	public TypeValueObject AsTypeValueObject() =>
		new(TypeName, Namespace.Length == 0 ? null : Namespace);

	public TypeModel MakeGeneric(params TypeModel[] arguments)
	{
		var typeName =
			$"{TypeName}<{string.Join(", ", arguments.Select(argument => argument.RenderFullName))}>";
		return new(typeName, Namespace, MetadataFullName, $"global::{Namespace}.{typeName}");
	}

	public static implicit operator TypeValueObject(TypeModel model) => model.AsTypeValueObject();

	public static implicit operator TypeModel(TypeValueObject model) =>
		new(
			model.TypeName,
			model.Namespace ?? string.Empty,
			model.MetadataFullName,
			model.RenderFullName
		);

	public static implicit operator TypeReferenceOptions(TypeModel model) =>
		new(model.RenderFullName);

	public static implicit operator string(TypeModel model) => model.RenderFullName;

	public override string ToString() => RenderFullName;
}

readonly record struct HostKitInfo(
	KitTargetDescriptor SymbolDescriptor,
	TypeModel HostKitType,
	TypeModel HostKitOptionsType,
	TypeModel HostKitResourceKitBaseType,
	TypeDeclarationAccessibility AccessibilityModifier,
	string ExtensionMethodName,
	EquatableArray<ResourceKitInfo> ResourceKits
)
{
	public bool ShouldGenerateOptions => !HostKitOptionsType.Equals(default);

	public bool HasResourceKits => !ResourceKits.IsEmpty;
}

readonly record struct ResourceKitInfo(
	KitTargetDescriptor SymbolDescriptor,
	TypeModel ResourceKitType,
	TypeModel ResourceKitOptionsType,
	TypeModel AspireResourceType,
	TypeDeclarationAccessibility AccessibilityModifier,
	string PropertyName,
	string? ResourceName,
	bool HasExplicitBaseType
);

sealed record KitTargetDescriptor(
	string TypeName,
	string Namespace,
	string MetadataFullName,
	TypeDeclarationAccessibility AccessibilityModifier,
	bool IsHostKit,
	string? Name,
	string? PropertyName,
	string? ExtensionName,
	bool GenerateOptions,
	bool IsGenericResourceDefinition,
	TypeModel? AspireResourceType,
	bool HasExplicitBaseType,
	bool IsDerivedFromExpectedBase
);
