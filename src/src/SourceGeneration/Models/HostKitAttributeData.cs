using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.SourceGeneratorFramework.Extensions;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

readonly record struct HostKitAttributeData(
	bool Exists,
	string? Name,
	string? ExtensionMethodName,
	bool GenerateOptions
)
{
	public static readonly HostKitAttributeData Empty = new(false, null, null, true);

	public static HostKitAttributeData FromAttributeData(
		Compilation compilation,
		ImmutableArray<AttributeData> attributeData
	)
	{
		var attributeSymbol = compilation.GetTypeByMetadataName(TypeLibrary.HostKitAttribute);
		if (attributeSymbol is not null)
		{
			for (var i = 0; i < attributeData.Length; i++)
			{
				var result = FromAttributeData(compilation, attributeData[i]);
				if (result.Exists)
					return result;
			}
		}

		return Empty;
	}

	public static HostKitAttributeData FromAttributeData(
		INamedTypeSymbol? attributeSymbol,
		AttributeData attributeData
	)
	{
		var exists =
			attributeSymbol is not null
			&& SymbolEqualityComparer.Default.Equals(
				attributeData?.AttributeClass,
				attributeSymbol
			);
		var name = (string?)null;
		var extensionMethodName = (string?)null;
		var generateOptions = true;

		if (exists)
			(name, extensionMethodName, generateOptions) = ReadAttributeArguments(attributeData!);

		return new(exists, name, extensionMethodName, generateOptions);
	}

	public static HostKitAttributeData FromAttributeData(
		Compilation compilation,
		AttributeData attributeData
	) =>
		FromAttributeData(
			compilation.GetTypeByMetadataName(TypeLibrary.HostKitAttribute),
			attributeData
		);

	static (string? Name, string? ExtensionMethodName, bool GenerateOptions) ReadAttributeArguments(
		AttributeData attributeData
	)
	{
		string? name;
		bool generateOptions;
		string? extensionMethodName;
		if (!attributeData.TryGetConstructorArgument(nameof(name), out name))
			name = attributeData.GetNamedArgument<string>(nameof(Name));
		if (!attributeData.TryGetConstructorArgument(nameof(generateOptions), out generateOptions))
			generateOptions = attributeData.GetNamedArgument(
				nameof(GenerateOptions),
				defaultValue: true
			);
		if (
			!attributeData.TryGetConstructorArgument(
				nameof(extensionMethodName),
				out extensionMethodName
			)
		)
			extensionMethodName = attributeData.GetNamedArgument<string>(
				nameof(ExtensionMethodName)
			);

		return (name, extensionMethodName, generateOptions);
	}
}
