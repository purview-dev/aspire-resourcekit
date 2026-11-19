using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace Purview.Aspire.ResourceKit.SourceGeneration.CodeFixes;

/// <summary>
/// Fixes <c>SG0044</c> by deleting a <c>BuildResource</c> override that only repeats the registration
/// ResourceKit generates. When the attribute does not declare the path yet, the path argument is moved into
/// <c>ProjectPath</c>/<c>ResourcePath</c> first, so a hand-written
/// <c>builder.AddDotnetProject(Name, AppHostProjects.Api.Path)</c> becomes
/// <c>[ResourceDefinition&lt;DotnetProjectResource&gt;(..., ProjectPath = AppHostProjects.Api.Path)]</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RedundantBuildResourceCodeFixProvider))]
public sealed class RedundantBuildResourceCodeFixProvider : CodeFixProvider
{
	// Mirrors RedundantBuildResourceAnalyzer; this assembly does not reference the generator.
	const string RedundantBuildResourceRuleId = "SG0044";
	const string FixKindProperty = "FixKind";
	const string MoveFix = "MoveToAttribute";
	const string PathPropertyProperty = "PathProperty";

	public override ImmutableArray<string> FixableDiagnosticIds => [RedundantBuildResourceRuleId];

	public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		var diagnostic = context.Diagnostics[0];
		var method = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
		if (method is null)
			return;

		if (
			diagnostic.Properties.TryGetValue(FixKindProperty, out var fixKind)
			&& fixKind == MoveFix
			&& diagnostic.Properties.TryGetValue(PathPropertyProperty, out var pathProperty)
			&& pathProperty is not null
			&& diagnostic.AdditionalLocations.Count == 2
		)
		{
			context.RegisterCodeFix(
				CodeAction.Create(
					title: $"Declare the path as {pathProperty} and remove BuildResource",
					createChangedSolution: cancellationToken =>
						MovePathToAttributeAsync(
							context.Document,
							method,
							diagnostic.AdditionalLocations[0],
							diagnostic.AdditionalLocations[1],
							pathProperty,
							cancellationToken
						),
					equivalenceKey: "MoveBuildResourcePathToAttribute"
				),
				diagnostic
			);
			return;
		}

		context.RegisterCodeFix(
			CodeAction.Create(
				title: "Remove the redundant BuildResource override",
				createChangedDocument: cancellationToken =>
					RemoveMethodAsync(context.Document, method, cancellationToken),
				equivalenceKey: "RemoveRedundantBuildResource"
			),
			diagnostic
		);
	}

	static async Task<Document> RemoveMethodAsync(
		Document document,
		MethodDeclarationSyntax method,
		CancellationToken cancellationToken
	)
	{
		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		editor.RemoveNode(method);
		return editor.GetChangedDocument();
	}

	static async Task<Solution> MovePathToAttributeAsync(
		Document document,
		MethodDeclarationSyntax method,
		Location pathLocation,
		Location attributeLocation,
		string pathProperty,
		CancellationToken cancellationToken
	)
	{
		var solution = document.Project.Solution;
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (root is null || model is null || attributeLocation.SourceTree is null)
			return solution;

		var pathNode = root.FindNode(pathLocation.SourceSpan, getInnermostNodeForTie: true);
		var pathExpression = pathNode as ExpressionSyntax ?? (pathNode as ArgumentSyntax)?.Expression;
		if (pathExpression is null)
			return solution;

		// The attribute may live in another partial declaration of the kit.
		var attributeDocument = solution.GetDocument(attributeLocation.SourceTree);
		if (attributeDocument is null)
			return solution;

		var attributeRoot = await attributeDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (
			attributeRoot?.FindNode(attributeLocation.SourceSpan).FirstAncestorOrSelf<AttributeSyntax>()
			is not { } attribute
		)
			return solution;

		var movedPath = AttributePathExpression(pathExpression, model, cancellationToken);
		if (movedPath is null)
			return solution;

		var updatedAttribute = attribute.AddArgumentListArguments(
			SyntaxFactory.AttributeArgument(
				SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(pathProperty)),
				nameColon: null,
				movedPath
			)
		);

		SolutionEditor editor = new(solution);
		var attributeEditor = await editor
			.GetDocumentEditorAsync(attributeDocument.Id, cancellationToken)
			.ConfigureAwait(false);
		attributeEditor.ReplaceNode(attribute, updatedAttribute);

		var methodEditor = await editor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
		methodEditor.RemoveNode(method);

		return editor.GetChangedSolution();
	}

	/// <summary>
	/// Produces the path as an attribute argument. The attribute may be in another file with other usings, so
	/// a literal is kept as written, a constant field (such as <c>AppHostProjects.Api.Path</c>) is fully
	/// qualified and left for the simplifier to shorten, and anything else - a local constant, a
	/// concatenation - becomes its literal value.
	/// </summary>
	static ExpressionSyntax? AttributePathExpression(
		ExpressionSyntax pathExpression,
		SemanticModel model,
		CancellationToken cancellationToken
	)
	{
		var operation = model.GetOperation(pathExpression, cancellationToken);
		while (operation is IConversionOperation { IsImplicit: true } conversion)
			operation = conversion.Operand;

		return operation switch
		{
			ILiteralOperation => pathExpression.WithoutTrivia(),
			IFieldReferenceOperation { Field: { IsConst: true } field } => SyntaxFactory
				.ParseExpression(
					$"{field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{field.Name}"
				)
				.WithAdditionalAnnotations(Simplifier.Annotation),
			{ ConstantValue: { HasValue: true, Value: string value } } => SyntaxFactory.LiteralExpression(
				SyntaxKind.StringLiteralExpression,
				SyntaxFactory.Literal(value)
			),
			_ => null,
		};
	}
}
