using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<ProjectResource>(Platform.ResourceKits.API)]
sealed partial class ExampleAPIKit
{
	protected override IResourceBuilder<ProjectResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => builder.AddProject<Projects.Example_Service>(Name);

	protected override void ConfigureResource()
	{
		if (HostKit.PublishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment(
				Options.PublishEnvironmentVariableName,
				HostKit.PublishMarker
			);

		ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);
		ResourceBuilder
			.WithReference(HostKit.AzureStorage.Blobs)
			.WaitFor(HostKit.AzureStorage.Blobs);

		if (HostKit.KeyVault.IsEnabled)
			ResourceBuilder.WithReference(HostKit.KeyVault).WaitFor(HostKit.KeyVault);

		if (HostKit.Redis.IsEnabled)
			ResourceBuilder.WithReference(HostKit.Redis).WaitFor(HostKit.Redis);
	}

	partial class ExampleAPIKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
	}
}
