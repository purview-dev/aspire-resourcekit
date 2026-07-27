using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class ExampleAPIAppResource(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, ProjectResource>(hostKit, Platform.ResourceKits.API)
{
	protected override IResourceBuilder<ProjectResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	) => builder.AddProject<Projects.Example_Service>(Name);

	protected override void ConfigureResource()
	{
		if (HostKit.PublishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment("PUBLISH_MARKER", HostKit.PublishMarker.ResourceBuilder);

		ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);
		ResourceBuilder.WithReference(HostKit.AzureStorage.Blobs).WaitFor(HostKit.AzureStorage.Blobs);

		if (HostKit.KeyVault.IsEnabled)
			ResourceBuilder.WithReference(HostKit.KeyVault.ResourceBuilder).WaitFor(HostKit.KeyVault.ResourceBuilder);

		if (HostKit.Redis.IsEnabled)
			ResourceBuilder.WithReference(HostKit.Redis.ResourceBuilder);
	}
}
