namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;

static class TypeHelpers
{
	public const string HostAppAttributeName = "HostAppAttribute";
	public const string AppResourceAttributeName = "AppResourceAttribute";

	public static readonly string[] GeneratedTypes = [HostAppAttributeName, AppResourceAttributeName];

	public const string ResourceIsolationNamespace = "Purview.Aspire.ResourceIsolation";

	public const string FullHostAppAttributeName = ResourceIsolationNamespace + "." + HostAppAttributeName;
	public const string FullAppResourceAttributeName = ResourceIsolationNamespace + "." + AppResourceAttributeName;

	public const string FullServiceLifetimeName = "Microsoft.Extensions.DependencyInjection.ServiceLifetime";

	public const string AppResourceInterfaceName = "IHostAppResource";

	public const string DisableAspireResourceIsolationSourceGeneratorProperty =
		"DisableAspireResourceIsolationSourceGenerator";
}
