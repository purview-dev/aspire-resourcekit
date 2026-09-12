using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class SourceGenLibrary
{
	// The aggregate generation model (value-equatable) and the per-compilation execution context,
	// kept separate so the context never participates in incremental output caching.
	internal sealed record GeneratorPipelines(
		IncrementalValueProvider<KitGenerationModel> Outputs,
		IncrementalValueProvider<GenerationContext<KitGenerationCapabilities>> Context
	);

	public static GeneratorPipelines GetGeneratorValueProviders(IncrementalGeneratorInitializationContext context)
	{
		var generationContext = IncrementalPipeline.GenerationContextValueProvider<
			KitGenerationCapabilities,
			HostKitGenerator
		>(
			context,
			static (compilation, generatorName, logger, _) =>
			{
				var hasIServivceCollection = TypeHelpers.HasType(
					compilation,
					TypeLibrary.Microsoft.Extensions.DependencyInjection.IServiceCollection
				);
				var hasConfigurationBinder = TypeHelpers.HasType(
					compilation,
					TypeLibrary.Microsoft.Extensions.Configuration.ConfigurationBinder
				);

				return new(hasIServivceCollection, hasConfigurationBinder);
			},
			PropertyLibrary.DisablePurviewAspireResourceKitSourceGeneratorPropertyName
		);
		var hostKits = GetHostKitPipeline(context);
		var resourceDefinitions = GetResourceDefinitionPipeline(context);
		var genericResourceDefinitions = GetGenerationResourceDefinitionPipeline(context);
		var allResourceKits = GetCombineResourceDefinitionsPipeline(resourceDefinitions, genericResourceDefinitions);

		var provider = generationContext
			.CollectWith(
				hostKits,
				static (_, hostKits, _) => new KitGenerationModel(hostKits, []) { HostKit = hostKits.FirstOrDefault() },
				GeneratorTrackingNames.CollectHostKits
			)
			.CombineWith(
				allResourceKits,
				CombineResourceDefinitionsPipeline(),
				GeneratorTrackingNames.CollectResourceKits
			);

		return new(provider, generationContext);
	}

	static Func<
		KitGenerationModel,
		EquatableArray<GeneratorResult<ResourceKitModel>>,
		CancellationToken,
		KitGenerationModel
	> CombineResourceDefinitionsPipeline() =>
		static (outputContext, resourceKits, cancellationToken) =>
		{
			var groupedResourceKits = resourceKits
				.Where(static r => !r.IsEmpty)
				.GroupBy(static r =>
				{
					if (r.Value.ResourceKitType.IsGlobalNamespace)
						return "<<global-namespace>>";

					// Use the namespace of the target type as the key for grouping
					return r.Value.ResourceKitType.Namespace!;
				})
				.OrderBy(static g => g.Key, StringComparer.Ordinal)
				.Select(static g => new ResourceKitGroup(
					g.Key,
					EquatableArray<GeneratorResult<ResourceKitModel>>.Create([.. g])
				))
				.ToArray();

			cancellationToken.ThrowIfCancellationRequested();

			return outputContext with
			{
				ResourceKits = EquatableArray<ResourceKitGroup>.Create(groupedResourceKits),
			};
		};

	static IncrementalValueProvider<
		EquatableArray<GeneratorResult<ResourceKitModel>>
	> GetCombineResourceDefinitionsPipeline(
		IncrementalValuesProvider<GeneratorResult<ResourceKitModel>> resourceDefinitions,
		IncrementalValuesProvider<GeneratorResult<ResourceKitModel>> genericResourceDefinitions
	) =>
		resourceDefinitions.CollectWith(
			genericResourceDefinitions,
			static (resourceKits, genericResourceKits, _) =>
				EquatableArray<GeneratorResult<ResourceKitModel>>.Create([.. resourceKits, .. genericResourceKits]),
			GeneratorTrackingNames.CombineResourceKits
		);

	static IncrementalValuesProvider<GeneratorResult<ResourceKitModel>> GetGenerationResourceDefinitionPipeline(
		IncrementalGeneratorInitializationContext context
	) =>
		IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute,
			transform: static (ctx, ct) => GetResourceKitModel(ctx, ct),
			predicate: static (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.GenericResourceDefinitionTargets
		);

	static IncrementalValuesProvider<GeneratorResult<ResourceKitModel>> GetResourceDefinitionPipeline(
		IncrementalGeneratorInitializationContext context
	) =>
		IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute,
			transform: static (ctx, ct) => GetResourceKitModel(ctx, ct),
			predicate: static (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.ResourceDefinitionTargets
		);

	static IncrementalValuesProvider<GeneratorResult<HostKitModel>> GetHostKitPipeline(
		IncrementalGeneratorInitializationContext context
	) =>
		// Get all classes decorated with the HostKitAttribute, ResourceDefinitionAttribute, or GenericResourceDefinitionAttribute
		IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute,
			transform: static (ctx, ct) => GetHostKitModel(ctx, ct),
			predicate: static (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.HostKitTargets
		);

	static GeneratorResult<HostKitModel> GetHostKitModel(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var symbol = (INamedTypeSymbol)context.TargetSymbol;

		var data = HostKitAttributeData.FromAttributeData(context.Attributes);
		TypeIdentity hostKitType = new(symbol);
		var optionsType = data.GenerateOptions
			? hostKitType.Nested(hostKitType.Name + TypeLibraryGenerator.OptionsBaseClassSuffix)
			: TypeIdentity.Empty;

		var diagnostics = ResourceKitRules
			.EvaluateHostKit(symbol, cancellationToken)
			.Select(static rule => rule.ToDiagnosticInfo())
			.ToImmutableArray();

		return GeneratorResult<HostKitModel>.Create(
			new(
				HostKitType: hostKitType,
				OptionsType: optionsType,
				ResourceKitBaseType: TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase,
				Accessibility: symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility(),
				ExtensionMethodName: data.ExtensionMethodName ?? PropertyLibrary.DefaultExtensionMethodName
			),
			diagnostics
		);
	}

	static GeneratorResult<ResourceKitModel> GetResourceKitModel(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var symbol = (INamedTypeSymbol)context.TargetSymbol;
		var allAttributes = ResourceDefinitionAttributeData.AllAttributeData(symbol.GetAttributes()).ToArray();
		var matchedAttribute = allAttributes.FirstOrDefault();

		var resourceName = matchedAttribute.Instance.Name ?? symbol.Name.TrimSuffix(TypeLibraryGenerator.TrimSuffixes);
		var hasExplicitBaseType = TypeHelpers.HasExplicitBaseType(symbol);
		var isGenericResourceDefinition = matchedAttribute.Attribute.AttributeClass!.IsGenericType;
		var propertyName =
			matchedAttribute.Instance.PropertyName ?? symbol.Name.TrimSuffix(TypeLibraryGenerator.TrimSuffixes)!;
		var (aspireResourceType, _, _) = ResourceKitRules.ResolveResourceType(matchedAttribute, symbol);

		TypeIdentity resourceKitType = new(symbol);
		var optionsType = resourceKitType.Nested(symbol.Name + TypeLibraryGenerator.OptionsBaseClassSuffix);

		var diagnostics = ResourceKitRules
			.EvaluateResourceKit(symbol, context.SemanticModel.Compilation, cancellationToken)
			.Select(static rule => rule.ToDiagnosticInfo())
			.ToImmutableArray();

		return GeneratorResult<ResourceKitModel>.Create(
			new(
				ResourceKitType: resourceKitType,
				OptionsType: optionsType,
				AspireResourceType: aspireResourceType,
				Accessibility: symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility(),
				PropertyName: propertyName,
				ResourceName: resourceName ?? "<unknown>",
				HasExplicitBaseType: hasExplicitBaseType
			),
			diagnostics
		);
	}
}
