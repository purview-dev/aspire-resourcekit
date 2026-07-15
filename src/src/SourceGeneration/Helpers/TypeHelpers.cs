namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;

static class TypeHelpers
{
	public const string HostAppAttributeName = "HostAppAttribute";
	public const string HostResourceAttributeName = "HostResourceAttribute";

	public static readonly string[] GeneratedTypes = [HostAppAttributeName, HostResourceAttributeName];

	public const string FullHostAppAttributeName = "Purview.Aspire.ResourceIsolation." + HostAppAttributeName;
	public const string FullHostResourceAttributeName = "Purview.Aspire.ResourceIsolation." + HostResourceAttributeName;

	public const string DisableAspireResourceIsolationSourceGeneratorProperty =
		"DisableAspireResourceIsolationSourceGenerator";
}
