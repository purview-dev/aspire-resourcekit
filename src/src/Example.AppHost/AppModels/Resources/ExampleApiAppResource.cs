using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[AppResource(Name = "example-api")]
sealed partial class ExampleApiAppResource(
	PublishMarkerAppResource publishMarker,
	SqlServerAppResource sqlServer,
	AzureStorageAppResource azureStorage,
	KeyVaultAppResource keyVault
) : ExampleHostAppResourceBase<ProjectResource>
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name);

	protected override void ConfigureResource([NotNull] ExampleHostApp app)
	{
		// Examples using IoC
		if (publishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment("PUBLISH_MARKER", publishMarker.ResourceBuilder);

		ResourceBuilder.WithReference(sqlServer.Database);
		ResourceBuilder.WithReference(azureStorage.Blobs);

		if (keyVault.IsEnabled)
			ResourceBuilder.WithReference(keyVault.ResourceBuilder);

		// Example of using the `app` parameter.
		ResourceBuilder.WithReference(app.Redis.ResourceBuilder, optional: !app.Redis.IsEnabled);
	}
}
