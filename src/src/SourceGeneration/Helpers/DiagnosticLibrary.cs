using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class DiagnosticLibrary
{
	const string Category = TypeLibraryGenerator.PurviewAspireResourceKitNamespace + ".SourceGenerator";

	public static readonly DiagnosticDescriptor ClassMustBePartial = new(
		id: "SG0001",
		title: "Class must be partial",
		messageFormat: "'{0}' must be declared partial to allow source generation",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Classes decorated with source-generation attributes must carry the 'partial' modifier "
			+ "so that the generator can emit additional members into the same class."
	);

	public static readonly DiagnosticDescriptor NoResourceKitsDefined = new(
		id: "SG0002",
		title: "No Host Kit resources defined",
		messageFormat: "No Host Kit resources were defined for '{0}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		customTags: [WellKnownDiagnosticTags.CompilationEnd]
	);

	public static readonly DiagnosticDescriptor NoHostKitInfoDefined = new(
		id: "SG0003",
		title: "No Host Kit info defined",
		messageFormat: "No Host Kit info was defined",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor MultipleHostKitsFoundInfo = new(
		id: "SG0004",
		title: "Multiple Host Kits defined",
		messageFormat: "Multiple Host Kits were defined in the app, only a single one is permitted",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: [WellKnownDiagnosticTags.CompilationEnd]
	);

	public static readonly DiagnosticDescriptor DuplicateResourcePropertyName = new(
		id: "SG0005",
		title: "Duplicate resource property name",
		messageFormat: "The property name '{0}' is used by multiple app resources; property names must be unique",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: [WellKnownDiagnosticTags.CompilationEnd]
	);

	public static readonly DiagnosticDescriptor ResourceMustDeriveFromResourceKitBase = new(
		id: "SG0006",
		title: $"Resource Kit must derive from {TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase.Name}<TResource> or {TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase.Name}<THostKit, TResource>",
		messageFormat: "'{0}' must derive from a valid Resource Kit Base",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourceNameNotDerivable = new(
		id: "SG0007",
		title: "Resource name could not be determined",
		messageFormat: "A resource name could not be derived from '{0}' and no Name was specified on the ResourceDefinitionAttribute",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidPropertyName = new(
		id: "SG0008",
		title: "Invalid property name",
		messageFormat: "The PropertyName '{0}' is not a valid C# identifier",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ServiceCollectionMissing = new(
		id: "SG0009",
		title: "IServiceCollection type missing",
		messageFormat: "Add the `Microsoft.Extensions.DependencyInjection.Abstractions` NuGet package",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor OptionDependencyMissing = new(
		id: "SG0010",
		title: "Configuration binder type missing",
		messageFormat: "Add the `Microsoft.Extensions.Configuration.Binder` NuGet package",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor OptionsBuilderConfigurationExtensionMissing = new(
		id: "SG0011",
		title: "Options configuration extension method missing",
		messageFormat: "Add the `Microsoft.Extensions.Options.ConfigurationExtensions` NuGet package",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonEmptyConstructorsNotSupported = new(
		id: "SG0012",
		title: "Non-empty constructors are not supported",
		messageFormat: "'{0}' must not declare constructors with parameters or executable constructor bodies",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor MixedResourceDefinitionAttributesNotSupported = new(
		id: "SG0013",
		title: "Mixed ResourceDefinition attribute usage is not supported",
		messageFormat: "'{0}' cannot use both ResourceDefinition and ResourceDefinition<TResource> attributes",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonGenericResourceDefinitionRequiresExplicitBase = new(
		id: "SG0014",
		title: "Non-generic ResourceDefinition requires explicit base",
		messageFormat: "'{0}' uses ResourceDefinition and must explicitly derive from '{1}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor GenericResourceDefinitionCannotHaveExplicitBase = new(
		id: "SG0015",
		title: "Generic ResourceDefinition cannot have explicit base",
		messageFormat: "'{0}' uses ResourceDefinition<TResource> and must not declare an explicit base type",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NoAspireResourceFound = new(
		id: "SG0016",
		title: "No Aspire resource found",
		messageFormat: "No Aspire resource, use the ResourceDefinition<TResource> or implement IResourceKit<TResource>, or inherit from ResourceKitBase<THostKit, TResource> or the generated ResourceKitBase<TResource>",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourcePropertyNeverSet = new(
		id: "SG0017",
		title: "Resource property is never set",
		messageFormat: "The '{0}' property of type '{1}' is never assigned in BuildResource or ConfigureResource; resource kit properties must be populated during the build or configure lifecycle",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Execution-only concern: the resource kit will fail at runtime, not generation."
	);

	public static readonly DiagnosticDescriptor ProjectDefinitionMismatch = new(
		id: "SG0018",
		title: "Project resource definition mismatch",
		messageFormat: "The '{0}' resource kit declares project '{1}' but BuildResource must add the same project via AddProject<T>()",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Execution-only concern: the declared project is not registered, so the resource fails at runtime; generation is not blocked."
	);

	public static readonly DiagnosticDescriptor ProjectResourceKitBaseMismatch = new(
		id: "SG0019",
		title: "Project resource kit base must use ProjectResource",
		messageFormat: "The '{0}' resource kit declares a project but its explicit base class '{1}' does not use ProjectResource",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Execution-only concern: the explicit base cannot build the declared project resource; generation is not blocked."
	);

	public static readonly DiagnosticDescriptor ResourceKitCannotParentItself = new(
		id: "SG0032",
		title: "Resource kit cannot be its own parent",
		messageFormat: "'{0}' cannot declare itself as its parent",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourceKitParentCycle = new(
		id: "SG0033",
		title: "Resource kit parent cycle",
		messageFormat: "Resource kits form a parent cycle: {0}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A resource kit's parent chain must be acyclic; a cycle has no valid build order."
	);

	public static readonly DiagnosticDescriptor ResourceKitParentNotResourceKit = new(
		id: "SG0034",
		title: "Parent type is not a resource kit",
		messageFormat: "The parent '{0}' of '{1}' is not a resource kit; mark it with ResourceDefinitionAttribute",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourceKitParentUnavailable = new(
		id: "SG0035",
		title: "Parent resource kit is not part of this composition",
		messageFormat: "The parent '{0}' of '{1}' is not part of this host kit composition",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A parent must be generated as a resource kit in the same compilation and host kit as its child."
	);

	public static readonly DiagnosticDescriptor ResourceKitChildPropertyCollision = new(
		id: "SG0036",
		title: "Generated child property collides with a member on the parent",
		messageFormat: "The generated property '{0}' on '{1}' collides with a member already declared on the parent resource kit",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DuplicateResourceKitChildPropertyName = new(
		id: "SG0037",
		title: "Duplicate child property name",
		messageFormat: "The child property name '{0}' is used by multiple children of '{1}'; child property names must be unique within a parent",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourceKitParentMemberCollision = new(
		id: "SG0038",
		title: "Generated Parent member collides with a user-authored member",
		messageFormat: "The generated '{0}' member on '{1}' collides with a member already declared on the resource kit",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourceKitChildGroupDiffersFromParent = new(
		id: "SG0039",
		title: "Child resource kit uses a different primary group than its parent",
		messageFormat: "The '{0}' resource kit uses primary group '{1}', but its parent '{2}' uses '{3}'. The child renders in its own group unless an Aspire parent relationship places it under the parent.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "ResourceKit parentage is lifecycle ownership; group membership is visual placement. A child with no [InGroup] inherits the parent's primary group. An explicit child group overrides that, so a mismatch is easy to miss."
	);

	public static readonly DiagnosticDescriptor RedundantResourceBuilderAccess = new(
		id: "SG0040",
		title: "Resource kit is already a resource builder",
		messageFormat: "Use the resource kit directly; it implements IResourceBuilder<TResource>, so '.ResourceBuilder' is redundant",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: "A resource kit derives from ResourceKitBase<THostKit, TResource>, which implements IResourceBuilder<TResource>. Calling an extension method on the kit and on kit.ResourceBuilder is equivalent."
	);

	public static readonly DiagnosticDescriptor AssignSetsMultiplePropertyPaths = new(
		id: "SG0020",
		title: "OptionsHelper.Assign action must set exactly one property path",
		messageFormat: "An OptionsHelper.Assign/Override action must set exactly one property path. Found {0} assignments: {1}. Split each into its own assignment: Assign<TOptions>(o => o.A = ..., o => o.B = ...).",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Each OptionsHelper.Assign/Override action must set exactly one property path; an action that assigns more than one property throws at runtime when the arguments are built."
	);

	public static readonly DiagnosticDescriptor GroupTypeNotResourceGroup = new(
		id: "SG0021",
		title: "Type is not a resource group",
		messageFormat: "'{1}' uses '{0}' as a resource group, but '{0}' is not a [ResourceGroup] class declared in this compilation",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "The type argument of [InGroup<TGroup>], and the Parent of a [ResourceGroup], must be a class marked [ResourceGroup] in the same compilation, because the generator emits the group alongside the host kit."
	);

	public static readonly DiagnosticDescriptor ResourceGroupCycle = new(
		id: "SG0022",
		title: "Resource group parent cycle",
		messageFormat: "The resource group '{0}' is its own ancestor; remove a Parent from the cycle",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DuplicateResourceGroupName = new(
		id: "SG0023",
		title: "Duplicate resource group name",
		messageFormat: "The resource group name '{0}' is used by more than one group; group names and property names must be unique",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A group's name identifies it in the dashboard, where names are compared case-insensitively, so two groups with the same name could not be told apart; its property name is a member of the generated Groups accessor.",
		customTags: [WellKnownDiagnosticTags.CompilationEnd]
	);

	public static readonly DiagnosticDescriptor MultiplePrimaryGroups = new(
		id: "SG0024",
		title: "More than one primary group",
		messageFormat: "'{0}' marks more than one group as Primary; '{1}' is used as the primary group",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "A resource is shown under one parent in the dashboard, so only one of its groups can be primary."
	);

	public static readonly DiagnosticDescriptor ResourceGroupCannotHaveExplicitBase = new(
		id: "SG0025",
		title: "Resource group cannot have explicit base",
		messageFormat: "The resource group '{0}' must not declare a base type; the generator derives it from ResourceKitGroup<THostKit>",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DuplicateUrlName = new(
		id: "SG0026",
		title: "Duplicate URL name",
		messageFormat: "'{0}' declares more than one URL named '{1}'; URL names must be unique per resource kit or group",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A URL's name is its generated options property. An [EndpointUrl] is named after its endpoint."
	);

	public static readonly DiagnosticDescriptor EndpointUrlRequiresEndpoints = new(
		id: "SG0027",
		title: "Endpoint URL on a resource without endpoints",
		messageFormat: "'{0}' declares a URL for endpoint '{1}', but '{2}' does not have endpoints",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "[EndpointUrl], and [ResourceUrl] with an Endpoint, need a resource type that implements IResourceWithEndpoints. A resource group has no endpoints."
	);

	public static readonly DiagnosticDescriptor RelativeUrlWithoutEndpoints = new(
		id: "SG0028",
		title: "Relative URL on a resource without endpoints",
		messageFormat: "'{0}' declares the relative URL '{1}', but '{2}' does not have endpoints to resolve it against",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Execution-only concern: the URL is skipped at runtime. Use an absolute URL, or declare it on a resource with endpoints."
	);

	public static readonly DiagnosticDescriptor MultipleResourceGroupParents = new(
		id: "SG0029",
		title: "Resource group declares its parent more than once",
		messageFormat: "The resource group '{0}' declares its parent more than once; use either ResourceGroup<TParent> or Parent = typeof(TParent), on a single [ResourceGroup] attribute",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A group has exactly one parent, so ResourceGroup and ResourceGroup<TParent> must not both be applied, and ResourceGroup<TParent> must not also set Parent."
	);

	public static readonly DiagnosticDescriptor ResourceGroupCannotBeGeneric = new(
		id: "SG0030",
		title: "Resource group cannot be generic",
		messageFormat: "The resource group '{0}' must not be generic or nested in a generic type",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A group is a single named node, created once per host kit. A generic group has no single instance, and a group parented to a constructed form of itself could expand without end."
	);

	public static readonly DiagnosticDescriptor ResourceGroupCannotBeResourceKit = new(
		id: "SG0031",
		title: "Resource group cannot be a resource kit or a group member",
		messageFormat: "The resource group '{0}' cannot also carry [ResourceDefinition] or [InGroup]; nest a group with ResourceGroup<TParent> or Parent instead",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A class is either a resource kit, which joins groups with [InGroup], or a resource group, which nests under a parent group. One class cannot be both, so a group can never contain itself as a member."
	);
}
