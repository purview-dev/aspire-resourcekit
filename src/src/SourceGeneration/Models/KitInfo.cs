using System.Collections.Immutable;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

readonly record struct HostKitInfo(
	TargetSymbolDescriptor SymbolDescriptor,
	TypeValueObject HostKitType,
	TypeValueObject HostKitOptionsType,
	TypeValueObject HostKitResourceKitBaseType,
	string AccessibilityModifier,
	string ExtensionMethodName,
	ImmutableArray<ResourceKitInfo> ResourceKits
)
{
	public bool ShouldGenerateOptions => HostKitOptionsType != TypeValueObject.Empty;

	public bool HasResourceKits => !ResourceKits.IsDefaultOrEmpty;
}

readonly record struct ResourceKitInfo(
	TargetSymbolDescriptor SymbolDescriptor,
	TypeValueObject ResourceKitType,
	TypeValueObject ResourceKitOptionsType,
	TypeValueObject AspireResourceType,
	string AccessibilityModifier,
	string PropertyName,
	string? ResourceName,
	bool HasExplicitBaseType
);
