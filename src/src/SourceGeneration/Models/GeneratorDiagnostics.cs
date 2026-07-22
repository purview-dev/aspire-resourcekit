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

	public static readonly DiagnosticDescriptor NoAppResourcesDefined = new(
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

	public static readonly DiagnosticDescriptor MultipleHostAppsFoundnfo = new(
		id: "SG0004",
		title: "Multiple host apps defined",
		messageFormat: "Multiple host apps were defined in the app, only a single one is permitted",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DuplicateResourcePropertyName = new(
		id: "SG0005",
		title: "Duplicate resource property name",
		messageFormat: "The property name '{0}' is used by multiple app resources; property names must be unique",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourceMustDeriveFromBase = new(
		id: "SG0006",
		title: "App resource must derive from generated base",
		messageFormat: "'{0}' must derive from '{1}' to be a valid app resource",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourceNameNotDerivable = new(
		id: "SG0007",
		title: "Resource name could not be determined",
		messageFormat: "A resource name could not be derived from '{0}' and no Name was specified on the AppResourceAttribute",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidPropertyName = new(
		id: "SG0008",
		title: "Invalid property name",
		messageFormat: "The PropertyName '{0}' is not a valid C# identifier",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ServiceLifetiemMissing = new(
		id: "SG0009",
		title: "ServiceLifetime type missing",
		messageFormat: "Add the `Microsoft.Extensions.DependencyInjection.Abstractions` NuGet package",
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
