namespace Purview.Aspire.ResourceKit.Example.AppHost17.AppModels.Resources;

[ResourceDefinition<ParameterResource>(Platform.ResourceKits.PublishMarker, ResourceKitMode.Publish)]
sealed partial class PublishMarkerKit
{
	protected override IResourceBuilder<ParameterResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddParameter(Name, "enabled", secret: false);
}
