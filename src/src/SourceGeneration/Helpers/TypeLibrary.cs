namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class TypeLibrary
{
	// Generated type information...
	public static readonly string[] TrimSuffixes = ["ResourceKit", "Resource", "Kit"];

	public const string OptionsBaseClassSuffix = "Options";

	public const string PurviewAspireResourceKitNamespace = "Purview.Aspire.ResourceKit";

	public static readonly TypeIdentity HostKitAttribute = new(
		nameof(HostKitAttribute),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeIdentity ResourceDefinitionAttribute = new(
		nameof(ResourceDefinitionAttribute),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeIdentity GenericResourceDefinitionAttribute = new(
		"ResourceDefinitionAttribute`1",
		PurviewAspireResourceKitNamespace
	);

	// Library types
	public static readonly TypeIdentity IHostKit = new(nameof(IHostKit), PurviewAspireResourceKitNamespace);

	public static readonly TypeIdentity HostKitBase = new(nameof(HostKitBase), PurviewAspireResourceKitNamespace);

	public static readonly TypeIdentity ResourceKitBase = new(
		nameof(ResourceKitBase),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeIdentity IResourceKit = new(nameof(IResourceKit), PurviewAspireResourceKitNamespace);

	// Other required types
	// Required for DI
	public static readonly TypeIdentity IServiceCollection = new(
		nameof(IServiceCollection),
		"Microsoft.Extensions.DependencyInjection"
	);

	public static readonly TypeIdentity ConfigurationBinder = new(
		nameof(ConfigurationBinder),
		"Microsoft.Extensions.Configuration"
	);

	// Required for Options
	public static readonly TypeIdentity OptionsBuilder = new(nameof(OptionsBuilder), "Microsoft.Extensions.Options");

	// Aspire types.
	public static readonly TypeIdentity IResource = new(nameof(IResource), "Aspire.Hosting.ApplicationModel");

	public static readonly TypeIdentity IDistributedApplicationBuilder = new(
		nameof(IDistributedApplicationBuilder),
		"Aspire.Hosting"
	);

	// Other useful types
	public static readonly TypeIdentity RequiredAttribute = new(
		nameof(RequiredAttribute),
		"System.ComponentModel.DataAnnotations"
	);

	public static readonly TypeIdentity EditorBrowsableState = new(
		nameof(EditorBrowsableState),
		"System.ComponentModel"
	);

	public static readonly TypeIdentity EditorBrowsableAttribute = new(
		nameof(EditorBrowsableAttribute),
		"System.ComponentModel"
	);

	public static readonly TypeIdentity InvalidOperationException = TypeIdentity.Create<InvalidOperationException>();

	public static readonly TypeIdentity ArgumentNullException = TypeIdentity.Create<ArgumentNullException>();

	public static readonly TypeIdentity Action = new(nameof(Action), "System");

	// Generated attributes (make sure this is after they're all initialized!)
	public static readonly TypeIdentity[] GeneratedTypes = [HostKitAttribute, ResourceDefinitionAttribute];
}
