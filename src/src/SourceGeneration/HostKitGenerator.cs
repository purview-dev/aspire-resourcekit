using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

[Generator(LanguageNames.CSharp)]
public sealed partial class HostKitGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(postInitContext =>
		{
			_logger?.Debug("Adding attributes:");
			_logger?.Debug($"- {TypeLibrary.EmbeddedAttribute.TypeName}", 1);

			postInitContext.AddSource(
				TypeLibrary.EmbeddedAttribute.TypeName + ".cs",
				SourceText.From(TypeLibrary.EmbeddedAttributeSource, Encoding.UTF8)
			);

			foreach (var resourceType in TypeLibrary.GeneratedTypes)
			{
				_logger?.Debug($"- {resourceType.TypeName}", 1);
				postInitContext.AddSource(
					resourceType.SymbolFullName + ".g.cs",
					EmbeddedResources.Load(resourceType.TypeName)
				);
			}
		});

		// Collect all of the host app types and host resource types.
		var valueProviders = SourceGenLibrary.GetGeneratorValueProviders(context, _logger);

		context.RegisterSourceOutput(
			valueProviders,
			(sourceProductionContext, model) =>
			{
				if (!model.IsSourceGeneratorEnabled)
				{
					_logger?.Debug("Source generator disabled.");
					return;
				}

				_logger?.Debug("Source generator enabled, processing...");

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
				var hostKitSymbol = model.HostKit.IsSuccess
					? model.HostKit.Value!.Target.Symbol
					: null;

				var resourceKitDescriptors = GatherResourceKits(
					model,
					diagnostics,
					hostKitSymbol,
					_logger
				);

				var validResourceDescriptors = ReportDiagnostics(
					sourceProductionContext,
					model,
					diagnostics,
					hostKitSymbol,
					resourceKitDescriptors,
					_logger
				);

				// If any fatal diagnostics were reported, do not generate any source code.
				if (model.HostKit.IsFatal || model.ResourceKits.Any(s => s.IsFatal))
					return;

				// We only support a single host app - if none was found, there is nothing to generate.
				if (hostKitSymbol is null)
					return;

				var hostKitInfo = CodeGenHelpers.BuildHostKit(
					model.HostKit.Value!,
					[.. validResourceDescriptors]
				);
				var source = BuildSource(
					hostKitInfo,
					model.GenerationContext,
					_logger,
					sourceProductionContext.CancellationToken
				);

				var fileName = $"{hostKitSymbol.Name}.AspireResourceKit.g.cs";

				_logger?.Debug($"Adding source file: {fileName}");

				sourceProductionContext.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
			}
		);
	}

	[SuppressMessage(
		"Style",
		"CA1502:Avoid excessive complexity",
		Justification = "This method is complex due to the number of validation checks."
	)]
	static List<KitTargetDescriptor> ReportDiagnostics(
		SourceProductionContext sourceProductionContext,
		KitGenerationModel model,
		List<DiagnosticInfo> diagnostics,
		INamedTypeSymbol? hostKitSymbol,
		List<KitTargetDescriptor> resourceKitDescriptors,
		GenerationLogger? logger
	)
	{
		List<KitTargetDescriptor> validResourceDescriptors = [];

		// Validate app resources: derive names, check uniqueness and base type.
		if (hostKitSymbol is not null && resourceKitDescriptors.Count > 0)
		{
			//var descriptor = model.HostKit.Value!;
			HashSet<string> seenPropertyNames = [with(StringComparer.Ordinal)];

			foreach (var resourceKitDescriptor in resourceKitDescriptors)
			{
				var resourceKitSymbol = resourceKitDescriptor.Target.Symbol;
				var hasExplicitBaseType = TypeHelpers.HasExplicitBaseType(
					resourceKitDescriptor.Target
				);

				if (resourceKitDescriptor.IsGenericResourceDefinition && hasExplicitBaseType)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.GenericResourceDefinitionCannotHaveExplicitBase,
							resourceKitDescriptor.Target.Declaration?.Identifier.GetLocation(),
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
							resourceKitDescriptor.Target.Declaration?.Identifier.GetLocation(),
							resourceKitSymbol.Name,
							TypeLibrary.ResourceKitBase.SymbolFullName
						)
					);
					continue;
				}

				// Derive the resource name (from attribute or type name).
				var resourceName =
					resourceKitDescriptor.Name ?? CodeGenHelpers.TrimSuffix(resourceKitSymbol.Name);
				if (string.IsNullOrWhiteSpace(resourceName))
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.ResourceNameNotDerivable,
							resourceKitDescriptor.Target.Declaration?.Identifier.GetLocation(),
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
							resourceKitDescriptor.Target.Declaration?.Identifier.GetLocation(),
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
							resourceKitDescriptor.Target.Declaration?.Identifier.GetLocation(),
							propertyName
						)
					);
					continue;
				}

				// Check that resources with an explicit base derive from the expected generated base (SG0006).
				// If no explicit base was declared, a generated partial will provide the host-specific base.
				if (
					hasExplicitBaseType
					&& !TypeHelpers.IsDerivedFromExpectedBase(
						resourceKitDescriptor.Target,
						TypeLibrary.ResourceKitBase.MakeGeneric(TypeLibrary.IResource)
					)
				)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.ResourceMustDeriveFromBase,
							resourceKitDescriptor.Target.Declaration?.Identifier.GetLocation(),
							resourceKitSymbol.Name,
							TypeLibrary.ResourceKitBase.SymbolFullName
						)
					);
					continue;
				}

				if (resourceKitDescriptor.AspireResourceTypeSymbol is null)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.NoAspireResourceFound,
							resourceKitDescriptor.Target.Declaration?.Identifier.GetLocation(),
							resourceKitSymbol.Name
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
					GeneratorDiagnostics.Create(
						GeneratorDiagnostics.NoResourceKitsDefined,
						hostKitSymbol
					)
				);
			}
		}
		else if (model.HostKit.IsEmpty)
			diagnostics.Add(GeneratorDiagnostics.Create(GeneratorDiagnostics.NoHostKitInfoDefined));

		// Report any diagnostics that were collected during the generation process.
		if (diagnostics.Count > 0)
		{
			ReportDiagnostics(sourceProductionContext, diagnostics, logger);
		}

		return validResourceDescriptors;
	}

	static List<KitTargetDescriptor> GatherResourceKits(
		KitGenerationModel model,
		List<DiagnosticInfo> diagnostics,
		INamedTypeSymbol? hostKitSymbol,
		GenerationLogger? logger
	)
	{
		// Resolve the app resource descriptors. All [ResourceKit] classes
		// attach to the single [HostApp]; the generated base class is not
		// visible during source generation, so interface-based filtering
		// is not possible.
		List<KitTargetDescriptor> resourceKitDescriptors;
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
			logger?.Debug("No host app found");
			resourceKitDescriptors = [];
		}

		var mixedUsageResourceSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		foreach (
			var group in resourceKitDescriptors.GroupBy(
				r => r.Target.Symbol,
				SymbolEqualityComparer.Default
			)
		)
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
					group.First().Target.Declaration?.Identifier.GetLocation(),
					group.Key.Name
				)
			);
		}

		resourceKitDescriptors =
		[
			.. resourceKitDescriptors.Where(resource =>
				!mixedUsageResourceSymbols.Contains(resource.Target.Symbol)
			),
		];
		return resourceKitDescriptors;
	}
}
