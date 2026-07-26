using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class ExampleAPIAppResource()
	: ResourceKitBase<ExampleHostApp, ProjectResource>(Platform.ResourceKits.API)
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name);

	protected override void ConfigureResource([NotNull] ExampleHostApp app)
	{
		if (app.PublishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment("PUBLISH_MARKER", app.PublishMarker.ResourceBuilder);

		ResourceBuilder.WithReference(app.Postgres.Database).WaitFor(app.Postgres.Database);
		ResourceBuilder.WithReference(app.AzureStorage.Blobs).WaitFor(app.AzureStorage.Blobs);

		if (app.KeyVault.IsEnabled)
			ResourceBuilder.WithReference(app.KeyVault.ResourceBuilder).WaitFor(app.KeyVault.ResourceBuilder);

		if (app.Redis.IsEnabled)
			ResourceBuilder.WithReference(app.Redis.ResourceBuilder);
	}
}
