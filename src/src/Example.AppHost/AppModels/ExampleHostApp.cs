namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels;

[HostApp]
sealed partial class ExampleHostApp(HostAppBuildMetadata metadata)
{
	public HostAppBuildMetadata Metadata { get; } = metadata;
}
