using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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
		GenerationContext executionContext,
		ImmutableArray<AttributeData> attributeData
	)
	{
		if (executionContext.HostKitAttribute is not null)
		{
			for (var i = 0; i < attributeData.Length; i++)
			{
				var result = FromAttributeData(executionContext, attributeData[i]);
				if (result.Exists)
					return result;
			}
		}

		return Empty;
	}

	public static HostKitAttributeData FromAttributeData(
		GenerationContext executionContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = executionContext.HostKitAttribute;
		var exists =
			attributeSymbol is not null
			&& SymbolEqualityComparer.Default.Equals(attributeData?.AttributeClass, attributeSymbol);
		var name = (string?)null;
		var extensionMethodName = (string?)null;
		var generateOptions = true;

		if (exists)
			(name, extensionMethodName, generateOptions) = ReadAttributeArguments(attributeData!);

		return new(exists, name, extensionMethodName, generateOptions);
	}

	static (string? Name, string? ExtensionMethodName, bool GenerateOptions) ReadAttributeArguments(
		AttributeData attributeData
	)
	{
		string? name = null;
		string? extensionMethodName = null;
		bool generateOptions = true;

		foreach (var ctorArg in attributeData.ConstructorArguments)
		{
			if (ctorArg.Value is string ctorName)
				name = ctorName;
			else if (ctorArg.Value is bool ctorGenerateOptions)
				generateOptions = ctorGenerateOptions;
		}

		foreach (var namedArg in attributeData.NamedArguments)
		{
			switch (namedArg.Key)
			{
				case nameof(Name):
					if (namedArg.Value.Value is string namedName)
						name = namedName;

					break;
				case nameof(ExtensionMethodName):
					if (namedArg.Value.Value is string namedExtensionMethodName)
						extensionMethodName = namedExtensionMethodName;

					break;
				case nameof(GenerateOptions):
					if (namedArg.Value.Value is bool namedGenerateOptions)
						generateOptions = namedGenerateOptions;

					break;
			}
		}

		return (name, extensionMethodName, generateOptions);
	}
}
