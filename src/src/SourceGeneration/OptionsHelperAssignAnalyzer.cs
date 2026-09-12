using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Reports <c>SG0020</c> when an <c>OptionsHelper.Assign</c> (or chained <c>IOptionsBuilder.Assign</c>)
/// action is a block-bodied lambda that assigns more than one property path. Each assignment action must
/// set exactly one property path, otherwise <c>OptionsHelper</c> throws at runtime when the arguments are
/// built.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionsHelperAssignAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[DiagnosticLibrary.AssignSetsMultiplePropertyPaths];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(
			static analysisContext => AnalyzeInvocation(analysisContext),
			SyntaxKind.InvocationExpression
		);
	}

	static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not InvocationExpressionSyntax invocation)
			return;

		if (
			context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
				is not IMethodSymbol method
			|| !IsOptionsHelperAssign(method)
		)
			return;

		var assignmentArguments = GetAssignmentActionArguments(invocation, method);
		foreach (var (argument, lambda) in assignmentArguments)
		{
			if (!TryGetBlockAssignments(lambda, out var assignments) || assignments.Count < 2)
				continue;

			var distinctPaths = assignments
				.Select(static assignment => assignment.Left.ToString())
				.Distinct(StringComparer.Ordinal)
				.ToArray();

			if (distinctPaths.Length < 2)
				continue;

			context.ReportDiagnostic(
				Diagnostic.Create(
					DiagnosticLibrary.AssignSetsMultiplePropertyPaths,
					lambda.GetLocation(),
					distinctPaths.Length,
					string.Join(", ", distinctPaths)
				)
			);
		}
	}

	/// <summary>
	/// Matches <c>OptionsHelper.Assign&lt;TOptions&gt;(...)</c>, its <c>sectionName</c> overload, and the
	/// chained <c>IOptionsBuilder.Assign&lt;TOptions&gt;(...)</c> calls by method name, containing
	/// namespace, and the <c>params Action&lt;T&gt;[]</c> parameter shape.
	/// </summary>
	static bool IsOptionsHelperAssign(IMethodSymbol method)
	{
		if (method.Name != "Assign")
			return false;

		if (method.ContainingNamespace?.ToDisplayString() != TypeLibraryGenerator.PurviewAspireResourceKitNamespace)
			return false;

		// The method must have a params Action<T>[] parameter, which is the last parameter.
		return method.Parameters.Any(static parameter =>
			parameter.IsParams
			&& parameter.Type is IArrayTypeSymbol { ElementType: INamedTypeSymbol { Name: "Action" } elementType }
			&& elementType.TypeArguments.Length == 1
		);
	}

	/// <summary>
	/// Returns each argument that is bound to the <c>params Action&lt;T&gt;[]</c> parameter together with
	/// its lambda (or anonymous method) syntax, skipping the leading <c>sectionName</c> string argument and
	/// arguments that pass the whole array as a single value.
	/// </summary>
	static IEnumerable<(ArgumentSyntax Argument, LambdaExpressionSyntax Lambda)> GetAssignmentActionArguments(
		InvocationExpressionSyntax invocation,
		IMethodSymbol method
	)
	{
		var paramsParameterIndex = method.Parameters.ToList().FindIndex(static parameter => parameter.IsParams);
		if (paramsParameterIndex < 0)
			yield break;

		var arguments = invocation.ArgumentList.Arguments;
		for (var i = paramsParameterIndex; i < arguments.Count; i++)
		{
			var argument = arguments[i];
			if (argument.Expression is LambdaExpressionSyntax lambda)
				yield return (argument, lambda);
		}
	}

	/// <summary>
	/// Collects the member-assignment expressions in a block-bodied lambda (or anonymous method). Only
	/// assignments whose left-hand side is a member access (for example <c>o.X.Y</c>) are considered.
	/// </summary>
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
