namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class TypeLibrary
{
	// Generated type information...
	public const string PurviewAspireResourceKitNamespace = "Purview.Aspire.ResourceKit";

	public const string OptionsBaseClassSuffix = "Options";

	public const string EmbeddedAttributeSource =
		@"#pragma warning disable IDE0001

namespace Microsoft.CodeAnalysis;

sealed partial class EmbeddedAttribute : global::System.Attribute
{
}
";

	public static readonly TypeValueObject HostKitAttribute = new(
		nameof(HostKitAttribute),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeValueObject ResourceDefinitionAttribute = new(
		nameof(ResourceDefinitionAttribute),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeValueObject GenericResourceDefinitionAttribute = new(
		"ResourceDefinitionAttribute`1",
		PurviewAspireResourceKitNamespace
	);

	// Library types
	public static readonly TypeValueObject IHostKit = new(
		nameof(IHostKit),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeValueObject HostKitBase = new(
		nameof(HostKitBase),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeValueObject ResourceKitBase = new(
		nameof(ResourceKitBase),
		PurviewAspireResourceKitNamespace
	);

	public static readonly TypeValueObject IResourceKit = new(
		nameof(IResourceKit),
		PurviewAspireResourceKitNamespace
	);

	// Other required types
	// Required for DI
	public static readonly TypeValueObject IServiceCollection = new(
		nameof(IServiceCollection),
		"Microsoft.Extensions.DependencyInjection"
	);

	public static readonly TypeValueObject ConfigurationBinder = new(
		nameof(ConfigurationBinder),
		"Microsoft.Extensions.Configuration"
	);

	// Required for Options
	public static readonly TypeValueObject OptionsBuilder = new(
		nameof(OptionsBuilder),
		"Microsoft.Extensions.Options"
	);

	// Aspire types.
	public static readonly TypeValueObject IResource = new(
		nameof(IResource),
		"Aspire.Hosting.ApplicationModel"
	);

	public static readonly TypeValueObject IDistributedApplicationBuilder = new(
		nameof(IDistributedApplicationBuilder),
		"Aspire.Hosting"
	);

	// Other useful types
	public static readonly TypeValueObject RequiredAttribute = new(
		nameof(RequiredAttribute),
		"System.ComponentModel.DataAnnotations"
	);

	public static readonly TypeValueObject EditorBrowsableState = new(
		nameof(EditorBrowsableState),
		"System.ComponentModel"
	);

	public static readonly TypeValueObject EditorBrowsableAttribute = new(
		nameof(EditorBrowsableAttribute),
		"System.ComponentModel"
	);

	public static readonly TypeValueObject NotNullAttribute = new(
		nameof(NotNullAttribute),
		"System.Diagnostics.CodeAnalysis"
	);

	public static readonly TypeValueObject Action = new(nameof(Action), "System");

	public static readonly TypeValueObject EmbeddedAttribute = new(
		nameof(EmbeddedAttribute),
		"Microsoft.CodeAnalysis"
	);

	// Generated attributes (make sure this is after they're all initialized!)
	public static readonly TypeValueObject[] GeneratedTypes =
	[
		HostKitAttribute,
		ResourceDefinitionAttribute,
	];
}
