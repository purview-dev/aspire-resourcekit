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

	public static readonly DiagnosticDescriptor AssignSetsMultiplePropertyPaths = new(
		id: "SG0020",
		title: "OptionsHelper.Assign action must set exactly one property path",
		messageFormat: "An OptionsHelper.Assign action must set exactly one property path. Found {0} assignments: {1}. Split each into its own assignment: Assign<TOptions>(o => o.A = ..., o => o.B = ...).",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Each OptionsHelper.Assign action must set exactly one property path; an action that assigns more than one property throws at runtime when the arguments are built."
	);
}
