using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

static class GeneratorDiagnostics
{
	const string Category = "Purview.Aspire.ResourceIsolation.SourceGenerator";

	public static readonly DiagnosticDescriptor ClassMustBePartial = new(
		id: "SG0001",
		title: "Class must be partial",
		messageFormat: "'{0}' must be declared partial to allow source generation",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Classes decorated with source-generation attributes must carry the 'partial' modifier "
			+ "so that the generator can emit additional members into the same class."
	);

	public static readonly DiagnosticDescriptor NoHostResourcesDefined = new(
		id: "SG0002",
		title: "No host resources defined",
		messageFormat: "No host resources were defined for '{0}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NoHostAppInfoDefined = new(
		id: "SG0003",
		title: "No host app info defined",
		messageFormat: "No host app info was defined",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor MultipleHostAppAttributesInfo = new(
		id: "SG0004",
		title: "Multiple host app attributes defined",
		messageFormat: "Multiple host app attributes were defined in the app, only a single one is allowed",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static DiagnosticInfo Create(
		DiagnosticDescriptor diagnostic,
		INamedTypeSymbol? symbol = null,
		TypeDeclarationSyntax? declaration = null,
		SyntaxNode? locationNode = null
	)
	{
		var messageArgs = symbol is not null ? new[] { symbol.Name } : [];

		return DiagnosticInfo.Create(
			diagnostic,
			locationNode?.GetLocation() ?? declaration?.Identifier.GetLocation(),
			messageArgs
		);
	}
}
