using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Reports <c>SG0040</c> when a resource kit is routed through its <c>ResourceBuilder</c> property even though
/// the kit itself implements <c>IResourceBuilder&lt;T&gt;</c>. The code fix
/// removes the redundant <c>.ResourceBuilder</c> hop.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantResourceBuilderAccessAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[DiagnosticLibrary.RedundantResourceBuilderAccess];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
	}

	static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
	{
		var node = (MemberAccessExpressionSyntax)context.Node;
		if (node.Name.Identifier.ValueText != "ResourceBuilder")
			return;

		if (node.Ancestors().OfType<InvocationExpressionSyntax>().Any(IsNameOf))
			return;

		// Only simplify when the kit is the receiver of the next member access/invocation, for example
		// Parent.ResourceBuilder.AddBlobs(...) -> Parent.AddBlobs(...). In an argument position the expected
		// type might be IResourceBuilder<IResource>, which the kit's own IResourceBuilder<TResource> does not
		// satisfy, so replacing it could break compilation.
		if (node.Parent is not MemberAccessExpressionSyntax outer || !ReferenceEquals(outer.Expression, node))
			return;

		if (
			context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol is not IPropertySymbol property
			|| property.Name != "ResourceBuilder"
		)
			return;

		if (
			context.SemanticModel.GetTypeInfo(node.Expression, context.CancellationToken).Type
			is not INamedTypeSymbol receiver
		)
			return;

		if (
			!receiver.AllInterfaces.Any(interfaceType =>
				TypeLibrary.Purview.Aspire.ResourceKit.IResourceKit.Matches(interfaceType)
			)
			|| !receiver.AllInterfaces.Any(interfaceType =>
				TypeLibrary.Aspire.Hosting.ApplicationModel.IResourceBuilder.Matches(interfaceType)
			)
		)
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(DiagnosticLibrary.RedundantResourceBuilderAccess, node.Name.GetLocation())
		);
	}

	static bool IsNameOf(InvocationExpressionSyntax invocation) =>
		invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" };
}
