using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureSubnetResource>(
	Platform.ResourceKits.SubnetWeb,
	PropertyName = "Web",
	Parent = typeof(VnetKit)
)]
sealed partial class SubnetWebKit
{
	protected override IResourceBuilder<AzureSubnetResource> BuildResource(IDistributedApplicationBuilder builder) =>
		Parent.AddSubnet(Name, "10.0.1.0/24");
}
