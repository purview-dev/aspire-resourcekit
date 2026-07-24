using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

sealed record class TargetSymbolDescriptor(
	INamedTypeSymbol Symbol,
	TypeDeclarationSyntax Declaration,
	bool IsHostApp,
	string? Name,
	string? PropertyName,
	bool GenerateOptions,
	string ServiceLifetime
);
