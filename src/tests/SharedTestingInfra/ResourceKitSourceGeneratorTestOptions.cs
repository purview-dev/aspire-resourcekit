namespace Purview.Aspire.ResourceKit;

public sealed record ResourceKitSourceGeneratorTestOptions : SourceGeneratorTestOptions
{
	public ResourceKitSourceGeneratorTestOptions()
	{
		List<string> additionalNamespaces = [];

		additionalNamespaces.AddRange([
			typeof(global::Aspire.Hosting.ApplicationModel.IResource).Namespace!,
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder).Namespace!,
			typeof(DefaultAspireResource).Namespace!,
		]);

		additionalNamespaces.Add(TypeLibrary.Microsoft.Extensions.Options.Namespace);
		additionalNamespaces.Add(TypeLibrary.Microsoft.Extensions.Configuration.Namespace);

		List<Type> additionalTypes =
		[
			typeof(global::Aspire.Hosting.ApplicationModel.IResource),
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder),
		];

		additionalTypes.Add(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection));
		additionalTypes.Add(typeof(Microsoft.Extensions.Options.IOptions<>));
		additionalTypes.Add(typeof(Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions));
		additionalTypes.Add(typeof(Microsoft.Extensions.Configuration.ConfigurationBinder));

		AdditionalNamespaces = [.. additionalNamespaces];
		AdditionalAssemblyTypes = [.. additionalTypes];
		//AdditionalReferences = AspireReferences();
		ExcludeGeneratedSourceHintNames =
		[
			"EmbeddedAttribute",
			TypeLibrary.Purview.Aspire.ResourceKit.HostKitAttribute.Name,
			TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute.Name,
		];
	}

	public static ResourceKitSourceGeneratorTestOptions NoValidation { get; } =
		new() { ThrowOnGenerationException = false };

	public static ResourceKitSourceGeneratorTestOptions Compile { get; } = new() { CompileToAssembly = true };

	public static ResourceKitSourceGeneratorTestOptions NoServiceCollectionReference
	{
		get
		{
			ResourceKitSourceGeneratorTestOptions result = new();
			return result with
			{
				AdditionalAssemblyTypes = result.AdditionalAssemblyTypes.Remove(
					typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)
				),
			};
		}
	}

	public static ResourceKitSourceGeneratorTestOptions NoOptionsReference
	{
		get
		{
			ResourceKitSourceGeneratorTestOptions result = new();
			return result with
			{
				AdditionalAssemblyTypes = result.AdditionalAssemblyTypes.Remove(
					typeof(Microsoft.Extensions.Options.IOptions<>)
				),
			};
		}
	}

	public static ResourceKitSourceGeneratorTestOptions NoOptionsConfigurationExtensionReference
	{
		get
		{
			ResourceKitSourceGeneratorTestOptions result = new();
			return result with
			{
				AdditionalAssemblyTypes = result
					.AdditionalAssemblyTypes.Remove(
						typeof(Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions)
					)
					.Remove(typeof(Microsoft.Extensions.Configuration.ConfigurationBinder)),
			};
		}
	}
}
