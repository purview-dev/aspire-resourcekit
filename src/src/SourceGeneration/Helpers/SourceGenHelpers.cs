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
		var appResourceValueProvider = GetGenerationValueProviders(context, TypeHelpers.AppResourceAttribute, logger);

		return isDisabledValueProvider
			.Combine(generationContextValueProvider) // (bool, GenerationContext)
			.Combine(hostAppValueProvider.Collect()) // ((bool, GenerationContext), ImmutableArray<T>)
			.Combine(appResourceValueProvider.Collect()) // (((bool, GenerationContext), ImmutableArray<T>), ImmutableArray<T>)
			.Select(
				static (nested, _) =>
				{
					var (((isDisabled, generationContext), hostApps), appResources) = nested;

					generationContext.Logger?.Debug("Combined all value providers:");
					generationContext.Logger?.Debug($"Disabled: {isDisabled}", 1);
					generationContext.Logger?.Debug($"Host Apps: {hostApps.Length}", 1);
					generationContext.Logger?.Debug($"App Resources: {appResources.Length}", 1);
					generationContext.Logger?.Debug($"Generation Context:", 1);
					foreach (var info in generationContext.GetDebugInfo())
						generationContext.Logger?.Debug(info, 2);

					// Work out any diagnostics we might need to raise here.
					List<DiagnosticInfo> diagnostics = [];
					if (hostApps.Length > 1)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.MultipleHostAppsFoundnfo));
					if (generationContext.ServiceLifetime is null)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.ServiceLifetimeMissing));

					if (generationContext.ConfigurationBinder is null)
					{
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.OptionDependencyMissing));
					}

					return new GenerationModel(
						IsSourceGeneratorEnabled: !isDisabled,
						GenerationContext: generationContext,
						HostApp: hostApps.FirstOrDefault(),
						AppResources: appResources,
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

		// Create a syntax provider that finds classes with the specified attribute.
		var targetSymbols = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				attributeType.SymbolFullName,
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: (ctx, ct) => GetSemanticTargetForGeneration(ctx, attributeType, logger, ct)
			)
			.WithTrackingName($"Get{attributeType.TypeName}Targets");

		return targetSymbols;

		// We only want to consider class declarations for generation, so we filter the syntax nodes accordingly.
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

			// Parse attribute named arguments.
			string? name = null;
			string? propertyName = null;
			string? serviceLifetime = null;

			if (context.Attributes.Length > 0)
			{
				foreach (var namedArg in context.Attributes[0].NamedArguments)
				{
					switch (namedArg.Key)
					{
						case "Name":
							name = namedArg.Value.Value?.ToString();
							logger?.Debug($"found Name: '{name ?? "<null>"}'", 1);

							break;
						case "PropertyName":
							propertyName = namedArg.Value.Value?.ToString();

							logger?.Debug($"found PropertyName: '{propertyName ?? "<null>"}'", 1);
							break;
						case "ServiceLifetime":
							serviceLifetime = namedArg.Value.Value is int li
								? li switch
								{
									1 => "Scoped",
									2 => "Transient",
									_ => "Singleton",
								}
								: "Singleton";

							logger?.Debug($"found ServiceLifetime: '{serviceLifetime ?? "<null>"}'", 1);

							break;
					}
				}
			}

			TargetSymbolDescriptor result = new(
				symbol,
				declaration,
				isHostApp,
				name,
				propertyName,
				serviceLifetime ?? "Singleton"
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
		// Collect the generation context, which includes references to required attributes and the logger
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
		// Opt-out: set <DisableAspireResourceKitSourceGenerator>true</DisableAspireResourceKitSourceGenerator> to skip generation.
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
