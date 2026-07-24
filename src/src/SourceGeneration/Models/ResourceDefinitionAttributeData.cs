using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

readonly record struct ResourceDefinitionAttributeData(
  bool Exists,
  string? Name,
  string? PropertyName,
  bool GenerateOptions,
  bool IsGeneric,
  string? GenericResourceTypeName
)
{
  static readonly SymbolDisplayFormat FullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

  public static readonly ResourceDefinitionAttributeData Empty = new(false, null, null, true, false, null);

  public static ResourceDefinitionAttributeData FromAttributeData(
    GenerationContext executionContext,
    ImmutableArray<AttributeData> attributeData
  )
  {
    if (
      executionContext.ResourceDefinitionAttribute is not null
      || executionContext.GenericResourceDefinitionAttribute is not null
    )
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

  public static ResourceDefinitionAttributeData FromAttributeData(
    GenerationContext executionContext,
    AttributeData attributeData
  )
  {
    var attributeSymbol = executionContext.ResourceDefinitionAttribute;
    var exists =
      attributeSymbol is not null
      && SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol);
    var genericAttributeSymbol = executionContext.GenericResourceDefinitionAttribute;
    var isGeneric =
      genericAttributeSymbol is not null
      && attributeData.AttributeClass is INamedTypeSymbol namedAttribute
      && SymbolEqualityComparer.Default.Equals(namedAttribute.ConstructedFrom, genericAttributeSymbol);

    exists = exists || isGeneric;

    var name = (string?)null;
    var propertyName = (string?)null;
    var generateOptions = true;
    var genericResourceTypeName = (string?)null;

    if (exists)
    {
      ReadAttributeArguments(attributeData, ref name, ref propertyName, ref generateOptions);
      if (isGeneric && attributeData.AttributeClass is INamedTypeSymbol attrClass && attrClass.TypeArguments.Length == 1)
        genericResourceTypeName = attrClass.TypeArguments[0].ToDisplayString(FullyQualifiedFormat);
    }

    return new(exists, name, propertyName, generateOptions, isGeneric, genericResourceTypeName);
  }

  static void ReadAttributeArguments(
    AttributeData attributeData,
    ref string? name,
    ref string? propertyName,
    ref bool generateOptions
  )
  {
    if (
      attributeData.ConstructorArguments.Length > 0
      && attributeData.ConstructorArguments[0].Value is string ctorName
    )
      name = ctorName;

    if (
      attributeData.ConstructorArguments.Length > 1
      && attributeData.ConstructorArguments[1].Value is string ctorPropertyName
    )
      propertyName = ctorPropertyName;

    if (
      attributeData.ConstructorArguments.Length > 2
      && attributeData.ConstructorArguments[2].Value is bool ctorGenerateOptions
    )
      generateOptions = ctorGenerateOptions;

    foreach (var namedArg in attributeData.NamedArguments)
    {
      switch (namedArg.Key)
      {
        case nameof(Name):
          if (namedArg.Value.Value is string namedName)
            name = namedName;

          break;
        case nameof(PropertyName):
          if (namedArg.Value.Value is string namedPropertyName)
            propertyName = namedPropertyName;

          break;
        case nameof(GenerateOptions):
          if (namedArg.Value.Value is bool namedGenerateOptions)
            generateOptions = namedGenerateOptions;

          break;
      }
    }
  }
}
