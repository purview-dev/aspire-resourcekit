using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Infrastructure;

public sealed record ResourceKitSourceGeneratorTestOptions : SourceGeneratorTestOptions
{
	public bool IncludeSourceGeneratorNamespaces { get; init; } = true;

	public bool IncludeIServiceCollectionReference { get; init; } = true;

	public bool IncludeOptionsReference { get; init; } = true;

	public bool IncludeOptionsConfigurationExtensionReference { get; init; } = true;

	public static ResourceKitSourceGeneratorTestOptions NoValidation { get; } = new()
	{
		ThrowOnGenerationException = false
	};
}

public abstract class ResourceKitSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, ResourceKitSourceGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new()
{
	// +1 is for the EmbeddedAttribute
	public static readonly int ExpectedGeneratedFileCount = TypeLibrary.GeneratedTypes.Length + 1;

	public static readonly int ExpectedFileCountPlusGen = ExpectedGeneratedFileCount + 1;

	protected override ResourceKitSourceGeneratorTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		ResourceKitSourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	) => base.OnBeforeRun(sources, BuildOptions(options), cancellationToken);

	static ImmutableArray<string> BuildSources()
	{
		var writer = CodeWriter.CreateTestWriter();
		writer
			.WriteFileScopedNamespace(TestHelper.DefaultAspireResource.Namespace)
			.WriteClass(
				new(TestHelper.DefaultAspireResource.Name, TypeDeclarationAccessibility.Public)
				{
					Interfaces = [TypeLibrary.IResource],
				},
				bodyWriter =>
					bodyWriter
						.WriteLine("public string Name { get; } = \"DefaultAspireResource\";")
						.NewLine()
						.WriteLine(
							$"public global::{TypeLibrary.IResource.Namespace}.ResourceAnnotationCollection Annotations {{ get; }} = [];"
						)
			);

		var testResource = writer.ToString();

		return [testResource];
	}

	static ResourceKitSourceGeneratorTestOptions BuildOptions(ResourceKitSourceGeneratorTestOptions options)
	{
		List<string> additionalNamespaces = [];

		if (options.IncludeSourceGeneratorNamespaces)
			additionalNamespaces.Add(TypeLibrary.PurviewAspireResourceKitNamespace);

		additionalNamespaces.AddRange([
			typeof(global::Aspire.Hosting.ApplicationModel.IResource).Namespace!,
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder).Namespace!,
		]);

		if (options.IncludeOptionsReference)
			additionalNamespaces.Add(TypeLibrary.OptionsBuilder.Namespace!);

		if (options.IncludeOptionsConfigurationExtensionReference)
			additionalNamespaces.Add(TypeLibrary.ConfigurationBinder.Namespace!);

		List<Type> additionalTypes =
		[
			typeof(global::Aspire.Hosting.ApplicationModel.IResource),
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder),
			typeof(IResourceKit<>),
		];

		if (options.IncludeIServiceCollectionReference)
			additionalTypes.Add(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection));

		if (options.IncludeOptionsReference)
			additionalTypes.Add(typeof(Microsoft.Extensions.Options.IOptions<>));

		if (options.IncludeOptionsConfigurationExtensionReference)
		{
			additionalTypes.Add(typeof(Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions));
			additionalTypes.Add(typeof(Microsoft.Extensions.Configuration.ConfigurationBinder));
		}

		var opts = options with
		{
			AdditionalNamespaces = [.. additionalNamespaces],
			AdditionalAssemblyTypes = [.. additionalTypes],
			AdditionalReferences = AspireReferences(),
			ExcludeGeneratedSourceHintNames =
			[
				"EmbeddedAttribute",
				TypeLibrary.HostKitAttribute.Name,
				TypeLibrary.ResourceDefinitionAttribute.Name,
			],
			AdditionalSources = BuildSources(),
		};

		return opts;
	}

	static ImmutableArray<MetadataReference> AspireReferences() =>
		[
			MetadataReference.CreateFromFile(
				typeof(global::Aspire.Hosting.ApplicationModel.IResource).Assembly.Location
			),
			MetadataReference.CreateFromFile(
				typeof(global::Aspire.Hosting.IDistributedApplicationBuilder).Assembly.Location
			),
			MetadataReference.CreateFromFile(typeof(IResourceKit<>).Assembly.Location),
		];
}
