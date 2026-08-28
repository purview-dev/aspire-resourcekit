using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

[Generator(LanguageNames.CSharp)]
public sealed partial class HostKitGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context
			.RegisterEmbeddedAttribute<HostKitGenerator>()
			.RegisterPostInitializationOutput(postInitContext =>
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
			(sourceProductionContext, generationModel) =>
			{
				if (generationModel.Context.Settings.IsSourceGeneratorDisabled)
					return;

				// If there are any fatal diagnostics, report them and skip generation
				if (ReportDiagnostics(sourceProductionContext, generationModel))
					return;
				if (!generationModel.HasHostKit)
					return;

				var validResourceKits = generationModel
					.ResourceKits.Select(m => new KeyValuePair<string, ImmutableArray<ResourceKitModel>>(
						m.Key,
						[.. m.Value.Where(m => m.ShouldProcess).Select(m => m.Value)]
					))
					.ToImmutableDictionary();

				var writer = CodeGenEmiiter.Emit(
					new(generationModel, validResourceKits),
					sourceProductionContext.CancellationToken
				);
				var hintName = HintNameHelper.ForHost(generationModel.HostKit.Value.HostKitType.MetadataFullName);
				sourceProductionContext.AddSource(hintName, writer);
			}
		);
	}

	static bool ReportDiagnostics(SourceProductionContext sourceProductionContext, KitGenerationModel outputContext)
	{
		var (IsFatal, Diagnostics) = outputContext.GetAllDiagnostics();
		foreach (var diagnostic in Diagnostics)
			sourceProductionContext.ReportDiagnostic(diagnostic.ToDiagnostic());

		return IsFatal;
	}
}
