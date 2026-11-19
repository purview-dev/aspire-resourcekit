using System.ComponentModel.DataAnnotations;
using Aspire.Hosting.Dotnet;
using Purview.Aspire.ResourceKit.Example.AppHost17.AppModels.Groups;

namespace Purview.Aspire.ResourceKit.Example.AppHost17.AppModels.Resources;

// ProjectPath is the only place the path is stated: with no BuildResource override, ResourceKit generates
// AddDotnetProject(builder, Name, path), reading the path from the AppHostProjects.Api.Path constant.
[ResourceDefinition<DotnetProjectResource>(Platform.ResourceKits.API, ProjectPath = AppHostProjects.Api.Path)]
[InGroup<ServicesGroup>]
[ResourceUrl("Health", Platform.EndpointsDefinitions.Health, DisplayText = "Health")]
[EndpointUrl("https", DisplayText = "API")]
sealed partial class ExampleAPIKit
{
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

		if (HostKit.Postgres.IsEnabled && HostKit.Postgres.Database.IsEnabled)
			ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);

		if (HostKit.AzureStorage.IsEnabled && HostKit.AzureStorage.Blobs.IsEnabled)
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
