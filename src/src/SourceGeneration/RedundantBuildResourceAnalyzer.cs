using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Reports <c>SG0044</c> when a hand-written <c>BuildResource</c> only repeats the registration ResourceKit
/// generates: <c>builder.AddDotnetProject(Name, path)</c>, <c>builder.AddViteApp(Name, path)</c> or
/// <c>builder.AddNextJsApp(Name, path)</c>, with every other argument left at its default.
/// </summary>
/// <remarks>
/// <para>
/// When the attribute already declares the same path, the override is a second statement of it and the code
/// fix deletes it. When the attribute declares no path, the code fix moves the path into
/// <c>ProjectPath</c>/<c>ResourcePath</c> first, so the attribute becomes the one place it is stated.
/// </para>
/// <para>
/// Generated code is excluded, so the generator's own <c>BuildResource</c> is never reported.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantBuildResourceAnalyzer : DiagnosticAnalyzer
{
	/// <summary>The diagnostic property naming the fix: <see cref="RemoveFix"/> or <see cref="MoveFix"/>.</summary>
	public const string FixKindProperty = "FixKind";

	/// <summary>The attribute already declares the path; delete the override.</summary>
	public const string RemoveFix = "Remove";

	/// <summary>The attribute declares no path; move it into the attribute, then delete the override.</summary>
	public const string MoveFix = "MoveToAttribute";

	/// <summary>The diagnostic property naming the attribute property that declares the path.</summary>
	public const string PathPropertyProperty = "PathProperty";

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[DiagnosticLibrary.RedundantBuildResource];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
	}

	static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
	{
		var declaration = (MethodDeclarationSyntax)context.Node;
		if (
			declaration.Identifier.ValueText != "BuildResource"
			|| !declaration.Modifiers.Any(SyntaxKind.OverrideKeyword)
			|| declaration.ParameterList.Parameters.Count != 1
		)
			return;

		if (SingleExpression(declaration) is not { } expression)
			return;

		var cancellationToken = context.CancellationToken;
		if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not { } method)
			return;

		var kit = method.ContainingType;
		var attributes = ResourceDefinitionAttributeData.AllAttributeData(kit).ToArray();
		// SG0013 owns a kit with mixed attributes; there is no single declaration to move the path into.
		if (attributes.Length != 1 || attributes[0].Attribute is not { } attribute)
			return;

		// Removing an override of a concrete base implementation would change behaviour, and the generator
		// does not replace one.
		if (ResourceKitRules.InheritsBuildResource(kit))
			return;

		if (ResourceKitRules.GetDefaultBuildResource(kit, attributes[0]) is not { } defaultBuild)
			return;

		if (context.SemanticModel.GetOperation(expression, cancellationToken) is not IInvocationOperation invocation)
			return;

		if (
			invocation.TargetMethod.Name != defaultBuild.MethodName
			|| !defaultBuild.ExtensionsType.Matches(invocation.TargetMethod.ContainingType)
		)
			return;

		if (MatchArguments(invocation, method.Parameters[0]) is not { } pathArgument)
			return;

		var path = (string)pathArgument.Value.ConstantValue.Value!;
		if (ChooseFix(attributes[0].Instance, defaultBuild, path) is not { } fix)
			return;

		if (attribute.ApplicationSyntaxReference is not { } attributeReference)
			return;

		Report(context, declaration, kit, defaultBuild, fix, pathArgument.Syntax, attributeReference);
	}

	/// <summary>
	/// Chooses the fix: remove the override when the attribute declares the same path, or move the path when
	/// the attribute declares none. A different declared path is SG0018's finding, not a redundant override.
	/// </summary>
	static (string Kind, string PathProperty, string Advice)? ChooseFix(
		ResourceDefinitionAttributeData instance,
		ResourceKitRules.DefaultBuildResource defaultBuild,
		string path
	)
	{
		if (defaultBuild.Path is { } declaredPath)
		{
			if (!PathsMatch(declaredPath, path))
				return null;

			var declaredProperty = string.IsNullOrWhiteSpace(instance.ResourcePath) ? "ProjectPath" : "ResourcePath";
			return (
				RemoveFix,
				declaredProperty,
				$"ResourceKit generates the same call from {declaredProperty}, so the override can be removed"
			);
		}

		// An ignored ProjectPath on a JavaScript app (SG0041) would conflict with a moved ResourcePath.
		if (!string.IsNullOrWhiteSpace(instance.ProjectPath) || !string.IsNullOrWhiteSpace(instance.ResourcePath))
			return null;

		return (
			MoveFix,
			defaultBuild.PathPropertyName,
			$"declare the path as {defaultBuild.PathPropertyName} on [ResourceDefinition] and ResourceKit generates the call"
		);
	}

	static void Report(
		SyntaxNodeAnalysisContext context,
		MethodDeclarationSyntax declaration,
		INamedTypeSymbol kit,
		ResourceKitRules.DefaultBuildResource defaultBuild,
		(string Kind, string PathProperty, string Advice) fix,
		SyntaxNode pathArgument,
		SyntaxReference attributeReference
	)
	{
		var properties = ImmutableDictionary<string, string?>
			.Empty.Add(FixKindProperty, fix.Kind)
			.Add(PathPropertyProperty, fix.PathProperty);

		context.ReportDiagnostic(
			Diagnostic.Create(
				DiagnosticLibrary.RedundantBuildResource,
				declaration.Identifier.GetLocation(),
				// [0] the path argument to move, [1] the attribute to move it into.
				additionalLocations:
				[
					pathArgument.GetLocation(),
					attributeReference.SyntaxTree.GetLocation(attributeReference.Span),
				],
				properties: properties,
				kit.Name,
				defaultBuild.MethodName + "(...)",
				fix.Advice
			)
		);
	}

	// An expression body, or a block holding a single return statement.
	static ExpressionSyntax? SingleExpression(MethodDeclarationSyntax declaration)
	{
		if (declaration.ExpressionBody is { } expressionBody)
			return expressionBody.Expression;

		return
			declaration.Body is { Statements.Count: 1 } body
			&& body.Statements[0] is ReturnStatementSyntax { Expression: { } returned }
			? returned
			: null;
	}

	/// <summary>
	/// Matches <c>(builder, Name, constantPath, [defaults...])</c> and returns the path argument, or
	/// <see langword="null"/> when the call does anything the generated one would not.
	/// </summary>
	static IArgumentOperation? MatchArguments(IInvocationOperation invocation, IParameterSymbol builderParameter)
	{
		IArgumentOperation? pathArgument = null;
		foreach (var argument in invocation.Arguments)
		{
			if (argument.Parameter is not { } parameter)
				return null;

			var value = Unwrap(argument.Value);
			switch (parameter.Ordinal)
			{
				case 0:
					if (
						value is not IParameterReferenceOperation reference
						|| !SymbolEqualityComparer.Default.Equals(reference.Parameter, builderParameter)
					)
						return null;
					break;

				case 1:
					if (
						value
						is not IPropertyReferenceOperation
						{
							Property: { Name: "Name", IsStatic: false },
							Instance: IInstanceReferenceOperation,
						}
					)
						return null;
					break;

				case 2:
					if (
						argument.ArgumentKind != ArgumentKind.Explicit
						|| value.ConstantValue is not { HasValue: true, Value: string }
					)
						return null;
					pathArgument = argument;
					break;

				default:
					// The run script must be left at (or set to) its default, or the generated call differs.
					if (argument.ArgumentKind == ArgumentKind.DefaultValue)
						break;

					if (
						!parameter.HasExplicitDefaultValue
						|| !value.ConstantValue.HasValue
						|| !Equals(value.ConstantValue.Value, parameter.ExplicitDefaultValue)
					)
						return null;
					break;
			}
		}

		return pathArgument;
	}

	static IOperation Unwrap(IOperation operation)
	{
		while (operation is IConversionOperation { IsImplicit: true } conversion)
			operation = conversion.Operand;

		return operation;
	}

	static bool PathsMatch(string left, string right) =>
		string.Equals(left.Replace('\\', '/').Trim(), right.Replace('\\', '/').Trim(), StringComparison.Ordinal);
}
