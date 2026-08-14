using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class SourceGenLibrary
{
	public static IncrementalValueProvider<KitGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabled = IncrementalPipeline.IsDisabledValueProvider(
			context,
			PropertyLibrary.DisablePurviewAspireResourceKitSourceGeneratorPropertyName
		);
		var generationContext = IncrementalPipeline.GenerationContextValueProvider(
			context,
			$"{AssemblyInfo.AssemblyName}.{nameof(HostKitGenerator)}",
			AssemblyInfo.Version,
			(compilation, generatorName, version, _) =>
				new KitGenerationContext(compilation, generatorName, version),
			logger
		);

		var hostKits = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.HostKitAttribute,
			transform: (ctx, ct) =>
				GetSemanticTargetForGeneration(ctx, TypeLibrary.HostKitAttribute, logger, ct),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: "GetHostKitTargets"
		);
		var resourceDefinitions = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.ResourceDefinitionAttribute,
			transform: (ctx, ct) =>
				GetSemanticTargetForGeneration(
					ctx,
					TypeLibrary.ResourceDefinitionAttribute,
					logger,
					ct
				),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: "GetResourceDefinitionTargets"
		);
		var genericResourceDefinitions = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.GenericResourceDefinitionAttribute,
			transform: (ctx, ct) =>
				GetSemanticTargetForGeneration(
					ctx,
					TypeLibrary.GenericResourceDefinitionAttribute,
					logger,
					ct
				),
			predicate: (s, _) => s is ClassDeclarationSyntax,
			trackingName: "GetGenericResourceDefinitionTargets"
		);

		return isDisabled
			.CombineWith(
				generationContext,
				static (isDisabled, GenerationContext, _) =>
				{
					KitGenerationModel model = new(!isDisabled, GenerationContext);

					List<DiagnosticInfo> diagnostics = [];
					if (GenerationContext.IServiceCollection is null)
					{
						diagnostics.Add(
							GeneratorDiagnostics.Create(
								GeneratorDiagnostics.ServiceCollectionMissing
							)
						);
					}

					if (GenerationContext.ConfigurationBinder is null)
					{
						diagnostics.Add(
							GeneratorDiagnostics.Create(
								GeneratorDiagnostics.OptionDependencyMissing
							)
						);
					}

					if (diagnostics.Count > 0)
						model.Diagnostics = model.Diagnostics.AddRange(diagnostics);

					return model;
				},
				"CombineIsDisabledWithGenerationContext"
			)
			.CollectWith(
				hostKits,
				(model, hostKits, _) =>
				{
					model.HostKit = hostKits.FirstOrDefault();
					if (hostKits.Length > 1)
					{
						foreach (var hostKit in hostKits)
						{
							if (hostKit.IsSuccess)
							{
								model.Diagnostics = model.Diagnostics.Add(
									GeneratorDiagnostics.Create(
										GeneratorDiagnostics.MultipleHostKitsFoundInfo,
										hostKit.Value!.Target.Symbol
									)
								);
							}
						}
					}

					return model;
				},
				"CollectHostKits"
			)
			.CollectWith(
				resourceDefinitions,
				(model, resourceKits, _) =>
				{
					model.ResourceKits = resourceKits;
					return model;
				},
				"CollectResourceKits"
			)
			.CollectWith(
				genericResourceDefinitions,
				(model, genericResourceKits, _) =>
				{
					model.ResourceKits = model.ResourceKits.AddRange(genericResourceKits);
					return model;
				},
				"CollectGenericResourceKits"
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
		logger?.Debug(
			$"Checking target {classDeclaration.Identifier} based on {attributeType.TypeName}"
		);

		if (
			context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken)
			is not INamedTypeSymbol symbol
		)
		{
			logger?.Error($"The symbol could not be found for {classDeclaration}");
			return GeneratorResult<KitTargetDescriptor>.Empty;
		}

		if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
		{
			logger?.Error($"Declaration is not partial for {symbol.Name}");
			return GeneratorResult<KitTargetDescriptor>.Fail(
				GeneratorDiagnostics.Create(
					GeneratorDiagnostics.ClassMustBePartial,
					symbol,
					classDeclaration
				)
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
		logger?.Debug(
			isHostKit
				? $"For HostKit {symbol.Name}, values:"
				: $"For ResourceKit {symbol.Name}, values:",
			1
		);

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
		var data = HostKitAttributeData.FromAttributeData(context.Attributes);
		logger?.Debug($"Name: '{data.Name ?? "<null>"}'", 2);
		logger?.Debug($"ExtensionMethodName: '{data.ExtensionMethodName ?? "<null>"}'", 2);
		logger?.Debug($"GenerateOptions: '{data.GenerateOptions}'", 2);

		return new(
			Target: new(symbol, classDeclaration),
			IsHostKit: true,
			Name: data.Name,
			PropertyName: null,
			ExtensionName: null,
			GenerateOptions: data.GenerateOptions,
			IsGenericResourceDefinition: false,
			AspireResourceTypeSymbol: null
		);
	}

	static KitTargetDescriptor BuildResourceKitDescriptor(
		GeneratorAttributeSyntaxContext context,
		INamedTypeSymbol symbol,
		ClassDeclarationSyntax classDeclaration,
		GenerationLogger? logger
	)
	{
		var data = ResourceDefinitionAttributeData.FromAttributeData(
			context.Attributes,
			out var attribute
		);

		if (attribute is null)
		{
			logger?.Warning(
				$"No attribute data found for {symbol.Name}, this should not happen",
				1
			);
		}

		var isGenericResourceDefinition = attribute?.AttributeClass?.IsGenericType ?? false;
		var propertyName = data.PropertyName ?? CodeGenHelpers.TrimSuffix(symbol.Name);
		var aspireResourceTypeSymbol = data.AspireResourceType;

		logger?.Debug($"Name: '{data.Name ?? "<null>"}'", 2);
		logger?.Debug($"PropertyName: '{propertyName ?? "<null>"}'", 2);
		logger?.Debug($"IsGenericResourceDefinition: '{isGenericResourceDefinition}'", 2);
		logger?.Debug(
			$"AspireResourceType: '{aspireResourceTypeSymbol?.ToDisplayString() ?? "<null>"}'",
			2
		);

		if (data.AspireResourceType is null)
		{
			aspireResourceTypeSymbol = ResolveAspireResourceTypeFromBaseClass(
				symbol,
				aspireResourceTypeSymbol,
				logger
			);
		}

		return new KitTargetDescriptor(
			Target: new(symbol, classDeclaration),
			IsHostKit: false,
			Name: data.Name,
			PropertyName: propertyName,
			ExtensionName: null,
			GenerateOptions: true,
			IsGenericResourceDefinition: isGenericResourceDefinition,
			AspireResourceTypeSymbol: aspireResourceTypeSymbol
		);
	}

	static INamedTypeSymbol? ResolveAspireResourceTypeFromBaseClass(
		INamedTypeSymbol symbol,
		INamedTypeSymbol? aspireResourceTypeSymbol,
		GenerationLogger? logger
	)
	{
		logger?.Debug(
			"Checking for explicit base class for attribute-based Aspire resource type",
			1
		);

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
			logger?.Debug(
				$"Checking type parameter '{param.ToDisplayString()}' for implemented interfaces",
				3
			);
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
		if (
			classDeclaration.ParameterList is not null
			&& classDeclaration.ParameterList.Parameters.Count > 0
		)
			return true;

		foreach (
			var constructor in classDeclaration
				.Members.OfType<ConstructorDeclarationSyntax>()
				.Where(c =>
					string.Equals(c.Identifier.ValueText, className, StringComparison.Ordinal)
				)
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
