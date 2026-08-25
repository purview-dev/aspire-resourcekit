using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;
using System.Collections.Immutable;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class SourceGenLibrary
{
	public static IncrementalValueProvider<KitGenerationCollectionResults> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context
	)
	{
		var generationContext = IncrementalPipeline
			.GenerationContextValueProvider<KitGenerationContext>(
				context,
				$"{AssemblyInfo.AssemblyName}.{nameof(HostKitGenerator)}",
				AssemblyInfo.Version,
				(compilation, generatorName, logger, _) =>
					new(compilation, generatorName, logger),
				PropertyLibrary.DisablePurviewAspireResourceKitSourceGeneratorPropertyName
			);

		// Get all classes decorated with the HostKitAttribute, ResourceDefinitionAttribute, or GenericResourceDefinitionAttribute
		var hostKits = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.HostKitAttribute,
			transform: (ctx, ct) => GetSemanticTargetForGeneration(ctx, TypeLibrary.HostKitAttribute, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.HostKitTargets
		);

		var resourceDefinitions = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.ResourceDefinitionAttribute,
			transform: (ctx, ct) =>
				GetSemanticTargetForGeneration(ctx, TypeLibrary.ResourceDefinitionAttribute, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.ResourceDefinitionTargets
		);

		var genericResourceDefinitions = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.GenericResourceDefinitionAttribute,
			transform: (ctx, ct) =>
				GetSemanticTargetForGeneration(ctx, TypeLibrary.GenericResourceDefinitionAttribute, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.GenericResourceDefinitionTargets
		);

		var allResourceKits = resourceDefinitions.CollectWith(
			genericResourceDefinitions,
			static (resourceKits, genericResourceKits, _) =>
				EquatableArray<GeneratorResult<KitTargetDescriptor>>.Create([.. resourceKits, .. genericResourceKits]),
			GeneratorTrackingNames.CombineResourceKits
		);

		var provider =
			generationContext.CollectWith(hostKits, static (context, hostKits, _) => new KitGenerationCollectionResults(context, hostKits), GeneratorTrackingNames.CollectHostKits)
			.CombineWith(allResourceKits,
				static (model, resourceKits, _) =>
				{
					var groupedResourceKits = resourceKits.GroupBy(static r =>
					{
						if (r.Value is null)
							return "<<missing-value>>";
						if (r.Value.Target.Identity.IsGlobalNamespace)
							return "<<global-namespace>>";

						// Use the namespace of the target type as the key for grouping
						return r.Value.Target.Identity.Namespace!;
					}).ToImmutableDictionary(static g => g.Key, static g => g.ToImmutableArray());

					return model with { ResourceKits = groupedResourceKits };
				},
				GeneratorTrackingNames.CollectResourceKits
			);

			return provider;
	}

	static GeneratorResult<KitTargetDescriptor> GetSemanticTargetForGeneration(
		GeneratorAttributeSyntaxContext context,
		TypeIdentity attributeType,
		CancellationToken cancellationToken
	)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;

		if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol symbol)
			return GeneratorResult<KitTargetDescriptor>.Empty;

		if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
		{
			return GeneratorResult<KitTargetDescriptor>.Fail(
				GeneratorDiagnostics.Create(GeneratorDiagnostics.ClassMustBePartial, symbol, classDeclaration)
			);
		}

		if (HasNonEmptyConstructors(classDeclaration, symbol.Name))
		{
			return GeneratorResult<KitTargetDescriptor>.Fail(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.NonEmptyConstructorsNotSupported,
					classDeclaration.Identifier.GetLocation(),
					symbol.Name
				)
			);
		}

		var isHostKit = attributeType == TypeLibrary.HostKitAttribute;
		var result = isHostKit
			? BuildHostKitDescriptor(context, symbol)
			: BuildResourceKitDescriptor(context, symbol);

		return GeneratorResult<KitTargetDescriptor>.Ok(result);
	}

	static KitTargetDescriptor BuildHostKitDescriptor(
		GeneratorAttributeSyntaxContext context,
		INamedTypeSymbol symbol
	)
	{
		var data = HostKitAttributeData.FromAttributeData(context.Attributes);
		var type = TypeReference.Create(symbol);

		return new(

			Target: type,
			IsHostKit: true,
			Name: data.Name,
			PropertyName: null,
			ExtensionName: null,
			GenerateOptions: data.GenerateOptions,
			IsGenericResourceDefinition: false,
			AspireResourceType: null,
			HasExplicitBaseType: false,
			IsDerivedFromExpectedBase: true
		);
	}

	static KitTargetDescriptor BuildResourceKitDescriptor(
		GeneratorAttributeSyntaxContext context,
		INamedTypeSymbol symbol
	)
	{
		var data = ResourceDefinitionAttributeData.FromAttributeData(context.Attributes, out var attribute);

		var isGenericResourceDefinition = attribute?.AttributeClass?.IsGenericType ?? false;
		var propertyName = data.PropertyName ?? CodeGenHelpers.TrimSuffix(symbol.Name);
		var aspireResourceTypeSymbol = data.AspireResourceType;

		if (data.AspireResourceType is null)
		{
			aspireResourceTypeSymbol = ResolveAspireResourceTypeFromBaseClass(symbol, aspireResourceTypeSymbol);
		}

		var type = TypeReference.Create(symbol);
		var aspireResourceType = aspireResourceTypeSymbol is null ? null : TypeReference.Create(aspireResourceTypeSymbol);

		var hasExplicitBaseType = TypeHelpers.HasExplicitBaseType(symbol);
		var derivesFromIResource = !hasExplicitBaseType && TypeHelpers.IsDerivedFromExpectedBase(
					symbol,
					TypeLibrary.ResourceKitBase.MakeGeneric(TypeLibrary.IResource)
				);

		return new KitTargetDescriptor(
			Target: type,
			IsHostKit: false,
			Name: data.Name,
			PropertyName: propertyName,
			ExtensionName: null,
			GenerateOptions: true,
			IsGenericResourceDefinition: isGenericResourceDefinition,
			AspireResourceType: aspireResourceType,
			HasExplicitBaseType: hasExplicitBaseType,
			IsDerivedFromExpectedBase: derivesFromIResource
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
