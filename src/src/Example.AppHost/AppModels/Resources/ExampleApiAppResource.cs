using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<ProjectResource>(Platform.ResourceKits.API)]
sealed partial class ExampleAPIAppResource
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name);

	protected override void ConfigureResource([NotNull] ExampleHostApp app)
	{
		if (app.PublishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment(Options.PublishEnvironmentVariableName, app.PublishMarker.ResourceBuilder);

		ResourceBuilder.WithReference(app.Postgres.Database).WaitFor(app.Postgres.Database);
		ResourceBuilder.WithReference(app.AzureStorage.Blobs).WaitFor(app.AzureStorage.Blobs);

		if (app.KeyVault.IsEnabled)
			ResourceBuilder.WithReference(app.KeyVault.ResourceBuilder).WaitFor(app.KeyVault.ResourceBuilder);

		if (app.Redis.IsEnabled)
			ResourceBuilder.WithReference(app.Redis.ResourceBuilder);
	}
}

partial class ExampleAPIAppResourceOptions
{
	[Required(AllowEmptyStrings = false)]
	public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
}
