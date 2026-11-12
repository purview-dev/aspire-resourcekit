using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureSubnetResource>(
	Platform.ResourceKits.SubnetData,
	PropertyName = "Data",
	Parent = typeof(VnetKit)
)]
sealed partial class SubnetDataKit
{
	protected override IResourceBuilder<AzureSubnetResource> BuildResource(IDistributedApplicationBuilder builder) =>
		Parent.AddSubnet(Name, "10.0.2.0/24");
}
