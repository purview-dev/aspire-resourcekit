namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels;

[HostResources]
sealed partial class HostApp(HostAppBuildMetadata metadata)
{
	public HostAppBuildMetadata Metadata { get; } = metadata;
}
