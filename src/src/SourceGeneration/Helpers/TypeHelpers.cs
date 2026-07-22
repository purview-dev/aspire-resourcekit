using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class TypeHelpers
{
	// Generated type information...
	public const string ResourceKitNamespace = "Purview.Aspire.ResourceKit";

	public static readonly TypeValueObject HostAppAttribute = new(nameof(HostAppAttribute), ResourceKitNamespace);

	public static readonly TypeValueObject AppResourceAttribute = new(
		nameof(AppResourceAttribute),
		ResourceKitNamespace
	);

	// Library types
	public static readonly TypeValueObject HostAppResource = new(nameof(HostAppResource), ResourceKitNamespace);
	public static readonly TypeValueObject IHostAppResource = new(nameof(IHostAppResource), ResourceKitNamespace);

	// Other required types
	// Required for DI
	public static readonly TypeValueObject ServiceLifetime = new(
		nameof(ServiceLifetime),
		"Microsoft.Extensions.DependencyInjection"
	);

	public static readonly TypeValueObject IOptions = new(nameof(IOptions), "Microsoft.Extensions.Options");
	public static readonly TypeValueObject Options = new(nameof(Options), "Microsoft.Extensions.Options");

	//  Required for AddOptions
	public static readonly TypeValueObject OptionsServiceCollectionExtensions = new(
		nameof(OptionsServiceCollectionExtensions),
		"Microsoft.Extensions.DependencyInjection"
	);

	// Required for BindConfiguration
	public static readonly TypeValueObject OptionsBuilderConfigurationExtensions = new(
		nameof(OptionsBuilderConfigurationExtensions),
		"Microsoft.Extensions.DependencyInjection"
	);

	// Other useful types
	public static readonly TypeValueObject NotNullAttribute = new(
		nameof(NotNullAttribute),
		"System.Diagnostics.CodeAnalysis"
	);
	public static readonly TypeValueObject EmbeddedAttribute = new(nameof(EmbeddedAttribute), "Microsoft.CodeAnalysis");

	// Generated attributes (make sure this is after they're all initialised!)
	public static readonly TypeValueObject[] GeneratedTypes = [HostAppAttribute, AppResourceAttribute];
}
