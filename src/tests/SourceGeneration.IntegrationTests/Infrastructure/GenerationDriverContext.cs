namespace Purview.Aspire.ResourceKit.SourceGeneration.Infrastructure;

public sealed record GenerationDriverContext(
	bool IncludeSystemNamespaces = true,
	bool IncludeSourceGeneratorNamespaces = true,
	bool IncludeServiceTimelifeReference = true,
	bool IncludeOptionsReference = true,
	bool IncludeOptionsConfigurationExtensionReference = true,
	bool ThrowOnGenerationException = true,
	bool? DisableSourceGenerator = null
)
{
	public static readonly GenerationDriverContext Default = new();

	public static readonly GenerationDriverContext DoNotThrowOnGenerationException = new(
		ThrowOnGenerationException: false
	);

	/// <summary>
	/// Disables the source generator via the
	/// <c>DisableAspireResourceKitSourceGenerator</c> analyzer-config option, matching the
	/// opt-out behaviour supported by <see cref="HostAppGenerator" />.
	/// </summary>
	public static readonly GenerationDriverContext Disabled = new(DisableSourceGenerator: true);

	/// <summary>
	/// Omits the <c>Microsoft.Extensions.DependencyInjection.Abstractions</c> metadata reference so
	/// the <c>ServiceLifetime</c> type is unavailable to the generator, exercising the
	/// <see cref="GeneratorDiagnostics.ServiceLifetiemMissing" /> diagnostic.
	/// </summary>
	public static readonly GenerationDriverContext WithoutServiceLifetime = new(IncludeServiceTimelifeReference: false);

	/// <summary>
	/// Omits the <c>Microsoft.Extensions.Options</c> metadata reference so
	/// configuration-related extension dependencies are unavailable to the generator, exercising the
	/// <see cref="GeneratorDiagnostics.OptionDependencyMissing" /> diagnostic.
	/// </summary>
	public static readonly GenerationDriverContext WithoutOptions = new(IncludeOptionsReference: false);

	/// <summary>
	/// Omits the options/configuration extension metadata reference so
	/// the configuration binder dependency is unavailable to the generator, exercising the
	/// <see cref="GeneratorDiagnostics.OptionDependencyMissing" /> diagnostic.
	/// </summary>
	public static readonly GenerationDriverContext WithoutOptionsConfigurationExtension = new(
		IncludeOptionsConfigurationExtensionReference: false
	);
}
