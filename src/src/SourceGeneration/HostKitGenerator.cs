using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;
using Purview.Aspire.ResourceKit.SourceGeneration.Templates;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

[Generator(LanguageNames.CSharp)]
public sealed partial class HostKitGenerator : IIncrementalGenerator, ILogSupport
{
	GenerationLogger? _logger;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(postInitContext =>
		{
			_logger?.Debug("Adding attributes:");
			_logger?.Debug($"- {TypeHelpers.EmbeddedAttribute.TypeName}", 1);

			postInitContext.AddEmbeddedAttributeDefinition();

			foreach (var resourceType in TypeHelpers.GeneratedTypes)
			{
				_logger?.Debug($"- {resourceType.TypeName}", 1);
				postInitContext.AddSource(
					resourceType.SymbolFullName + ".g.cs",
					EmbeddedResources.LoadTemplate(resourceType.TypeName)
				);
			}
		});

		// Collect all of the host app types and host resource types.
		var valueProviders = SourceGenHelpers.GetGeneratorValueProviders(context, _logger);

		context.RegisterSourceOutput(
			valueProviders,
			static (sourceProductionContext, model) =>
			{
				if (!model.IsSourceGeneratorEnabled)
				{
					model.GenerationContext.Logger?.Debug("Source generator disabled.");
					return;
				}

				model.GenerationContext.Logger?.Debug("Source generator enabled, processing...");

				List<DiagnosticInfo> diagnostics = [];
				if (model.HostKit.HasDiagnostics)
					diagnostics.AddRange(model.HostKit.Diagnostics);
				if (!model.Diagnostics.IsDefaultOrEmpty)
					diagnostics.AddRange(model.Diagnostics);

				// Collect any diagnostics from the app resource results.
				foreach (var resourceResult in model.ResourceKits)
				{
					if (resourceResult.HasDiagnostics)
						diagnostics.AddRange(resourceResult.Diagnostics);
				}

				// Resolve the host app symbol (if any).
				var hostKitSymbol = model.HostKit.IsSuccess ? model.HostKit.Value!.Symbol : null;

				// Resolve the app resource descriptors. All [AppResource] classes
				// attach to the single [HostApp]; the generated base class is not
				// visible during source generation, so interface-based filtering
				// is not possible.
				List<TargetSymbolDescriptor> resourceKitDescriptors;
				if (hostKitSymbol is not null)
				{
					resourceKitDescriptors = [];
					foreach (var resourceResult in model.ResourceKits)
					{
						if (!resourceResult.IsSuccess)
							continue;

						resourceKitDescriptors.Add(resourceResult.Value!);
					}
				}
				else
				{
					model.GenerationContext.Logger?.Debug("No host app found");
					resourceKitDescriptors = [];
				}

				var mixedUsageResourceSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
				foreach (var group in resourceKitDescriptors.GroupBy(r => r.Symbol, SymbolEqualityComparer.Default))
				{
					if (group.Key is null)
						continue;

					var hasGeneric = group.Any(r => r.IsGenericResourceDefinition);
					var hasNonGeneric = group.Any(r => !r.IsGenericResourceDefinition);
					if (!hasGeneric || !hasNonGeneric)
						continue;

					mixedUsageResourceSymbols.Add(group.Key);
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.MixedResourceDefinitionAttributesNotSupported,
							group.First().Declaration.Identifier.GetLocation(),
							group.Key.Name
						)
					);
				}

				resourceKitDescriptors =
				[
					.. resourceKitDescriptors.Where(resource => !mixedUsageResourceSymbols.Contains(resource.Symbol)),
				];

				List<TargetSymbolDescriptor> validResourceDescriptors = [];

				// Validate app resources: derive names, check uniqueness and base type.
				if (hostKitSymbol is not null && resourceKitDescriptors.Count > 0)
				{
					var descriptor = model.HostKit.Value!;
					var baseClassName =
						$"{descriptor.Name ?? descriptor.Symbol.Name}{TypeHelpers.ResourceKitBaseClassSuffix}";
					HashSet<string> seenPropertyNames = [with(StringComparer.Ordinal)];

					foreach (var resourceKitDescriptor in resourceKitDescriptors)
					{
						var resourceKitSymbol = resourceKitDescriptor.Symbol;
						var hasExplicitBaseType = TypeHelpers.HasExplicitBaseType(resourceKitDescriptor);

						if (resourceKitDescriptor.IsGenericResourceDefinition && hasExplicitBaseType)
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.GenericResourceDefinitionCannotHaveExplicitBase,
									resourceKitDescriptor.Declaration.Identifier.GetLocation(),
									resourceKitSymbol.Name
								)
							);
							continue;
						}

						if (!resourceKitDescriptor.IsGenericResourceDefinition && !hasExplicitBaseType)
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.NonGenericResourceDefinitionRequiresExplicitBase,
									resourceKitDescriptor.Declaration.Identifier.GetLocation(),
									resourceKitSymbol.Name,
									baseClassName
								)
							);
							continue;
						}

						// Derive the resource name (from attribute or type name).
						var resourceName =
							resourceKitDescriptor.Name ?? TypeHelpers.DeriveResourceName(resourceKitSymbol.Name);
						if (string.IsNullOrWhiteSpace(resourceName))
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.ResourceNameNotDerivable,
									resourceKitDescriptor.Declaration.Identifier.GetLocation(),
									resourceKitSymbol.Name
								)
							);
							continue;
						}

						var propertyName = resourceKitDescriptor.PropertyName!;
						if (!TypeHelpers.IsValidIdentifier(propertyName))
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.InvalidPropertyName,
									resourceKitDescriptor.Declaration.Identifier.GetLocation(),
									propertyName
								)
							);
							continue;
						}

						// Check for duplicate property names (SG0005).
						if (!seenPropertyNames.Add(propertyName))
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.DuplicateResourcePropertyName,
									resourceKitDescriptor.Declaration.Identifier.GetLocation(),
									propertyName
								)
							);
							continue;
						}

						// Check that resources with an explicit base derive from the expected generated base (SG0006).
						// If no explicit base was declared, a generated partial will provide the host-specific base.
						if (
							hasExplicitBaseType
							&& !TypeHelpers.IsDerivedFromExpectedBase(resourceKitDescriptor, baseClassName)
						)
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.ResourceMustDeriveFromBase,
									resourceKitDescriptor.Declaration.Identifier.GetLocation(),
									resourceKitSymbol.Name,
									baseClassName
								)
							);
							continue;
						}

						if (resourceKitDescriptor.AspireResourceTypeSymbol is null)
						{
							diagnostics.Add(
								DiagnosticInfo.Create(
									GeneratorDiagnostics.NoAspireResourceFound,
									resourceKitDescriptor.Declaration.Identifier.GetLocation(),
									resourceKitSymbol.Name,
									baseClassName
								)
							);
							continue;
						}

						validResourceDescriptors.Add(resourceKitDescriptor);
					}
				}

				if (validResourceDescriptors.Count == 0)
				{
					if (!model.HostKit.IsEmpty)
					{
						diagnostics.Add(
							GeneratorDiagnostics.Create(GeneratorDiagnostics.NoAppResourcesDefined, hostKitSymbol)
						);
					}
				}
				else if (model.HostKit.IsEmpty)
					diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.NoHostKitInfoDefined));

				// Report any diagnostics that were collected during the generation process.
				if (diagnostics.Count > 0)
					ReportDiagnostics(sourceProductionContext, diagnostics, model.GenerationContext.Logger);

				// If any fatal diagnostics were reported, do not generate any source code.
				if (model.HostKit.IsFatal || model.ResourceKits.Any(s => s.IsFatal))
					return;

				// We only support a single host app - if none was found, there is nothing to generate.
				if (hostKitSymbol is null)
					return;

				var hostKitInfo = CodeGenHelpers.BuildHostKit(model.HostKit.Value!, [.. validResourceDescriptors]);
				var source = BuildSource(
					hostKitInfo,
					model.GenerationContext,
					sourceProductionContext.CancellationToken
				);
				var fileName = $"{hostKitSymbol.Name}.AspireResourceKit.g.cs";

				model.GenerationContext.Logger?.Debug($"Adding source file: {fileName}");

				sourceProductionContext.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
			}
		);
	}

	static string ToCamelCase(string value)
	{
		return string.IsNullOrEmpty(value) ? value
			: value.Length == 1 ? char.ToLowerInvariant(value[0]).ToString()
			: char.ToLowerInvariant(value[0]) + value.Substring(1);
	}
}
