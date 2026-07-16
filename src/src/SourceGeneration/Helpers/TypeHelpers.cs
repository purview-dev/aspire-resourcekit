namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;

static class TypeHelpers
{
	public const string HostAppAttributeName = "HostAppAttribute";
	public const string HostResourceAttributeName = "HostResourceAttribute";

	public static readonly string[] GeneratedTypes = [HostAppAttributeName, HostResourceAttributeName];

	public const string ResourceIsolationNamespace = "Purview.Aspire.ResourceIsolation";

	public const string FullHostAppAttributeName = ResourceIsolationNamespace + "." + HostAppAttributeName;
	public const string FullHostResourceAttributeName = ResourceIsolationNamespace + "." + HostResourceAttributeName;

	public const string HostResourceInterfaceName = "IHostAppResource";

	public const string DisableAspireResourceIsolationSourceGeneratorProperty =
		"DisableAspireResourceIsolationSourceGenerator";
}
