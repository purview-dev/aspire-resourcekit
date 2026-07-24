using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

readonly record struct HostAppAttributeData(bool Exists, string? Name, bool GenerateOptions, int ServiceLifetime)
{
	public static readonly HostAppAttributeData Empty = new(false, null, true, 0);

	public static HostAppAttributeData FromAttributeData(
		GenerationContext executionContext,
		ImmutableArray<AttributeData> attributeData
	)
	{
		if (executionContext.HostAppAttribute is not null)
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

	public static HostAppAttributeData FromAttributeData(
		GenerationContext executionContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = executionContext.HostAppAttribute;
		var exists =
			attributeSymbol is not null
			&& SymbolEqualityComparer.Default.Equals(attributeData?.AttributeClass, attributeSymbol);
		var name = (string?)null;
		var generateOptions = true;
		var serviceLifetime = 0;

		if (exists)
			(name, generateOptions, serviceLifetime) = ReadAttributeArguments(
				attributeData!,
				name,
				generateOptions,
				serviceLifetime
			);

		return new(exists, name, generateOptions, serviceLifetime);
	}

	static (string? Name, bool GenerateOptions, int ServiceLifetime) ReadAttributeArguments(
		AttributeData attributeData,
		string? name,
		bool generateOptions,
		int serviceLifetime
	)
	{
		foreach (var ctorArg in attributeData.ConstructorArguments)
		{
			if (ctorArg.Value is string ctorName)
				name = ctorName;
			else if (ctorArg.Value is bool ctorGenerateOptions)
				generateOptions = ctorGenerateOptions;
			else if (ctorArg.Value is int ctorServiceLifetime)
				serviceLifetime = ctorServiceLifetime;
		}

		foreach (var namedArg in attributeData.NamedArguments)
		{
			switch (namedArg.Key)
			{
				case nameof(Name):
					if (namedArg.Value.Value is string namedName)
						name = namedName;

					break;
				case nameof(GenerateOptions):
					if (namedArg.Value.Value is bool namedGenerateOptions)
						generateOptions = namedGenerateOptions;

					break;
				case nameof(ServiceLifetime):
					if (namedArg.Value.Value is int namedServiceLifetime)
						serviceLifetime = namedServiceLifetime;

					break;
			}
		}

		return (name, generateOptions, serviceLifetime);
	}
}
