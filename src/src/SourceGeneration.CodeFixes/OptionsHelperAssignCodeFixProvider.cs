using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Purview.Aspire.ResourceKit.SourceGeneration.CodeFixes;

/// <summary>
/// Provides a code fix for <c>SG0020</c> that splits a block-bodied <c>OptionsHelper.Assign</c> action
/// assigning multiple property paths into one assignment per argument.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OptionsHelperAssignCodeFixProvider))]
public sealed class OptionsHelperAssignCodeFixProvider : CodeFixProvider
{
	const string OptionsHelperAssignRuleId = "SG0020";

	public override ImmutableArray<string> FixableDiagnosticIds => [OptionsHelperAssignRuleId];

	public override FixAllProvider? GetFixAllProvider() => null;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		var diagnostic = context.Diagnostics[0];
		var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
		var lambda = node as LambdaExpressionSyntax ?? node?.FirstAncestorOrSelf<LambdaExpressionSyntax>();
		var invocation = lambda?.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
		if (invocation is null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				title: "Split into separate assignments",
				createChangedDocument: cancellationToken =>
					SplitAssignmentsAsync(context.Document, invocation, cancellationToken),
				equivalenceKey: "SplitIntoSeparateAssignments"
			),
			diagnostic
		);
	}

	static async Task<Document> SplitAssignmentsAsync(
		Document document,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		List<ArgumentSyntax> newArguments = [];
		foreach (var argument in invocation.ArgumentList.Arguments)
		{
			if (
				argument.Expression is not LambdaExpressionSyntax lambda
				|| !TryGetBlockAssignments(lambda, out var assignments)
				|| assignments.Count < 2
			)
			{
				newArguments.Add(argument);
				continue;
			}

			var distinctPaths = assignments
				.Select(static assignment => assignment.Left.ToString())
				.Distinct(StringComparer.Ordinal)
				.ToArray();
			if (distinctPaths.Length < 2)
			{
				newArguments.Add(argument);
				continue;
			}

			var parameter = lambda switch
			{
				SimpleLambdaExpressionSyntax simple => simple.Parameter,
				ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters[0],
				_ => null,
			};
			if (parameter is null)
			{
				newArguments.Add(argument);
				continue;
			}

			foreach (var assignment in assignments)
			{
				var simpleLambda = SyntaxFactory.SimpleLambdaExpression(
					parameter.WithoutTrivia(),
					assignment.WithoutTrivia()
				);
				newArguments.Add(SyntaxFactory.Argument(simpleLambda));
			}
		}

		var separators = Enumerable.Repeat(
			SyntaxFactory.Token(SyntaxKind.CommaToken),
			Math.Max(0, newArguments.Count - 1)
		);
		var newArgumentList = SyntaxFactory
			.ArgumentList(SyntaxFactory.SeparatedList(newArguments, separators))
			.WithTriviaFrom(invocation.ArgumentList)
			.WithAdditionalAnnotations(Formatter.Annotation);

		var newInvocation = invocation.WithArgumentList(newArgumentList);
		var newRoot = root.ReplaceNode(invocation, newInvocation);
		return await Formatter
			.FormatAsync(document.WithSyntaxRoot(newRoot), cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	static bool TryGetBlockAssignments(LambdaExpressionSyntax lambda, out List<AssignmentExpressionSyntax> assignments)
	{
		BlockSyntax? body = null;
		if (lambda is SimpleLambdaExpressionSyntax simpleLambda && simpleLambda.Body is BlockSyntax simpleBlock)
			body = simpleBlock;
		else if (
			lambda is ParenthesizedLambdaExpressionSyntax parenthesized
			&& parenthesized.Body is BlockSyntax parenthesizedBlock
		)
			body = parenthesizedBlock;

		if (body is null)
		{
			assignments = [];
			return false;
		}

		assignments =
		[
			.. body.DescendantNodes()
				.OfType<AssignmentExpressionSyntax>()
				.Where(static assignment => assignment.Left is MemberAccessExpressionSyntax),
		];

		return true;
	}
}
