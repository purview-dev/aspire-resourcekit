namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

[Generate("Purview.Aspire.ResourceKit.HostKitAttribute")]
readonly partial record struct HostKitAttributeData(
	// For the property
	[Property]
	// For the ctor
	[Argument("name")]
		string? Name,
	string? ExtensionMethodName,
#pragma warning disable format
	[Property] [Argument("generateOptions", DefaultValue = true)] bool GenerateOptions,
	[Property(IsEnum = true, DefaultValue = "Purview.Aspire.ResourceKit.ResourceKitMode.All")]
	[Argument("mode", IsEnum = true, DefaultValue = "Purview.Aspire.ResourceKit.ResourceKitMode.All")]
		string? Mode
#pragma warning restore format
);

[Generate("Purview.Aspire.ResourceKit.ResourceDefinitionAttribute", MatchByInheritance = true)]
readonly partial record struct ResourceDefinitionAttributeData(
	// For the property
	[Property]
	// For the ctor
	[Argument("name")]
		string? Name,
	// For the property
	[Property]
	// For the ctor
	[Argument("propertyName")]
		string? PropertyName,
	[Property(IsEnum = true, DefaultValue = "Purview.Aspire.ResourceKit.ResourceKitMode.All")]
	[Argument("mode", IsEnum = true, DefaultValue = "Purview.Aspire.ResourceKit.ResourceKitMode.All")]
		string? Mode,
	[GenericTypeArgument] TypeIdentity AspireResourceType
);

// Parent is a System.Type, which a model cannot hold (ADM0010), and ResourceGroupAttribute<TParent> carries the
// parent as a type argument; ResourceKitRules.GetGroupParentDeclarations reads both.
[Generate("Purview.Aspire.ResourceKit.ResourceGroupAttribute", MatchByInheritance = true)]
readonly partial record struct ResourceGroupAttributeData(
	// For the property
	[Property]
	// For the ctor
	[Argument("name")]
		string? Name,
#pragma warning disable format
	[Property(DefaultValue = true)] bool ShowInDashboard,
	[Property(IsEnum = true, DefaultValue = "Purview.Aspire.ResourceKit.ResourceKitMode.All")] string? Mode
#pragma warning restore format
);

[Generate("Purview.Aspire.ResourceKit.InGroupAttribute", MatchByInheritance = true)]
readonly partial record struct InGroupAttributeData(
	[Property] bool Primary,
	[GenericTypeArgument] TypeIdentity GroupType
);

[Generate("Purview.Aspire.ResourceKit.ResourceUrlAttribute")]
readonly partial record struct ResourceUrlAttributeData(
#pragma warning disable format
	[Argument("name")] string? Name,
	[Argument("url")] string? Url,
	[Property] string? DisplayText,
	[Property] string? Endpoint,
	[Property(IsEnum = true, DefaultValue = "Aspire.Hosting.ApplicationModel.UrlDisplayLocation.SummaryAndDetails")]
		string? DisplayLocation
#pragma warning restore format
);

[Generate("Purview.Aspire.ResourceKit.EndpointUrlAttribute")]
readonly partial record struct EndpointUrlAttributeData(
#pragma warning disable format
	[Argument("endpoint")] string? Endpoint,
	[Property] string? DisplayText,
	[Property(IsEnum = true, DefaultValue = "Aspire.Hosting.ApplicationModel.UrlDisplayLocation.SummaryAndDetails")]
		string? DisplayLocation
#pragma warning restore format
);
