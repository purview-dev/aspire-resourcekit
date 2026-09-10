using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Reports ResourceKit model diagnostics (SG0001-SG0017) directly from the compilation so the source
/// generator only needs to gate generation. Rules are evaluated through <see cref="ResourceKitRules"/> so
/// the analyzer and generator share a single set of rule definitions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResourceKitDiagnosticAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[
			DiagnosticLibrary.ClassMustBePartial,
			DiagnosticLibrary.NoResourceKitsDefined,
			DiagnosticLibrary.MultipleHostKitsFoundInfo,
			DiagnosticLibrary.DuplicateResourcePropertyName,
			DiagnosticLibrary.ResourceMustDeriveFromResourceKitBase,
			DiagnosticLibrary.ResourceNameNotDerivable,
			DiagnosticLibrary.InvalidPropertyName,
			DiagnosticLibrary.NonEmptyConstructorsNotSupported,
			DiagnosticLibrary.MixedResourceDefinitionAttributesNotSupported,
			DiagnosticLibrary.NonGenericResourceDefinitionRequiresExplicitBase,
			DiagnosticLibrary.GenericResourceDefinitionCannotHaveExplicitBase,
			DiagnosticLibrary.NoAspireResourceFound,
			DiagnosticLibrary.ResourcePropertyNeverSet,
			DiagnosticLibrary.ProjectDefinitionMismatch,
			DiagnosticLibrary.ProjectResourceKitBaseMismatch,
		];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationStartContext =>
		{
			ConcurrentBag<INamedTypeSymbol> hostKits = [];
			ConcurrentBag<INamedTypeSymbol> resourceKits = [];

			compilationStartContext.RegisterSymbolAction(
				symbolContext =>
				{
					if (symbolContext.Symbol is not INamedTypeSymbol type || type.TypeKind != TypeKind.Class)
						return;

					var isHostKit = TypeHelpers.HasAttribute(
						type,
						TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute
					);
					if (isHostKit)
					{
						hostKits.Add(type);
						ReportRules(
							symbolContext,
							ResourceKitRules.EvaluateHostKit(type, symbolContext.CancellationToken)
						);
					}

					var isResourceKit =
						TypeHelpers.HasAttribute(
							type,
							TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute
						)
						|| TypeHelpers.HasAttribute(
							type,
							TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute
						);
					if (isResourceKit)
					{
						resourceKits.Add(type);
						ReportRules(
							symbolContext,
							ResourceKitRules.EvaluateResourceKit(
								type,
								symbolContext.Compilation,
								symbolContext.CancellationToken
							)
						);
					}
				},
				SymbolKind.NamedType
			);

			compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
				ReportCompilationRules(compilationEndContext, hostKits, resourceKits)
			);
		});
	}

	static void ReportRules(SymbolAnalysisContext context, ImmutableArray<ResourceKitRules.RuleEvaluation> rules)
	{
		foreach (var rule in rules)
			context.ReportDiagnostic(rule.ToDiagnostic());
	}

	static void ReportCompilationRules(
		CompilationAnalysisContext context,
		ConcurrentBag<INamedTypeSymbol> hostKits,
		ConcurrentBag<INamedTypeSymbol> resourceKits
	)
	{
		var hostKitArray = hostKits.ToArray();
		var resourceKitArray = resourceKits.ToArray();

		if (hostKitArray.Length > 1)
		{
			foreach (var hostKit in hostKitArray)
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						DiagnosticLibrary.MultipleHostKitsFoundInfo,
						GetLocation(hostKit) ?? Location.None
					)
				);
			}
		}

		if (hostKitArray.Length == 1 && resourceKitArray.Length == 0)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					DiagnosticLibrary.NoResourceKitsDefined,
					GetLocation(hostKitArray[0]) ?? Location.None,
					hostKitArray[0].Name
				)
			);
		}

		Dictionary<string, int> seenPropertyNames = new(StringComparer.Ordinal);
		foreach (var resourceKit in resourceKitArray)
		{
			var allAttributes = ResourceDefinitionAttributeData.AllAttributeData(resourceKit).ToArray();
			var matchedAttribute = allAttributes.FirstOrDefault();
			if (matchedAttribute.Attribute is null)
				continue;

			var propertyName =
				matchedAttribute.Instance.PropertyName
				?? resourceKit.Name.TrimSuffix(TypeLibraryGenerator.TrimSuffixes);
			if (string.IsNullOrWhiteSpace(propertyName))
				continue;

			seenPropertyNames[propertyName!] =
				(seenPropertyNames.TryGetValue(propertyName!, out var count) ? count : 0) + 1;
		}

		foreach (var duplicatePropertyName in seenPropertyNames.Where(static kvp => kvp.Value > 1))
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					DiagnosticLibrary.DuplicateResourcePropertyName,
					Location.None,
					duplicatePropertyName.Key
				)
			);
		}
	}

	static Location? GetLocation(ISymbol symbol) =>
		symbol.Locations.FirstOrDefault(static location => location.IsInSource);
}
