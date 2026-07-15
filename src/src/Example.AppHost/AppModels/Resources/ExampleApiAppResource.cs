using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

sealed class ExampleApiAppResource(
	PublishMarkerAppResource publishMarker,
	SqlServerAppResource sqlServer,
	AzureStorageAppResource azureStorage,
	KeyVaultAppResource keyVault,
	RedisAppResource redis,
	IEnvironmentTagProvider environmentTagProvider
) : HostAppResource<HostApp, ProjectResource>
{
	public override string Name { get; } = "example-api";

	protected override IResourceBuilder<ProjectResource> Build(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name);

	protected override void Configure([NotNull] HostApp app)
	{
		if (publishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment("PUBLISH_MARKER", publishMarker.ResourceBuilder);

		ResourceBuilder.WithEnvironment("HOST_APP_ENVIRONMENT", environmentTagProvider.GetTag());
		ResourceBuilder.WithEnvironment("HOST_APP_METADATA", app.Metadata.EnvironmentTag);

		ResourceBuilder.WithReference(sqlServer.Database);
		ResourceBuilder.WithReference(azureStorage.Blobs);
		ResourceBuilder.WithReference(keyVault.ResourceBuilder);
		ResourceBuilder.WithReference(redis.ResourceBuilder);
	}
}
