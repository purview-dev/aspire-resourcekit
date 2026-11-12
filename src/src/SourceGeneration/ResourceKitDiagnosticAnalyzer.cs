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
			DiagnosticLibrary.GroupTypeNotResourceGroup,
			DiagnosticLibrary.ResourceGroupCycle,
			DiagnosticLibrary.DuplicateResourceGroupName,
			DiagnosticLibrary.MultiplePrimaryGroups,
			DiagnosticLibrary.ResourceGroupCannotHaveExplicitBase,
			DiagnosticLibrary.DuplicateUrlName,
			DiagnosticLibrary.EndpointUrlRequiresEndpoints,
			DiagnosticLibrary.RelativeUrlWithoutEndpoints,
			DiagnosticLibrary.MultipleResourceGroupParents,
			DiagnosticLibrary.ResourceGroupCannotBeGeneric,
			DiagnosticLibrary.ResourceGroupCannotBeResourceKit,
			DiagnosticLibrary.ResourceKitCannotParentItself,
			DiagnosticLibrary.ResourceKitParentCycle,
			DiagnosticLibrary.ResourceKitParentNotResourceKit,
			DiagnosticLibrary.ResourceKitParentUnavailable,
			DiagnosticLibrary.DuplicateResourceKitChildPropertyName,
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
			ConcurrentBag<INamedTypeSymbol> resourceGroups = [];

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

					if (ResourceKitRules.HasResourceGroupAttribute(type))
					{
						resourceGroups.Add(type);
						ReportRules(
							symbolContext,
							ResourceKitRules.EvaluateResourceGroup(type, symbolContext.CancellationToken)
						);
					}
				},
				SymbolKind.NamedType
			);

			compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
			{
				ReportCompilationRules(compilationEndContext, hostKits, resourceKits);
				ReportGroupCompilationRules(compilationEndContext, resourceGroups);
			});
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

		ReportHierarchyRules(context, resourceKitArray);

		// Only roots are host properties. A child property is scoped to its parent, and SG0037 owns duplicates
		// inside that scope.
		Dictionary<string, int> seenPropertyNames = [with(StringComparer.Ordinal)];
		foreach (var resourceKit in resourceKitArray)
		{
			if (ResourceKitRules.GetParent(resourceKit) is not null)
				continue;

			var allAttributes = ResourceDefinitionAttributeData.AllAttributeData(resourceKit).ToArray();
			var matchedAttribute = allAttributes.FirstOrDefault();
			if (matchedAttribute.Attribute is null)
				continue;

			var propertyName = matchedAttribute.Instance.PropertyName
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

	static void ReportHierarchyRules(CompilationAnalysisContext context, INamedTypeSymbol[] resourceKitArray)
	{
		var ordered = resourceKitArray
			.OrderBy(static symbol => new TypeIdentity(symbol).MetadataFullName, StringComparer.Ordinal)
			.ToArray();

		var nodes = ordered
			.Select(static symbol =>
			{
				var matched = ResourceDefinitionAttributeData.AllAttributeData(symbol.GetAttributes()).FirstOrDefault();
				var propertyName = matched.Instance.PropertyName
					?? symbol.Name.TrimSuffix(TypeLibraryGenerator.TrimSuffixes)
					?? symbol.Name;
				var parent = ResourceKitRules.GetParent(symbol);

				return new ResourceKitRules.HierarchyNode(
					new TypeIdentity(symbol),
					parent is null ? TypeIdentity.Empty : new TypeIdentity(parent),
					symbol.Name,
					propertyName
				);
			})
			.ToImmutableArray();

		foreach (var finding in ResourceKitRules.EvaluateHierarchy(nodes))
		{
			var symbol = ordered[finding.NodeIndex];

			// SG0034 already explains a parent type that is not a resource kit; do not also call it unavailable.
			if (finding.Descriptor == DiagnosticLibrary.ResourceKitParentUnavailable)
			{
				var parent = ResourceKitRules.GetParent(symbol);
				if (parent is not null && !ResourceKitRules.HasResourceDefinitionAttribute(parent))
					continue;
			}

			context.ReportDiagnostic(
				Diagnostic.Create(finding.Descriptor, GetLocation(symbol) ?? Location.None, [.. finding.MessageArgs])
			);
		}
	}

	// A group's name identifies it in the dashboard, which compares names case-insensitively; its property
	// name is a member of the generated Groups accessor, which C# compares ordinally.
	static void ReportGroupCompilationRules(
		CompilationAnalysisContext context,
		ConcurrentBag<INamedTypeSymbol> resourceGroups
	)
	{
		var groups = resourceGroups.ToArray();

		var duplicates = groups
			.GroupBy(ResourceKitRules.GroupName, StringComparer.OrdinalIgnoreCase)
			.Where(static g => g.Count() > 1)
			.Concat(
				groups
					.GroupBy(ResourceKitRules.GroupPropertyName, StringComparer.Ordinal)
					.Where(static g => g.Count() > 1)
			);

		HashSet<ISymbol> reported = [with(SymbolEqualityComparer.Default)];
		foreach (var duplicate in duplicates)
		{
			foreach (var group in duplicate.Where(reported.Add))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						DiagnosticLibrary.DuplicateResourceGroupName,
						GetLocation(group) ?? Location.None,
						duplicate.Key
					)
				);
			}
		}
	}

	static Location? GetLocation(ISymbol symbol) =>
		symbol.Locations.FirstOrDefault(static location => location.IsInSource);
}
