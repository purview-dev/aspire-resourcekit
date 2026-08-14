using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Infrastructure;

public abstract class SourceGeneratorTestBase<TGenerator> : TUnitSourceGeneratorTestBase<TGenerator>
	where TGenerator : class, IIncrementalGenerator, new()
{
	// +1 is for the EmbeddedAttribute
	public static readonly int ExpectedGeneratedFileCount = TypeLibrary.GeneratedTypes.Length + 1;

	public static readonly int ExpectedFileCountPlusGen = ExpectedGeneratedFileCount + 1;

	protected override async Task OnAfterRunAsync(
		DriverRunResult result,
		IEnumerable<string> sources,
		SourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	)
	{
		var context = (GenerationDriverContext)options.State!;

		if (context.EnsureValid)
			result.EnsureValid();

		if (context.ValidateNoErrorDiagnostics)
			await Assert.That(result).HasNoErrorDiagnostics();
	}

	protected Task<DriverRunResult> ResourceKitGenerateAsync(
		IEnumerable<string> sources,
		CancellationToken cancellationToken
	) => ResourceKitGenerateAsync(sources, GenerationDriverContext.Default, cancellationToken);

	protected Task<DriverRunResult> ResourceKitGenerateAsync(
		string source,
		CancellationToken cancellationToken
	) => ResourceKitGenerateAsync([source], GenerationDriverContext.Default, cancellationToken);

	protected async Task<DriverRunResult> ResourceKitGenerateAsync(
		IEnumerable<string> sources,
		GenerationDriverContext context,
		CancellationToken cancellationToken
	)
	{
		var options = BuildOptions(context);
		sources = BuildSources(sources);
		return await GenerateAsync(sources, options, cancellationToken);
	}

	protected Task<DriverRunResult> ResourceKitGenerateAsync(
		string source,
		GenerationDriverContext context,
		CancellationToken cancellationToken
	) => ResourceKitGenerateAsync([source], context, cancellationToken);

	IEnumerable<string> BuildSources(IEnumerable<string> sources)
	{
		var writer = CodeWriter.CreateTestWriter();
		writer
			.WriteFileScopedNamespace(TestHelper.DefaultAspireResource.Namespace)
			.WriteClass(
				new(TestHelper.DefaultAspireResource.TypeName)
				{
					Interfaces = [TypeLibrary.IResource],
					Accessibility = TypeDeclarationAccessibility.Public,
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

		return [.. sources, testResource];
	}

	static SourceGeneratorTestOptions BuildOptions(GenerationDriverContext context)
	{
		List<string> namespaces = [];
		if (context.IncludeSystemNamespaces)
		{
			namespaces.AddRange([
				typeof(string).Namespace!,
				typeof(IEnumerable<>).Namespace!,
				typeof(Enumerable).Namespace!,
			]);
		}

		if (context.IncludeSourceGeneratorNamespaces)
			namespaces.Add(TypeLibrary.PurviewAspireResourceKitNamespace);

		namespaces.AddRange([
			typeof(global::Aspire.Hosting.ApplicationModel.IResource).Namespace!,
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder).Namespace!,
		]);

		if (context.IncludeOptionsReference)
			namespaces.Add(TypeLibrary.OptionsBuilder.Namespace!);

		if (context.IncludeOptionsConfigurationExtensionReference)
			namespaces.Add(TypeLibrary.ConfigurationBinder.Namespace!);

		List<Type> additionalTypes =
		[
			typeof(global::Aspire.Hosting.ApplicationModel.IResource),
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder),
			typeof(IResourceKit<>),
		];

		if (context.IncludeIServiceCollectionReference)
			additionalTypes.Add(
				typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)
			);

		if (context.IncludeOptionsReference)
			additionalTypes.Add(typeof(Microsoft.Extensions.Options.IOptions<>));

		if (context.IncludeOptionsConfigurationExtensionReference)
		{
			additionalTypes.Add(
				typeof(Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions)
			);
			additionalTypes.Add(typeof(Microsoft.Extensions.Configuration.ConfigurationBinder));
		}

		var opts = new SourceGeneratorTestOptions
		{
			AdditionalNamespaces = [.. namespaces],
			AdditionalAssemblyTypes = [.. additionalTypes],
			ThrowOnGenerationException = context.ThrowOnGenerationException,
			ExcludeGeneratedAttributes =
			[
				TypeLibrary.EmbeddedAttribute.TypeName,
				TypeLibrary.HostKitAttribute.TypeName,
				TypeLibrary.ResourceDefinitionAttribute.TypeName,
			],
			CompileToAssembly = context.CompileToAssembly,
			PreprocessReferences = AppendAspireResource,
			State = context,
		};

		if (context.DisableSourceGenerator is bool disable)
		{
			opts = opts with
			{
				DisableSourceGeneratorPropertyName =
					PropertyLibrary.DisablePurviewAspireResourceKitSourceGeneratorPropertyName,
				DisableSourceGeneratorValue = disable,
			};
		}

		return opts;
	}

	static void AppendAspireResource(ImmutableArray<MetadataReference> references)
	{
		references.Add(
			MetadataReference.CreateFromFile(
				typeof(global::Aspire.Hosting.ApplicationModel.IResource).Assembly.Location
			)
		);
		references.Add(
			MetadataReference.CreateFromFile(
				typeof(global::Aspire.Hosting.IDistributedApplicationBuilder).Assembly.Location
			)
		);
		references.Add(MetadataReference.CreateFromFile(typeof(IResourceKit<>).Assembly.Location));
	}
}
