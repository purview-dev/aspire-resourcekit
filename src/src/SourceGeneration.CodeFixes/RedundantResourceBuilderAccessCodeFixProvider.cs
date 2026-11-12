using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Aspire.ResourceKit.SourceGeneration.CodeFixes;

/// <summary>
/// Fixes <c>SG0040</c> by removing a redundant <c>.ResourceBuilder</c> hop from a resource kit expression:
/// <c>Parent.ResourceBuilder.AddBlobs(...)</c> becomes <c>Parent.AddBlobs(...)</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RedundantResourceBuilderAccessCodeFixProvider))]
public sealed class RedundantResourceBuilderAccessCodeFixProvider : CodeFixProvider
{
	const string RedundantResourceBuilderRuleId = "SG0040";

	public override ImmutableArray<string> FixableDiagnosticIds => [RedundantResourceBuilderRuleId];

	public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		var diagnostic = context.Diagnostics[0];
		var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
		var memberAccess = node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
		if (memberAccess is null || memberAccess.Name.Identifier.ValueText != "ResourceBuilder")
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				title: "Use the resource kit directly",
				createChangedDocument: cancellationToken =>
					RemoveResourceBuilderAsync(context.Document, memberAccess, cancellationToken),
				equivalenceKey: "UseResourceKitDirectly"
			),
			diagnostic
		);
	}

	static async Task<Document> RemoveResourceBuilderAsync(
		Document document,
		MemberAccessExpressionSyntax memberAccess,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		// Keep the original trivia (indentation, trailing comment) on the receiver that replaces the hop.
		var replacement = memberAccess.Expression.WithTriviaFrom(memberAccess);
		var newRoot = root.ReplaceNode(memberAccess, replacement);

		return document.WithSyntaxRoot(newRoot);
	}
}
