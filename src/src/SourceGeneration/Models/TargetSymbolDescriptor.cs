using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

sealed record class TargetSymbolDescriptor(
	INamedTypeSymbol Symbol,
	TypeDeclarationSyntax Declaration,
	bool IsHostKit,
	string? Name,
	string? PropertyName,
	string? ExtensionName,
	bool GenerateOptions,
	bool IsGenericResourceDefinition,
	INamedTypeSymbol? AspireResourceTypeSymbol
);
