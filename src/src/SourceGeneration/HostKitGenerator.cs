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
			_logger?.Debug("Adding attributes:");
			foreach (var resourceType in TypeLibrary.GeneratedTypes)
			{
				_logger?.Debug($"- {resourceType.TypeName}", 1);
				postInitContext.AddSource(
					resourceType.MetadataFullName + ".g.cs",
					EmbeddedResources.Load(resourceType.TypeName)
				);
			}
		});

		var valueProvider = SourceGenLibrary.GetGeneratorValueProviders(context, _logger);

		context.RegisterSourceOutput(
			valueProvider,
			(sourceProductionContext, model) =>
			{
				if (!model.IsSourceGeneratorEnabled)
					return;

				var diagnostics = new List<DiagnosticInfo>();
				diagnostics.AddRange(model.Diagnostics);
				if (model.HostKit.HasDiagnostics)
					diagnostics.AddRange(model.HostKit.Diagnostics);
				foreach (var resourceResult in model.ResourceKits)
				{
					if (resourceResult.HasDiagnostics)
						diagnostics.AddRange(resourceResult.Diagnostics);
				}

				var resourceKitDescriptors = GatherResourceKits(model, diagnostics);
				var hostKit = model.HostKit.IsSuccess ? model.HostKit.Value : null;
				var validResourceDescriptors = ReportDiagnostics(
					sourceProductionContext,
					model,
					diagnostics,
					hostKit,
					resourceKitDescriptors
				);

				if (model.HostKit.IsFatal || model.ResourceKits.Any(s => s.IsFatal) || hostKit is null)
					return;

				var hostKitInfo = CodeGenHelpers.BuildHostKit(hostKit, validResourceDescriptors);
				var writer = BuildSource(hostKitInfo, sourceProductionContext.CancellationToken);
				var hintName = HintNameHelper.ForHost(hostKitInfo.HostKitType.MetadataFullName);
				sourceProductionContext.AddSource(hintName, writer);
			}
		);
	}

	static List<KitTargetDescriptor> ReportDiagnostics(
		SourceProductionContext sourceProductionContext,
		KitGenerationModel model,
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
							resource.TypeName
						)
					);
					continue;
				}

				if (!resource.IsGenericResourceDefinition && !resource.HasExplicitBaseType)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							GeneratorDiagnostics.NonGenericResourceDefinitionRequiresExplicitBase,
							resource.TypeName,
							TypeLibrary.ResourceKitBase.MetadataFullName
						)
					);
					continue;
				}

				var resourceName = resource.Name ?? CodeGenHelpers.TrimSuffix(resource.TypeName);
				if (string.IsNullOrWhiteSpace(resourceName))
				{
					diagnostics.Add(
						DiagnosticInfo.Create(GeneratorDiagnostics.ResourceNameNotDerivable, resource.TypeName)
					);
					continue;
				}

				var propertyName = resource.PropertyName ?? CodeGenHelpers.TrimSuffix(resource.TypeName);
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
							resource.TypeName,
							TypeLibrary.ResourceKitBase.MetadataFullName
						)
					);
					continue;
				}

				if (resource.AspireResourceType is null)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(GeneratorDiagnostics.NoAspireResourceFound, resource.TypeName)
					);
					continue;
				}

				validResourceDescriptors.Add(resource);
			}
		}

		if (validResourceDescriptors.Count == 0 && !model.HostKit.IsEmpty)
			diagnostics.Add(
				DiagnosticInfo.Create(GeneratorDiagnostics.NoResourceKitsDefined, hostKit?.TypeName ?? string.Empty)
			);
		else if (model.HostKit.IsEmpty)
			diagnostics.Add(DiagnosticInfo.Create(GeneratorDiagnostics.NoHostKitInfoDefined));

		if (diagnostics.Count > 0)
			ReportDiagnostics(sourceProductionContext, diagnostics, null);

		return validResourceDescriptors;
	}

	static List<KitTargetDescriptor> GatherResourceKits(KitGenerationModel model, List<DiagnosticInfo> diagnostics)
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
