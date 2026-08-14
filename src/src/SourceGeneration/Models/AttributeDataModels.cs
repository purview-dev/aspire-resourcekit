using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.SourceGeneratorFramework.Extensions;
using Purview.SourceGeneratorFramework.Testing.Generators;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

[GenerateAttributeDataModel("Purview.Aspire.ResourceKit.HostKitAttribute")]
readonly partial record struct HostKitAttributeData(
	// For the property
	[AttributeNamedProperty]
	// For the ctor
	[AttributeCtorProperty("name")]
		string? Name,
	string? ExtensionMethodName,
	[AttributeNamedProperty]
	[AttributeCtorProperty("generateOptions", DefaultValue = true)]
		bool GenerateOptions
);

[GenerateAttributeDataModel(
	"Purview.Aspire.ResourceKit.ResourceDefinitionAttribute",
	MatchByInheritance = true
)]
readonly partial record struct ResourceDefinitionAttributeData(
	// For the property
	[AttributeNamedProperty]
	// For the ctor
	[AttributeCtorProperty("name")]
		string? Name,
	// For the property
	[AttributeNamedProperty]
	// For the ctor
	[AttributeCtorProperty("propertyName")]
		string? PropertyName,
	[AttributeGenericTypeArgumentProperty(0)] INamedTypeSymbol? AspireResourceType
);

readonly record struct ResourceDefinitionAttributeData1(
	bool Exists,
	string? Name,
	string? PropertyName,
	bool IsGeneric,
	INamedTypeSymbol? AspireResourceType
)
{
	public static readonly ResourceDefinitionAttributeData1 Empty = new(
		false,
		null,
		null,
		false,
		null
	);

	public static ResourceDefinitionAttributeData1 FromAttributeData(
		Compilation compilation,
		ImmutableArray<AttributeData> attributeData
	)
	{
		var resourceDefinitionAttribute = compilation.GetTypeByMetadataName(
			TypeLibrary.ResourceDefinitionAttribute.SymbolFullName
		);
		var genericResourceDefinitionAttribute = compilation.GetTypeByMetadataName(
			TypeLibrary.GenericResourceDefinitionAttribute.SymbolFullName
		);

		if (
			resourceDefinitionAttribute is not null
			|| genericResourceDefinitionAttribute is not null
		)
		{
			for (var i = 0; i < attributeData.Length; i++)
			{
				var result = FromAttributeData(
					resourceDefinitionAttribute,
					genericResourceDefinitionAttribute,
					attributeData[i]
				);
				if (result.Exists)
					return result;
			}
		}

		return Empty;
	}

	public static ResourceDefinitionAttributeData1 FromAttributeData(
		INamedTypeSymbol? resourceDefinitionAttribute,
		INamedTypeSymbol? genericResourceDefinitionAttribute,
		AttributeData attributeData
	)
	{
		var exists =
			resourceDefinitionAttribute is not null
			&& SymbolEqualityComparer.Default.Equals(
				attributeData.AttributeClass,
				resourceDefinitionAttribute
			);
		var isGeneric =
			genericResourceDefinitionAttribute is not null
			&& attributeData.AttributeClass is INamedTypeSymbol namedAttribute
			&& SymbolEqualityComparer.Default.Equals(
				namedAttribute.ConstructedFrom,
				genericResourceDefinitionAttribute
			);

		exists = exists || isGeneric;

		var name = (string?)null;
		var propertyName = (string?)null;
		INamedTypeSymbol? aspireResourceType = null;

		if (exists)
		{
			(name, propertyName) = ReadAttributeArguments(attributeData);
			if (
				isGeneric
				&& attributeData.AttributeClass is INamedTypeSymbol attrClass
				&& attrClass.TypeArguments.Length == 1
			)
				aspireResourceType = attrClass.TypeArguments[0] as INamedTypeSymbol;
		}

		return new(exists, name, propertyName, isGeneric, aspireResourceType);
	}

	static (string? Name, string? PropertyName) ReadAttributeArguments(AttributeData attributeData)
	{
		string? name;
		string? propertyName;

		if (!attributeData.TryGetConstructorArgument(nameof(name), out name))
			name = attributeData.GetNamedArgument<string>(nameof(Name));
		if (!attributeData.TryGetConstructorArgument(nameof(propertyName), out propertyName))
			propertyName = attributeData.GetNamedArgument<string>(nameof(PropertyName));

		return (name, propertyName);
	}
}
