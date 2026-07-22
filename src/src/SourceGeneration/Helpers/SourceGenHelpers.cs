using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;

static class SourceGenHelpers
{
	public static IncrementalValueProvider<GenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabledValueProvider = IsSourceGeneratorDisabledValueProvider(context, logger);
		var generationContextValueProvider = GetGeneratorValueProvider(context, logger);
		var hostAppValueProvider = GetGenerationValueProviders(context, TypeHelpers.FullHostAppAttributeName, logger);
		var appResourceValueProvider = GetGenerationValueProviders(
			context,
			TypeHelpers.FullAppResourceAttributeName,
			logger
		);

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
					generationContext.Logger?.Debug($"Context: {generationContext.GetDebugInfo()}", 1);

					// Work out any diagnostics we might need to raise here.
					var hasMultipleHostApps = hostApps.Length > 1;
					var hasServiceTimeTypeAvailable = generationContext.ServiceLifetime != null;

					List<DiagnosticInfo> diagnostics = [];
					if (hasMultipleHostApps)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.MultipleHostAppsFoundnfo));
					if (!hasServiceTimeTypeAvailable)
						diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.ServiceLifetiemMissing));

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
		string fullAttributeName,
		GenerationLogger? logger
	)
	{
		logger?.Debug($"Generating value providers for {fullAttributeName}");

		// Create a syntax provider that finds classes with the specified attribute.
		var targetSymbols = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				fullAttributeName,
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: (ctx, ct) => GetSemanticTargetForGeneration(ctx, fullAttributeName, logger, ct)
			)
			.WithTrackingName($"Get{fullAttributeName}Targets");

		return targetSymbols;

		// We only want to consider class declarations for generation, so we filter the syntax nodes accordingly.
		static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax;

		static GeneratorResult<TargetSymbolDescriptor> GetSemanticTargetForGeneration(
			GeneratorAttributeSyntaxContext context,
			string fullAttributeName,
			GenerationLogger? logger,
			CancellationToken cancellationToken
		)
		{
			logger?.Debug($"Checking target {context.TargetNode} based on {fullAttributeName}");

			var declaration = (TypeDeclarationSyntax)context.TargetNode;
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

			var isHostApp = fullAttributeName == TypeHelpers.FullHostAppAttributeName;

			logger?.Debug($"Processing Attribute: {fullAttributeName}");
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
		// Opt-out: set <DisableAspireResourceIsolationSourceGenerator>true</DisableAspireResourceIsolationSourceGenerator> to skip generation.
		var isDisabledValueProvider = context
			.AnalyzerConfigOptionsProvider.Select(
				(opts, _) =>
				{
					logger?.Debug(
						$"Checking MSBuild property {TypeHelpers.DisableAspireResourceIsolationSourceGeneratorProperty}"
					);

					opts.GlobalOptions.TryGetValue(
						TypeHelpers.DisableAspireResourceIsolationSourceGeneratorProperty,
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
