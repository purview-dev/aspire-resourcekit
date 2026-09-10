namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

[GenerateTypeLibrary]
static partial class TypeLibraryGenerator
{
	// Generated type information...
	public static readonly string[] TrimSuffixes = ["ResourceKit", "Resource", "Kit"];

	public const string OptionsBaseClassSuffix = "Options";

	public const string PurviewAspireResourceKitNamespace = "Purview.Aspire.ResourceKit";

	[TypeRef(PurviewAspireResourceKitNamespace, IncludeInGetTypes = true)]
	static readonly TypeIdentity HostKitAttribute = default;

	[TypeRef(PurviewAspireResourceKitNamespace, IncludeInGetTypes = true)]
	static readonly TypeIdentity ResourceDefinitionAttribute = default;

	[TypeRef(nameof(ResourceDefinitionAttribute), PurviewAspireResourceKitNamespace, 1, true)]
	static readonly TypeIdentity GenericResourceDefinitionAttribute = default;

	// Library types
	[TypeRef(PurviewAspireResourceKitNamespace, IncludeInGetTypes = true)]
	static readonly TypeIdentity IHostKit = default;

	[TypeRef(PurviewAspireResourceKitNamespace, 1, true)]
	static readonly TypeIdentity HostKitBase = default;

	[TypeRef(PurviewAspireResourceKitNamespace, IncludeInGetTypes = true)]
	static readonly TypeIdentity ResourceKitBase = default;

	[TypeRef(PurviewAspireResourceKitNamespace, 1, true)]
	static readonly TypeIdentity IResourceKit = default;

	// Other required types
	[TypeRef("Microsoft.Extensions.Configuration")]
	static readonly TypeIdentity ConfigurationBinder = default;

	// Required for Options
	[TypeRef("Microsoft.Extensions.Options")]
	static readonly TypeIdentity OptionsBuilder = default;

	// Aspire types.
	[TypeRef("Aspire.Hosting.ApplicationModel")]
	static readonly TypeIdentity IResource = default;

	[TypeRef("Aspire.Hosting.ApplicationModel", 1)]
	static readonly TypeIdentity IResourceBuilder = default;

	[TypeRef("Aspire.Hosting.ApplicationModel")]
	static readonly TypeIdentity ProjectResource = default;

	[TypeRef("Aspire.Hosting")]
	static readonly TypeIdentity IProjectMetadata = default;

	[TypeRef("Aspire.Hosting.ApplicationModel")]
	static readonly TypeIdentity ResourceAnnotations = default;

	[TypeRef("Aspire.Hosting")]
	static readonly TypeIdentity IDistributedApplicationBuilder = default;

	// Other useful types
	[TypeRef("System.ComponentModel.DataAnnotations")]
	static readonly TypeIdentity RequiredAttribute = default;

	[TypeRef("System.ComponentModel")]
	static readonly TypeIdentity EditorBrowsableState = default;

	[TypeRef("System.ComponentModel")]
	static readonly TypeIdentity EditorBrowsableAttribute = default;
}
