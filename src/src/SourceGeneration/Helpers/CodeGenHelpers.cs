using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class CodeGenHelpers
{
	public const string DefaultExtensionMethodName = "AddAspireResourceKit";

	public static HostKitInfo BuildHostKit(
		KitTargetDescriptor hostKitSymbol,
		ImmutableArray<KitTargetDescriptor> resourceKitSymbols
	)
	{
		TypeValueObject hostKitType = new(hostKitSymbol.Target.Symbol);

		var hostKitOptionsNamespace =
			hostKitType.Namespace
			+ (hostKitType.IsGlobalNamespace ? null : '.')
			+ hostKitType.TypeName;

		var hostKitOptionsType = hostKitSymbol.GenerateOptions
			? new TypeValueObject(
				$"{hostKitType.TypeName}{TypeLibrary.OptionsBaseClassSuffix}",
				hostKitOptionsNamespace
			)
			: TypeValueObject.Empty;
		var resourceKits = resourceKitSymbols
			.Select(r =>
			{
				TypeValueObject resourceKitType = new(r.Target.Symbol);

				var resourceKitOptionsNamespace =
					resourceKitType.Namespace
					+ (resourceKitType.IsGlobalNamespace ? null : '.')
					+ resourceKitType.TypeName;
				var resourceKitOptionsType = hostKitSymbol.GenerateOptions
					? new TypeValueObject(
						$"{resourceKitType.TypeName}{TypeLibrary.OptionsBaseClassSuffix}",
						resourceKitOptionsNamespace
					)
					: TypeValueObject.Empty;

				TypeValueObject aspireResourceType = new(r.AspireResourceTypeSymbol!);
				var resourceName = r.Name;
				var propertyName = r.PropertyName;
				if (string.IsNullOrWhiteSpace(propertyName))
					propertyName = TrimSuffix(r.Target.Symbol.Name);

				return new ResourceKitInfo(
					r,
					resourceKitType,
					resourceKitOptionsType,
					aspireResourceType,
					r.Target.Symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility()!.Value,
					r.PropertyName!,
					resourceName,
					TypeHelpers.HasExplicitBaseType(r.Target)
				);
			})
			.ToImmutableArray();

		return new(
			hostKitSymbol,
			hostKitType,
			hostKitOptionsType,
			TypeLibrary.ResourceKitBase,
			hostKitSymbol
				.Target.Symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility()!
				.Value,
			hostKitSymbol.ExtensionName ?? DefaultExtensionMethodName,
			resourceKits
		);
	}

	public static string TrimSuffix(string typeName) =>
		typeName.EndsWith("ResourceKit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "ResourceKit".Length)
		: typeName.EndsWith("ResourceKit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "ResourceKit".Length)
		: typeName.EndsWith("Resource", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Resource".Length)
		: typeName.EndsWith("Kit", StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - "Kit".Length)
		: typeName;
}
