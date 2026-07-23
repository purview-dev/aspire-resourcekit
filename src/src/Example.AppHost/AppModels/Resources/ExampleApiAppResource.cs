using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[AppResource(Name = "example-api")]
sealed partial class ExampleApiAppResource : ExampleHostAppResourceBase<ProjectResource>
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Example_Service>(Name);

	protected override void ConfigureResource([NotNull] ExampleHostApp app)
	{
		// Examples using IoC
		if (app.PublishMarker.IsEnabled)
			ResourceBuilder.WithEnvironment("PUBLISH_MARKER", app.PublishMarker.ResourceBuilder);

		ResourceBuilder.WithReference(app.SqlServer.Database);
		ResourceBuilder.WithReference(app.AzureStorage.Blobs);

		if (app.KeyVault.IsEnabled)
			ResourceBuilder.WithReference(app.KeyVault.ResourceBuilder);

		// Example of using the `app` parameter.
		ResourceBuilder.WithReference(app.Redis.ResourceBuilder, optional: !app.Redis.IsEnabled);
	}
}
