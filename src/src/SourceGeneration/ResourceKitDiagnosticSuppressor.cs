using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Suppresses <c>CS8618</c> for non-nullable <c>IResourceBuilder&lt;T&gt;</c> properties declared on
/// resource kits. These properties are populated at runtime during the <c>BuildResource</c>/
/// <c>ConfigureResource</c> lifecycle, so the "must contain a non-null value when exiting the constructor"
/// warning does not apply. Nullable <c>IResourceBuilder&lt;T&gt;?</c> properties are left untouched.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResourceKitDiagnosticSuppressor : DiagnosticSuppressor
{
	static readonly SuppressionDescriptor CS8618 = new(
		id: "SGSUP0001",
		suppressedDiagnosticId: "CS8618",
		justification: "Non-nullable IResourceBuilder<T> properties on resource kits are populated during the build or configure lifecycle."
	);

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [CS8618];

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			if (!string.Equals(diagnostic.Id, "CS8618", StringComparison.Ordinal))
				continue;

			if (diagnostic.Location.SourceTree is not { } tree)
				continue;

			var root = tree.GetRoot(context.CancellationToken);
			if (root.FindNode(diagnostic.Location.SourceSpan) is not PropertyDeclarationSyntax propertyDeclaration)
				continue;

			var model = context.GetSemanticModel(tree);
			if (model.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken) is not IPropertySymbol property)
				continue;

			// Only non-nullable IResourceBuilder<T> properties are suppressed.
			if (property.Type.NullableAnnotation != NullableAnnotation.NotAnnotated)
				continue;

			if (!TypeLibrary.Aspire.Hosting.ApplicationModel.IResourceBuilder.Matches(property.Type))
				continue;

			if (!IsResourceKit(property.ContainingType))
				continue;

			context.ReportSuppression(Suppression.Create(CS8618, diagnostic));
		}
	}

	/// <summary>
	/// Determines whether the type is a resource kit: decorated with
	/// <c>ResourceDefinition</c>/<c>ResourceDefinition&lt;TResource&gt;</c>, or deriving from
	/// <c>Purview.Aspire.ResourceKit.ResourceKitBase&lt;,&gt;</c> (directly or via the generated
	/// <c>ResourceKitBase&lt;TResource&gt;</c>).
	/// </summary>
	static bool IsResourceKit(INamedTypeSymbol type)
	{
		if (
			TypeHelpers.HasAttribute(type, TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
			|| TypeHelpers.HasAttribute(type, TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute)
		)
			return true;

		for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			var definition = baseType.OriginalDefinition;
			if (
				string.Equals(definition.Name, "ResourceKitBase", StringComparison.Ordinal)
				&& string.Equals(
					definition.ContainingNamespace?.ToDisplayString(),
					"Purview.Aspire.ResourceKit",
					StringComparison.Ordinal
				)
			)
				return true;
		}

		return false;
	}
}
