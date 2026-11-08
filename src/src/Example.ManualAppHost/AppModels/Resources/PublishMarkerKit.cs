namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class PublishMarkerKit(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, ParameterResource>(hostKit, Platform.ResourceKits.PublishMarker)
{
	protected override ResourceKitMode Mode => ResourceKitMode.Publish;

	protected override IResourceBuilder<ParameterResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddParameter(Name, "enabled", secret: false);
}
