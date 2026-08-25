using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

[Generator(LanguageNames.CSharp)]
public sealed partial class HostKitGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterEmbeddedAttribute(
			$"{AssemblyInfo.AssemblyName}.{nameof(HostKitGenerator)}",
			AssemblyInfo.Version
		);

		context.RegisterPostInitializationOutput(postInitContext =>
		{
			foreach (var resourceType in TypeLibrary.GeneratedTypes)
			{
				postInitContext.AddSource(
					resourceType.MetadataFullName + ".g.cs",
					EmbeddedResourceHelper.Load(resourceType.Name)
				);
			}
		});

		var valueProvider = SourceGenLibrary.GetGeneratorValueProviders(context);

		context.RegisterSourceOutput(
			valueProvider,
			(sourceProductionContext, collectionResults) =>
			{
				if (collectionResults.Context.Settings.IsSourceGeneratorDisabled)
					return;

				if (collectionResults.HostKits.Count > 1)
					sourceProductionContext.ReportDiagnostic(Diagnostic.Create( GeneratorDiagnostics.MultipleHostKitsFoundInfo, null));

				if (collectionResults.HostKit.HasDiagnostics)
					sourceProductionContext.ReportDiagnostics(collectionResults.HostKit.Diagnostics);

				foreach (var resourceResult in collectionResults.ResourceKits.SelectMany(r => r.Value))
				{
					if (resourceResult.HasDiagnostics)
						sourceProductionContext.ReportDiagnostics(resourceResult.Diagnostics);
				}

				if (collectionResults.HostKits.Count > 0 || collectionResults.HostKit.IsFatal || collectionResults.ResourceKits.Any(s => s.Value.Any(r => r.IsFatal)))
					return;

				var generatioNmodel = CodeGenHelpers.BuildGenerationModel(collectionResults);
				var writer = BuildSource(generatioNmodel, sourceProductionContext.CancellationToken);
				var hintName = HintNameHelper.ForHost(generatioNmodel.HostKitType.MetadataFullName);
				sourceProductionContext.AddSource(hintName, writer);
			}
		);
	}

	static List<KitTargetDescriptor> ReportDiagnostics(
		SourceProductionContext sourceProductionContext,
		KitGenerationCollectionResults model,
		List<DiagnosticInfo> diagnostics,
		KitTargetDescriptor? hostKit,
		List<KitTargetDescriptor> resourceKitDescriptors
	)
	{
		List<KitTargetDescriptor> validResourceDescriptors = [];
		if (hostKit is not null)
		{
			HashSet<string> seenPropertyNames = [with(StringComparer.Ordinal)];
			foreach (var resource in resourceKitDescriptors)
			{
				if (resource.IsGenericResourceDefinition && resource.HasExplicitBaseType)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.GenericResourceDefinitionCannotHaveExplicitBase,
							resource.Target.Identity.Name
						)
					);
					continue;
				}

				if (!resource.IsGenericResourceDefinition && !resource.HasExplicitBaseType)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.NonGenericResourceDefinitionRequiresExplicitBase,
							resource.Target.Identity.Name,
							TypeLibrary.ResourceKitBase.MetadataFullName
						)
					);
					continue;
				}

				var resourceName = resource.Name ?? CodeGenHelpers.TrimSuffix(resource.Target.Identity.Name);
				if (string.IsNullOrWhiteSpace(resourceName))
				{
					diagnostics.Add(
						DiagnosticInfo.Create(GeneratorDiagnostics.ResourceNameNotDerivable, resource.Target.Identity.Name)
					);
					continue;
				}

				var propertyName = resource.PropertyName ?? CodeGenHelpers.TrimSuffix(resource.Target.Identity.Name);
				if (!TypeHelpers.IsValidIdentifier(propertyName))
				{
					diagnostics.Add(DiagnosticInfo.Create(GeneratorDiagnostics.InvalidPropertyName, propertyName));
					continue;
				}

				if (!seenPropertyNames.Add(propertyName))
				{
					diagnostics.Add(
						DiagnosticInfo.Create(GeneratorDiagnostics.DuplicateResourcePropertyName, propertyName)
					);
					continue;
				}

				if (resource.HasExplicitBaseType && !resource.IsDerivedFromExpectedBase)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.ResourceMustDeriveFromBase,
							resource.Target.Identity.Name,
							TypeLibrary.ResourceKitBase.MetadataFullName
						)
					);
					continue;
				}

				if (resource.AspireResourceType is null)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(GeneratorDiagnostics.NoAspireResourceFound, resource.Target.Identity.Name)
					);
					continue;
				}

				validResourceDescriptors.Add(resource);
			}
		}

		if (validResourceDescriptors.Count == 0 && !model.HostKit.IsEmpty)
			diagnostics.Add(
				DiagnosticInfo.Create(GeneratorDiagnostics.NoResourceKitsDefined, model.HostKit.Value!.Target.Identity.Name)
			);
		else if (model.HostKit.IsEmpty)
			diagnostics.Add(DiagnosticInfo.Create(GeneratorDiagnostics.NoHostKitInfoDefined));

		if (diagnostics.Count > 0)
			sourceProductionContext.ReportDiagnostics(diagnostics);

		return validResourceDescriptors;
	}

	static List<KitTargetDescriptor> GatherResourceKits(KitGenerationCollectionResults model, List<DiagnosticInfo> diagnostics)
	{
		var resources = model.ResourceKits.Where(result => result.IsSuccess).Select(result => result.Value!).ToList();
		var mixedTypes = new HashSet<string>(StringComparer.Ordinal);

		foreach (var group in resources.GroupBy(resource => resource.MetadataFullName, StringComparer.Ordinal))
		{
			if (
				group.Any(resource => resource.IsGenericResourceDefinition)
				&& group.Any(resource => !resource.IsGenericResourceDefinition)
			)
			{
				mixedTypes.Add(group.Key);
				diagnostics.Add(
					DiagnosticInfo.Create(
						GeneratorDiagnostics.MixedResourceDefinitionAttributesNotSupported,
						group.First().TypeName
					)
				);
			}
		}

		return [.. resources.Where(resource => !mixedTypes.Contains(resource.MetadataFullName))];
	}
}
