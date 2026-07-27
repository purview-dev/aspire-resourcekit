using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class SourceGenHelpers
{
	public const string DisablePurviewAspireResourceKitSourceGeneratorPropertyName =
		"DisablePurviewAspireResourceKitSourceGenerator";

	static readonly ConcurrentDictionary<int, string> TabCache = new();

	public static string GetSpacing(int tabs, string? message = null) =>
		(tabs <= 0 ? string.Empty : TabCache.GetOrAdd(tabs, t => new string(' ', t))) + message;

	public static IncrementalValueProvider<GenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabledValueProvider = IsSourceGeneratorDisabledValueProvider(context, logger);
		var generationContextValueProvider = GetGeneratorValueProvider(context, logger);
		var hostAppValueProvider = GetGenerationValueProviders(context, TypeHelpers.HostKitAttribute, logger);
		var appResourceValueProvider = GetGenerationValueProviders(
			context,
			TypeHelpers.ResourceDefinitionAttribute,
			logger
		);
		var genericAppResourceValueProvider = GetGenerationValueProviders(
			context,
			TypeHelpers.GenericResourceDefinitionAttribute,
			logger
		);

		return isDisabledValueProvider
			.Combine(generationContextValueProvider)
			.Combine(hostAppValueProvider.Collect())
			.Combine(appResourceValueProvider.Collect().Combine(genericAppResourceValueProvider.Collect()))
			.Select(
				static (nested, _) =>
				{
					var (((isDisabled, generationContext), hostApps), (appResources, genericAppResources)) = nested;
					var resourceKits = appResources.AddRange(genericAppResources);

					generationContext.Logger?.Debug("Combined all value providers:");
					generationContext.Logger?.Debug($"Disabled: {isDisabled}", 1);
					generationContext.Logger?.Debug($"Host Kits: {hostApps.Length}", 1);
					generationContext.Logger?.Debug($"Resource Kits: {resourceKits.Length}", 1);
					generationContext.Logger?.Debug("Generation Context:", 1);

					foreach (var info in generationContext.GetDebugInfo())
						generationContext.Logger?.Debug(info, 2);

					List<DiagnosticInfo> diagnostics = [];
					if (hostApps.Length > 1)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.MultipleHostKitsFoundInfo));
					if (generationContext.IServiceCollection is null)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.ServiceCollectionMissing));

					if (generationContext.ConfigurationBinder is null)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.OptionDependencyMissing));

					return new GenerationModel(
						IsSourceGeneratorEnabled: !isDisabled,
						GenerationContext: generationContext,
						HostKit: hostApps.FirstOrDefault(),
						ResourceKits: resourceKits,
						Diagnostics: [.. diagnostics]
					);
				}
			);
	}

	static IncrementalValuesProvider<GeneratorResult<TargetSymbolDescriptor>> GetGenerationValueProviders(
		IncrementalGeneratorInitializationContext context,
		TypeValueObject attributeType,
		GenerationLogger? logger
	)
	{
		logger?.Debug($"Generating value providers for {attributeType}");

		var targetSymbols = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				attributeType.SymbolFullName,
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: (ctx, ct) => GetSemanticTargetForGeneration(ctx, attributeType, logger, ct)
			)
			.WithTrackingName($"Get{attributeType.TypeName}Targets");

		return targetSymbols;

		static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax;

		static GeneratorResult<TargetSymbolDescriptor> GetSemanticTargetForGeneration(
			GeneratorAttributeSyntaxContext context,
			TypeValueObject attributeType,
			GenerationLogger? logger,
			CancellationToken cancellationToken
		)
		{
			var declaration = (TypeDeclarationSyntax)context.TargetNode;
			var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
			var generationContext = GenerationContext.Create(
				context.SemanticModel.Compilation,
				logger,
				cancellationToken
			);
			logger?.Debug($"Checking target {declaration.Identifier} based on {attributeType.TypeName}");

			if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol symbol)
			{
				logger?.Error($"The symbol could not be found for {declaration}");
				return GeneratorResult<TargetSymbolDescriptor>.Empty;
			}

			if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
			{
				logger?.Error($"Declaration is not partial for {symbol.Name}");
				return GeneratorResult<TargetSymbolDescriptor>.Fail(
					GeneratorDiagnostics.Create(GeneratorDiagnostics.ClassMustBePartial, symbol, declaration)
				);
			}

			if (HasNonEmptyConstructors(classDeclaration, symbol.Name))
			{
				logger?.Error($"Declaration has non-empty constructors for {symbol.Name}");
				return GeneratorResult<TargetSymbolDescriptor>.Fail(
					DiagnosticInfo.Create(
						GeneratorDiagnostics.NonEmptyConstructorsNotSupported,
						declaration.Identifier.GetLocation(),
						symbol.Name
					)
				);
			}

			var isHostKit = attributeType == TypeHelpers.HostKitAttribute;

			logger?.Debug($"Processing Attribute: {attributeType.SymbolFullName}");
			if (isHostKit)
				logger?.Debug($"For HostKit {symbol.Name}, values:", 1);
			else
				logger?.Debug($"For ResourceKit {symbol.Name}, values:", 1);

			string? name;
			string? propertyName = null;
			string? extensionMethodName = null;
			var generateOptions = true;
			var isGenericResourceDefinition = false;
			INamedTypeSymbol? aspireResourceTypeSymbol = null;

			if (isHostKit)
			{
				var data = HostKitAttributeData.FromAttributeData(generationContext, context.Attributes);
				name = data.Name;
				generateOptions = data.GenerateOptions;

				logger?.Debug($"Name: '{name ?? "<null>"}'", 2);
				logger?.Debug($"ExtensionMethodName: '{data.ExtensionMethodName ?? "<null>"}'", 2);
				logger?.Debug($"GenerateOptions: '{generateOptions}'", 2);
			}
			else
			{
				var data = ResourceDefinitionAttributeData.FromAttributeData(generationContext, context.Attributes);
				name = data.Name;
				propertyName = data.PropertyName;
				isGenericResourceDefinition = data.IsGeneric;
				aspireResourceTypeSymbol = data.AspireResourceType;

				logger?.Debug($"Name: '{name ?? "<null>"}'", 2);
				logger?.Debug($"PropertyName: '{propertyName ?? "<null>"}'", 2);
				logger?.Debug($"GenericAttribute: '{isGenericResourceDefinition}'", 2);
				logger?.Debug($"AspireResourceType: '{aspireResourceTypeSymbol?.ToDisplayString() ?? "<null>"}'", 2);

				if (!isGenericResourceDefinition)
				{
					logger?.Debug("Checking for explicit base class for non-generic resource definition", 1);
					if (symbol.BaseType is not null && symbol.BaseType.TypeParameters.Length > 0)
					{
						foreach (var param in symbol.BaseType.TypeParameters)
						{
							foreach (var @interface in param.AllInterfaces)
							{
								TypeValueObject t = new(@interface);
								if (t == TypeHelpers.IResource)
								{
									logger?.Debug($"Found Aspire Resource '{param.Name}' implements IResource", 2);
									aspireResourceTypeSymbol = @interface;
									break;
								}
							}
						}
					}
				}
			}

			TargetSymbolDescriptor result = new(
				symbol,
				declaration,
				isHostKit,
				name,
				propertyName,
				extensionMethodName,
				generateOptions,
				isGenericResourceDefinition,
				aspireResourceTypeSymbol
			);

			return GeneratorResult<TargetSymbolDescriptor>.Ok(result);
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

	static IncrementalValueProvider<GenerationContext> GetGeneratorValueProvider(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var generationContextValueProvider = context
			.CompilationProvider.Select(
				(compilation, cancellationToken) => GenerationContext.Create(compilation, logger, cancellationToken)
			)
			.WithTrackingName("GetGenerationContext");

		return generationContextValueProvider;
	}

	static IncrementalValueProvider<bool> IsSourceGeneratorDisabledValueProvider(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabledValueProvider = context
			.AnalyzerConfigOptionsProvider.Select(
				(opts, _) =>
				{
					logger?.Debug(
						$"Checking MSBuild property {DisablePurviewAspireResourceKitSourceGeneratorPropertyName}"
					);

					opts.GlobalOptions.TryGetValue(
						DisablePurviewAspireResourceKitSourceGeneratorPropertyName,
						out var val
					);
					if (bool.TryParse(val, out var isDisabled))
					{
						if (isDisabled)
						{
							logger?.Info(
								"Purview Aspire Resource Isolation source generators are disabled via MSBuild property"
							);
						}
					}

					return isDisabled;
				}
			)
			.WithTrackingName("IsSourceGeneratorDisabled");

		return isDisabledValueProvider;
	}

	public static string AddCodeGen(string source)
	{
		return source
			.Replace(CodeGenHelpers.CodeGenReplacementToken, CodeGenHelpers.GetGeneratedCodeAttribute())
			.Replace(
				CodeGenHelpers.NonClassCodeGenReplacementToken,
				CodeGenHelpers.GetNonClassGeneratedCodeAttribute()
			);
	}
}
