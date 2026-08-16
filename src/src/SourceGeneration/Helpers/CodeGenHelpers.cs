using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class CodeGenHelpers
{
	public const string DefaultExtensionMethodName = "AddAspireResourceKit";

	public static HostKitInfo BuildHostKit(
		KitTargetDescriptor hostKitSymbol,
		IEnumerable<KitTargetDescriptor> resourceKitSymbols
	)
	{
		var hostKitType = new TypeModel(
			hostKitSymbol.TypeName,
			hostKitSymbol.Namespace,
			hostKitSymbol.MetadataFullName,
			RenderTypeName(hostKitSymbol.Namespace, hostKitSymbol.TypeName)
		);

		var hostKitOptionsNamespace = string.IsNullOrEmpty(hostKitType.Namespace)
			? hostKitType.TypeName
			: $"{hostKitType.Namespace}.{hostKitType.TypeName}";

		var hostKitOptionsType = hostKitSymbol.GenerateOptions
			? CreateTypeModel($"{hostKitType.TypeName}{TypeLibrary.OptionsBaseClassSuffix}", hostKitOptionsNamespace)
			: default;
		var resourceKits = resourceKitSymbols
			.Select(r =>
			{
				var resourceKitType = new TypeModel(
					r.TypeName,
					r.Namespace,
					r.MetadataFullName,
					RenderTypeName(r.Namespace, r.TypeName)
				);

				var resourceKitOptionsNamespace = string.IsNullOrEmpty(resourceKitType.Namespace)
					? resourceKitType.TypeName
					: $"{resourceKitType.Namespace}.{resourceKitType.TypeName}";
				var resourceKitOptionsType = hostKitSymbol.GenerateOptions
					? CreateTypeModel(
						$"{resourceKitType.TypeName}{TypeLibrary.OptionsBaseClassSuffix}",
						resourceKitOptionsNamespace
					)
					: default;

				var resourceName = r.Name;
				var propertyName = r.PropertyName;
				if (string.IsNullOrWhiteSpace(propertyName))
					propertyName = TrimSuffix(r.TypeName);

				return new ResourceKitInfo(
					r,
					resourceKitType,
					resourceKitOptionsType,
					r.AspireResourceType!.Value,
					r.AccessibilityModifier,
					propertyName!,
					resourceName,
					r.HasExplicitBaseType
				);
			})
			.ToArray();

		return new(
			hostKitSymbol,
			hostKitType,
			hostKitOptionsType,
			TypeLibrary.ResourceKitBase,
			hostKitSymbol.AccessibilityModifier,
			hostKitSymbol.ExtensionName ?? DefaultExtensionMethodName,
			EquatableArray<ResourceKitInfo>.Create(resourceKits)
		);
	}

	static TypeModel CreateTypeModel(string typeName, string @namespace)
	{
		var metadataName = string.IsNullOrEmpty(@namespace) ? typeName : $"{@namespace}.{typeName}";
		return new(typeName, @namespace, metadataName, RenderTypeName(@namespace, typeName));
	}

	static string RenderTypeName(string @namespace, string typeName) =>
		string.IsNullOrEmpty(@namespace) ? $"global::{typeName}" : $"global::{@namespace}.{typeName}";

	public static string TrimSuffix(string typeName) =>
		typeName.EndsWith("ResourceKit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "ResourceKit".Length)
		: typeName.EndsWith("ResourceKit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "ResourceKit".Length)
		: typeName.EndsWith("Resource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Resource".Length)
		: typeName.EndsWith("Kit", StringComparison.Ordinal) ? typeName.Substring(0, typeName.Length - "Kit".Length)
		: typeName;
}
