using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<Projects.Example_Service>(Platform.ResourceKits.API)]
sealed partial class ExampleAPIKit
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name).WithUrl("/health", "Health");

	protected override void ConfigureResource()
	{
		if (HostKit.PublishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment(Options.PublishEnvironmentVariableName, HostKit.PublishMarker);

		ResourceBuilder.WithEnvironment(
			OptionsHelper
				.Environment(
					new DemoServiceEnvironmentOptions
					{
						Service = new()
						{
							Name = Name,
							Enabled = true,
							Labels = { ["region"] = "west" },
						},
						Replicas = [1, 2, 3],
						Routes = { ["health"] = "/health" },
					}
				)
				.Override(static o => o.Service.Name = "api")
				.Ignore(static o => o.Service.Labels)
				.Build()
		);

		ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);
		ResourceBuilder.WithReference(HostKit.AzureStorage.Blobs).WaitFor(HostKit.AzureStorage.Blobs);

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

	sealed class DemoServiceEnvironmentOptions
	{
		public DemoServiceOptions Service { get; set; } = new();

		public List<int> Replicas { get; set; } = [];

		public Dictionary<string, string> Routes { get; set; } = [];
	}

	sealed class DemoServiceOptions
	{
		public string Name { get; set; } = string.Empty;

		public bool Enabled { get; set; }

		public Dictionary<string, string> Labels { get; set; } = [];
	}
}
