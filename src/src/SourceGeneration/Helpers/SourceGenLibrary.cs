using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class SourceGenLibrary
{
	public static IncrementalValuesProvider<KitGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabled = IncrementalPipeline.IsDisabledValueProvider(
			context,
			PropertyLibrary.DisablePurviewAspireResourceKitSourceGeneratorPropertyName
		);
		var generationContext = IncrementalPipeline
			.GenerationContextValueProvider(
				context,
				$"{AssemblyInfo.AssemblyName}.{nameof(HostKitGenerator)}",
				AssemblyInfo.Version,
				(compilation, generatorName, version, _) =>
					new GenerationContext(compilation, generatorName, version, validateCodeWriterScopes: true),
				logger
			)
			.Select(
				static (context, _) =>
					new GenerationCapabilities(
						context.GetTypeByMetadataName(TypeLibrary.IServiceCollection) is not null,
						context.GetTypeByMetadataName(TypeLibrary.ConfigurationBinder) is not null
					)
			);

		var hostKits = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.HostKitAttribute,
			transform: (ctx, ct) => GetSemanticTargetForGeneration(ctx, TypeLibrary.HostKitAttribute, logger, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.HostKitTargets
		);
		var resourceDefinitions = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.ResourceDefinitionAttribute,
			transform: (ctx, ct) =>
				GetSemanticTargetForGeneration(ctx, TypeLibrary.ResourceDefinitionAttribute, logger, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.ResourceDefinitionTargets
		);
		var genericResourceDefinitions = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.GenericResourceDefinitionAttribute,
			transform: (ctx, ct) =>
				GetSemanticTargetForGeneration(ctx, TypeLibrary.GenericResourceDefinitionAttribute, logger, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: GeneratorTrackingNames.GenericResourceDefinitionTargets
		);

		var allHostKits = hostKits.Collect();
		var allResourceKits = resourceDefinitions.CollectWith(
			genericResourceDefinitions,
			static (resourceKits, genericResourceKits, _) =>
				EquatableArray<GeneratorResult<KitTargetDescriptor>>.Create([.. resourceKits, .. genericResourceKits]),
			GeneratorTrackingNames.CollectGenericResourceKits
		);

		return hostKits
			.CombineWith(
				allHostKits,
				static (hostKit, hostKitCollection, _) =>
				{
					var diagnostics = new List<DiagnosticInfo>();
					if (hostKitCollection.Length > 1 && hostKit.IsSuccess)
						diagnostics.Add(
							DiagnosticInfo.Create(
								GeneratorDiagnostics.MultipleHostKitsFoundInfo,
								hostKit.Value!.TypeName
							)
						);

					return (hostKit, EquatableArray<DiagnosticInfo>.Create([.. diagnostics]));
				},
				GeneratorTrackingNames.CollectHostKits
			)
			.CombineWith(
				generationContext,
				static (hostState, capabilities, _) =>
				{
					var diagnostics = new List<DiagnosticInfo>(hostState.Item2);
					if (!capabilities.HasIServiceCollection)
						diagnostics.Add(DiagnosticInfo.Create(GeneratorDiagnostics.ServiceCollectionMissing));
					if (!capabilities.HasConfigurationBinder)
						diagnostics.Add(DiagnosticInfo.Create(GeneratorDiagnostics.OptionDependencyMissing));

					return new KitGenerationModel(
						true,
						capabilities.HasIServiceCollection,
						capabilities.HasConfigurationBinder
					)
					{
						HostKit = hostState.hostKit,
						Diagnostics = EquatableArray<DiagnosticInfo>.Create([.. diagnostics]),
					};
				},
				GeneratorTrackingNames.CombineCapabilities
			)
			.CombineWith(
				allResourceKits,
				static (model, resources, _) => model with { ResourceKits = resources },
				GeneratorTrackingNames.CollectResourceKits
			)
			.CombineWith(
				isDisabled,
				static (model, disabled, _) => model with { IsSourceGeneratorEnabled = !disabled },
				GeneratorTrackingNames.ApplyDisabled
			);
	}

	static GeneratorResult<KitTargetDescriptor> GetSemanticTargetForGeneration(
		GeneratorAttributeSyntaxContext context,
		TypeValueObject attributeType,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		logger?.Debug($"Checking target {classDeclaration.Identifier} based on {attributeType.TypeName}");

		if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol symbol)
		{
			logger?.Error($"The symbol could not be found for {classDeclaration}");
			return GeneratorResult<KitTargetDescriptor>.Empty;
		}

		if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
		{
			logger?.Error($"Declaration is not partial for {symbol.Name}");
			return GeneratorResult<KitTargetDescriptor>.Fail(
				GeneratorDiagnostics.Create(GeneratorDiagnostics.ClassMustBePartial, symbol, classDeclaration)
			);
		}

		if (HasNonEmptyConstructors(classDeclaration, symbol.Name))
		{
			logger?.Error($"Declaration has non-empty constructors for {symbol.Name}");
			return GeneratorResult<KitTargetDescriptor>.Fail(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.NonEmptyConstructorsNotSupported,
					classDeclaration.Identifier.GetLocation(),
					symbol.Name
				)
			);
		}

		var isHostKit = attributeType == TypeLibrary.HostKitAttribute;

		logger?.Debug($"Processing Attribute: {attributeType.MetadataFullName}");
		logger?.Debug(isHostKit ? $"For HostKit {symbol.Name}, values:" : $"For ResourceKit {symbol.Name}, values:", 1);

		var result = isHostKit
			? BuildHostKitDescriptor(context, symbol, classDeclaration, logger)
			: BuildResourceKitDescriptor(context, symbol, classDeclaration, logger);

		return GeneratorResult<KitTargetDescriptor>.Ok(result);
	}

	static KitTargetDescriptor BuildHostKitDescriptor(
		GeneratorAttributeSyntaxContext context,
		INamedTypeSymbol symbol,
		ClassDeclarationSyntax classDeclaration,
		GenerationLogger? logger
	)
	{
		_ = classDeclaration;
		var data = HostKitAttributeData.FromAttributeData(context.Attributes);
		logger?.Debug($"Name: '{data.Name ?? "<null>"}'", 2);
		logger?.Debug($"ExtensionMethodName: '{data.ExtensionMethodName ?? "<null>"}'", 2);
		logger?.Debug($"GenerateOptions: '{data.GenerateOptions}'", 2);
		var type = new TypeValueObject(symbol);

		return new(
			TypeName: type.TypeName,
			Namespace: type.Namespace ?? string.Empty,
			MetadataFullName: type.MetadataFullName,
			AccessibilityModifier: symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility()!.Value,
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
		INamedTypeSymbol symbol,
		ClassDeclarationSyntax classDeclaration,
		GenerationLogger? logger
	)
	{
		var data = ResourceDefinitionAttributeData.FromAttributeData(context.Attributes, out var attribute);

		if (attribute is null)
		{
			logger?.Warning($"No attribute data found for {symbol.Name}, this should not happen", 1);
		}

		var isGenericResourceDefinition = attribute?.AttributeClass?.IsGenericType ?? false;
		var propertyName = data.PropertyName ?? CodeGenHelpers.TrimSuffix(symbol.Name);
		var aspireResourceTypeSymbol = data.AspireResourceType;

		logger?.Debug($"Name: '{data.Name ?? "<null>"}'", 2);
		logger?.Debug($"PropertyName: '{propertyName ?? "<null>"}'", 2);
		logger?.Debug($"IsGenericResourceDefinition: '{isGenericResourceDefinition}'", 2);
		logger?.Debug($"AspireResourceType: '{aspireResourceTypeSymbol?.ToDisplayString() ?? "<null>"}'", 2);

		if (data.AspireResourceType is null)
		{
			aspireResourceTypeSymbol = ResolveAspireResourceTypeFromBaseClass(symbol, aspireResourceTypeSymbol, logger);
		}

		var type = new TypeValueObject(symbol);
		var target = new TargetSymbolDescriptor(symbol, classDeclaration);
		TypeModel? aspireResourceType = aspireResourceTypeSymbol is null ? null : ToTypeModel(aspireResourceTypeSymbol);

		return new KitTargetDescriptor(
			TypeName: type.TypeName,
			Namespace: type.Namespace ?? string.Empty,
			MetadataFullName: type.MetadataFullName,
			AccessibilityModifier: symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility()!.Value,
			IsHostKit: false,
			Name: data.Name,
			PropertyName: propertyName,
			ExtensionName: null,
			GenerateOptions: true,
			IsGenericResourceDefinition: isGenericResourceDefinition,
			AspireResourceType: aspireResourceType,
			HasExplicitBaseType: TypeHelpers.HasExplicitBaseType(target),
			IsDerivedFromExpectedBase: !TypeHelpers.HasExplicitBaseType(target)
				|| TypeHelpers.IsDerivedFromExpectedBase(
					target,
					TypeLibrary.ResourceKitBase.MakeGeneric(TypeLibrary.IResource)
				)
		);
	}

	static TypeModel ToTypeModel(INamedTypeSymbol symbol)
	{
		var type = new TypeValueObject(symbol);
		var @namespace = type.Namespace ?? string.Empty;
		var renderFullName = string.IsNullOrEmpty(@namespace) ? $"global::{type.TypeName}" : type.RenderFullName;
		return new(type.TypeName, @namespace, type.MetadataFullName, renderFullName);
	}

	static INamedTypeSymbol? ResolveAspireResourceTypeFromBaseClass(
		INamedTypeSymbol symbol,
		INamedTypeSymbol? aspireResourceTypeSymbol,
		GenerationLogger? logger
	)
	{
		logger?.Debug("Checking for explicit base class for attribute-based Aspire resource type", 1);

		if (symbol.BaseType is null || symbol.BaseType.TypeParameters.Length == 0)
		{
			logger?.Debug("No explicit base class found for non-generic resource definition", 2);
			return aspireResourceTypeSymbol;
		}

		logger?.Debug(
			$"Found explicit base class '{symbol.BaseType.Name}' (Type Argument Count: {symbol.BaseType.TypeArguments.Length}), checking for base-class defined Aspire resource type",
			2
		);

		foreach (var param in symbol.BaseType.TypeArguments)
		{
			logger?.Debug($"Checking type parameter '{param.ToDisplayString()}' for implemented interfaces", 3);
			foreach (var @interface in param.AllInterfaces)
			{
				var t = new TypeValueObject(@interface);
				if (t == TypeLibrary.IResource)
				{
					logger?.Debug($"Found Aspire Resource '{param.Name}' implements IResource", 3);
					return (INamedTypeSymbol)param;
				}
			}
		}

		logger?.Warning(
			"No Aspire resource type found in base class type parameters for non-generic resource definition",
			2
		);

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
