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
		var hostAppValueProvider = GetGenerationValueProviders(context, TypeHelpers.HostAppAttribute, logger);
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
		var appResourceAliasValueProvider = GetGenerationValueProviders(
			context,
			new TypeValueObject("AppResourceAttribute", TypeHelpers.ResourceKitNamespace),
			logger
		);

		return isDisabledValueProvider
			.Combine(generationContextValueProvider)
			.Combine(hostAppValueProvider.Collect())
			.Combine(
				appResourceValueProvider
					.Collect()
					.Combine(genericAppResourceValueProvider.Collect().Combine(appResourceAliasValueProvider.Collect()))
			)
			.Select(
				static (nested, _) =>
				{
					var (((isDisabled, generationContext), hostApps), (appResources, (genericAppResources, appResourceAliases))) = nested;
					var allAppResources = appResources.AddRange(genericAppResources).AddRange(appResourceAliases);

					generationContext.Logger?.Debug("Combined all value providers:");
					generationContext.Logger?.Debug($"Disabled: {isDisabled}", 1);
					generationContext.Logger?.Debug($"Host Apps: {hostApps.Length}", 1);
					generationContext.Logger?.Debug($"App Resources: {allAppResources.Length}", 1);
					generationContext.Logger?.Debug("Generation Context:", 1);
					foreach (var info in generationContext.GetDebugInfo())
						generationContext.Logger?.Debug(info, 2);

					List<DiagnosticInfo> diagnostics = [];
					if (hostApps.Length > 1)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.MultipleHostAppsFoundnfo));
					if (generationContext.IServiceCollection is null)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.ServiceCollectionMissing));

					if (generationContext.ConfigurationBinder is null)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.OptionDependencyMissing));

					return new GenerationModel(
						IsSourceGeneratorEnabled: !isDisabled,
						GenerationContext: generationContext,
						HostApp: hostApps.FirstOrDefault(),
						AppResources: allAppResources,
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

			var isHostApp = attributeType == TypeHelpers.HostAppAttribute;

			logger?.Debug($"Processing Attribute: {attributeType.SymbolFullName}");
			if (isHostApp)
				logger?.Debug($"for Host App {symbol.Name}", 1);
			else
				logger?.Debug($"for App Resource {symbol.Name}", 1);

			string? name;
			string? propertyName;
			bool generateOptions;
			bool isGenericResourceDefinition;
			string? genericResourceTypeName;

			if (isHostApp)
			{
				var data = HostAppAttributeData.FromAttributeData(generationContext, context.Attributes);
				name = data.Name;
				propertyName = null;
				generateOptions = data.GenerateOptions;
				isGenericResourceDefinition = false;
				genericResourceTypeName = null;

				logger?.Debug($"found Name: '{name ?? "<null>"}'", 1);
				logger?.Debug($"found GenerateOptions: '{generateOptions}'", 1);
			}
			else
			{
				var data = ResourceDefinitionAttributeData.FromAttributeData(generationContext, context.Attributes);
				name = data.Name;
				propertyName = data.PropertyName;
				generateOptions = data.GenerateOptions;
				isGenericResourceDefinition = data.IsGeneric;
				genericResourceTypeName = data.GenericResourceTypeName;

				logger?.Debug($"found Name: '{name ?? "<null>"}'", 1);
				logger?.Debug($"found PropertyName: '{propertyName ?? "<null>"}'", 1);
				logger?.Debug($"found GenerateOptions: '{generateOptions}'", 1);
				logger?.Debug($"found GenericAttribute: '{isGenericResourceDefinition}'", 1);
				logger?.Debug($"found GenericResourceTypeName: '{genericResourceTypeName ?? "<null>"}'", 1);
			}

			TargetSymbolDescriptor result = new(
				symbol,
				declaration,
				isHostApp,
				name,
				propertyName,
				generateOptions,
				isGenericResourceDefinition,
				genericResourceTypeName
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
}
