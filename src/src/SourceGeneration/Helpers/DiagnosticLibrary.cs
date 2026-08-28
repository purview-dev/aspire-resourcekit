using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class DiagnosticLibrary
{
	const string Category = TypeLibrary.PurviewAspireResourceKitNamespace + ".SourceGenerator";

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
		isEnabledByDefault: true
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
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DuplicateResourcePropertyName = new(
		id: "SG0005",
		title: "Duplicate resource property name",
		messageFormat: "The property name '{0}' is used by multiple app resources; property names must be unique",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor ResourceMustDeriveFromResourceKitBase = new(
		id: "SG0006",
		title: $"Resource Kit must derive from {TypeLibrary.ResourceKitBase.Name}<TResource> or {TypeLibrary.ResourceKitBase.Name}<THostKit, TResource>",
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
}
