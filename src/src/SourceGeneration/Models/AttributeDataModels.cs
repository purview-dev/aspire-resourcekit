using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Generators;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

[Generate("Purview.Aspire.ResourceKit.HostKitAttribute")]
readonly partial record struct HostKitAttributeData(
	// For the property
	[Property]
	// For the ctor
	[Argument("name")]
		string? Name,
	string? ExtensionMethodName,
#pragma warning disable format
	[Property] [Argument("generateOptions", DefaultValue = true)] bool GenerateOptions
#pragma warning restore format
);

[Generate("Purview.Aspire.ResourceKit.ResourceDefinitionAttribute", MatchByInheritance = true)]
readonly partial record struct ResourceDefinitionAttributeData(
	// For the property
	[Property]
	// For the ctor
	[Argument("name")]
		string? Name,
	// For the property
	[Property]
	// For the ctor
	[Argument("propertyName")]
		string? PropertyName,
	[GenericTypeArgument] INamedTypeSymbol? AspireResourceType
);
