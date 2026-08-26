using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class SourceGenLibrary
{
	public static IncrementalValueProvider<KitGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context
	)
	{
		var generationContext = IncrementalPipeline.GenerationContextValueProvider<
			KitGenerationCapabilities,
			HostKitGenerator
		>(
			context,
			static (compilation, generatorName, logger, _) =>
			{
				var hasIServivceCollection = TypeHelpers.HasType(compilation, TypeLibrary.IServiceCollection);
				var hasConfigurationBinder = TypeHelpers.HasType(compilation, TypeLibrary.ConfigurationBinder);

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
				static (context, hostKits, ct) =>
				{
					var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
					if (hostKits.Length > 1)
					{
						diagnostics.Add(
							DiagnosticInfo.Create(
								DiagnosticLibrary.MultipleHostKitsFoundInfo,
								hostKits.Where(m => !m.IsEmpty).Select(m => m.Value.SyntaxRef),
								ct
							)
						);
					}

					return new KitGenerationModel(context, hostKits, diagnostics.ToImmutable())
					{
						HostKit = hostKits.FirstOrDefault(),
					};
				},
				GeneratorTrackingNames.CollectHostKits
			)
			.CombineWith(
				allResourceKits,
				CombineResourceDefinitionsPipeline(),
				GeneratorTrackingNames.CollectResourceKits
			);

		return provider;
	}

	static Func<
		KitGenerationModel,
		EquatableArray<GeneratorResult<ResourceKitModel>>,
		CancellationToken,
		KitGenerationModel
	> CombineResourceDefinitionsPipeline() =>
		static (outputContext, resourceKits, cancellationToken) =>
		{
			ConcurrentDictionary<string, int> seenPropertyNames = new(StringComparer.Ordinal);
			var groupedResourceKits = resourceKits
				.GroupBy(r =>
				{
					if (r.IsEmpty)
						return "<<missing-value>>";

					if (r.Value.ResourceKitType.IsGlobalNamespace)
						return "<<global-namespace>>";

					if (!string.IsNullOrWhiteSpace(r.Value.PropertyName))
						seenPropertyNames.AddOrUpdate(r.Value.PropertyName, 1, (_, count) => count + 1);

					// Use the namespace of the target type as the key for grouping
					return r.Value.ResourceKitType.Namespace!;
				})
				.ToImmutableDictionary(static g => g.Key, static g => g.ToImmutableArray());

			cancellationToken.ThrowIfCancellationRequested();

			if (seenPropertyNames.Any(kvp => kvp.Value > 1))
			{
				var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
				diagnostics.AddRange(outputContext.Diagnostics);

				foreach (
					var duplicatePropertyName in seenPropertyNames.Where(kvp => kvp.Value > 1).Select(kvp => kvp.Key)
				)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(DiagnosticLibrary.DuplicateResourcePropertyName, duplicatePropertyName)
					);
				}

				outputContext = outputContext with { Diagnostics = diagnostics.ToImmutable() };
			}

			cancellationToken.ThrowIfCancellationRequested();

			return outputContext with
			{
				ResourceKits = groupedResourceKits,
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
			TypeLibrary.GenericResourceDefinitionAttribute,
			transform: static (ctx, ct) => GetResourceKitModel(ctx, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.GenericResourceDefinitionTargets
		);

	static IncrementalValuesProvider<GeneratorResult<ResourceKitModel>> GetResourceDefinitionPipeline(
		IncrementalGeneratorInitializationContext context
	) =>
		IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.ResourceDefinitionAttribute,
			transform: static (ctx, ct) => GetResourceKitModel(ctx, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.ResourceDefinitionTargets
		);

	static IncrementalValuesProvider<GeneratorResult<HostKitModel>> GetHostKitPipeline(
		IncrementalGeneratorInitializationContext context
	) =>
		// Get all classes decorated with the HostKitAttribute, ResourceDefinitionAttribute, or GenericResourceDefinitionAttribute
		IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.HostKitAttribute,
			transform: static (ctx, ct) => GetHostKitModel(ctx, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.HostKitTargets
		);

	static GeneratorResult<HostKitModel> GetHostKitModel(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		var symbol = (INamedTypeSymbol)context.TargetSymbol;

		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
		if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
		{
			diagnostics.Add(DiagnosticInfo.Create(DiagnosticLibrary.ClassMustBePartial, symbol, classDeclaration));
		}

		if (HasNonEmptyConstructors(classDeclaration, symbol.Name))
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.NonEmptyConstructorsNotSupported,
					classDeclaration.Identifier.GetLocation(),
					symbol.Name
				)
			);
		}

		var data = HostKitAttributeData.FromAttributeData(context.Attributes);
		TypeIdentity hostKitType = new(symbol);
		var optionsType = data.GenerateOptions ? hostKitType.Nested(hostKitType.Name + "Options") : TypeIdentity.Empty;

		return GeneratorResult<HostKitModel>.Ok(
			new(
				HostKitType: hostKitType,
				OptionsType: optionsType,
				ResourceKitBaseType: TypeLibrary.ResourceKitBase,
				Accessibility: symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility(),
				ExtensionMethodName: data.ExtensionMethodName ?? PropertyLibrary.DefaultExtensionMethodName,
				SyntaxRef: classDeclaration.GetReference()
			),
			diagnostics.ToImmutable()
		);
	}

	static GeneratorResult<ResourceKitModel> GetResourceKitModel(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var symbol = (INamedTypeSymbol)context.TargetSymbol;
		var data = ResourceDefinitionAttributeData.FromAttributeData(context.Attributes, out var attribute);

		var resourceName = data.Name ?? symbol.Name.TrimSuffix(TypeLibrary.TrimSuffixes);
		var hasExplicitBaseType = TypeHelpers.HasExplicitBaseType(symbol);
		var isDerivedFromExpectedBase =
			hasExplicitBaseType && TypeHelpers.IsDerivedFromExpectedBase(symbol, TypeLibrary.ResourceKitBase);
		var isGenericResourceDefinition = attribute!.AttributeClass!.IsGenericType;
		var propertyName = data.PropertyName ?? symbol.Name.TrimSuffix(TypeLibrary.TrimSuffixes)!;

		var aspireResourceTypeSymbol = data.AspireResourceType;
		if (data.AspireResourceType is null)
			aspireResourceTypeSymbol = ResolveAspireResourceTypeFromBaseClass(symbol, aspireResourceTypeSymbol);

		TypeIdentity resourceKitType = new(symbol);
		var optionsType = resourceKitType.Nested(symbol.Name + "Options");
		var aspireResourceType = aspireResourceTypeSymbol is null ? TypeIdentity.Empty : new(aspireResourceTypeSymbol);

		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
		if (isGenericResourceDefinition && hasExplicitBaseType)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(DiagnosticLibrary.GenericResourceDefinitionCannotHaveExplicitBase, symbol)
			);
		}
		else if (!isGenericResourceDefinition && !hasExplicitBaseType)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.NonGenericResourceDefinitionRequiresExplicitBase,
					symbol,
					TypeLibrary.ResourceKitBase.MetadataFullName
				)
			);
		}

		if (string.IsNullOrWhiteSpace(resourceName))
		{
			diagnostics.Add(DiagnosticInfo.Create(DiagnosticLibrary.ResourceNameNotDerivable, symbol));
		}

		if (!TypeHelpers.IsValidIdentifier(propertyName))
		{
			diagnostics.Add(DiagnosticInfo.Create(DiagnosticLibrary.InvalidPropertyName, symbol, propertyName));
		}

		if (hasExplicitBaseType && !isDerivedFromExpectedBase)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.ResourceMustDeriveFromResourceKitBase,
					symbol,
					TypeLibrary.ResourceKitBase.MetadataFullName
				)
			);
		}

		if (aspireResourceType == TypeIdentity.Empty)
		{
			diagnostics.Add(DiagnosticInfo.Create(DiagnosticLibrary.NoAspireResourceFound, symbol));
		}

		return GeneratorResult<ResourceKitModel>.Ok(
			new(
				ResourceKitType: resourceKitType,
				OptionsType: optionsType,
				AspireResourceType: aspireResourceType,
				Accessibility: symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility(),
				PropertyName: propertyName,
				ResourceName: resourceName ?? "<unknown>",
				HasExplicitBaseType: hasExplicitBaseType,
				SyntaxRef: context.TargetNode.GetReference()
			),
			diagnostics.ToImmutable()
		);
	}

	static INamedTypeSymbol? ResolveAspireResourceTypeFromBaseClass(
		INamedTypeSymbol symbol,
		INamedTypeSymbol? aspireResourceTypeSymbol
	)
	{
		if (symbol.BaseType is null || symbol.BaseType.TypeParameters.Length == 0)
			return aspireResourceTypeSymbol;

		foreach (var param in symbol.BaseType.TypeArguments)
		{
			foreach (var @interface in param.AllInterfaces)
			{
				var t = new TypeIdentity(@interface);
				if (t == TypeLibrary.IResource)
					return (INamedTypeSymbol)param;
			}
		}

		return aspireResourceTypeSymbol;
	}

	static bool HasNonEmptyConstructors(ClassDeclarationSyntax classDeclaration, string className)
	{
		if (classDeclaration.ParameterList is not null && classDeclaration.ParameterList.Parameters.Count > 0)
			return true;

		foreach (
			var constructor in classDeclaration
				.Members.OfType<ConstructorDeclarationSyntax>()
				.Where(c => string.Equals(c.Identifier.ValueText, className, StringComparison.Ordinal))
		)
		{
			if (constructor.ParameterList.Parameters.Count > 0)
				return true;

			if (constructor.ExpressionBody is not null || constructor.Initializer is not null)
				return true;

			if (constructor.Body is not null && constructor.Body.Statements.Count > 0)
				return true;
		}

		return false;
	}
}
