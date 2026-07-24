using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class TypeHelpers
{
	// Generated type information...
	public const string ResourceKitNamespace = "Purview.Aspire.ResourceKit";

	public const string BaseClassSuffix = "ResourceBase";

	public static readonly TypeValueObject HostAppAttribute = new(nameof(HostAppAttribute), ResourceKitNamespace);

	public static readonly TypeValueObject ResourceDefinitionAttribute = new(
		nameof(ResourceDefinitionAttribute),
		ResourceKitNamespace
	);

	// Library types
	public static readonly TypeValueObject HostAppBase = new(nameof(HostAppBase), ResourceKitNamespace);

	public static readonly TypeValueObject ResourceKitBase = new(nameof(ResourceKitBase), ResourceKitNamespace);

	// Other required types
	// Required for DI
	public static readonly TypeValueObject ServiceLifetime = new(
		nameof(ServiceLifetime),
		"Microsoft.Extensions.DependencyInjection"
	);

	public static readonly TypeValueObject ConfigurationBinder = new(
		nameof(ConfigurationBinder),
		"Microsoft.Extensions.Configuration"
	);

	// Aspire types.
	public static readonly TypeValueObject IResource = new(nameof(IResource), "Aspire.Hosting.ApplicationModel");
	public static readonly TypeValueObject IDistributedApplicationBuilder = new(
		nameof(IDistributedApplicationBuilder),
		"Aspire.Hosting"
	);

	// Other useful types
	public static readonly TypeValueObject NotNullAttribute = new(
		nameof(NotNullAttribute),
		"System.Diagnostics.CodeAnalysis"
	);
	public static readonly TypeValueObject EmbeddedAttribute = new(nameof(EmbeddedAttribute), "Microsoft.CodeAnalysis");

	// Generated attributes (make sure this is after they're all initialised!)
	public static readonly TypeValueObject[] GeneratedTypes = [HostAppAttribute, ResourceDefinitionAttribute];
}
