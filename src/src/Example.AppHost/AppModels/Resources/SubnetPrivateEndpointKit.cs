using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureSubnetResource>(
	Platform.ResourceKits.SubnetPrivateEndpoint,
	PropertyName = "PrivateEndpoint",
	Parent = typeof(VnetKit)
)]
sealed partial class SubnetPrivateEndpointKit
{
	protected override IResourceBuilder<AzureSubnetResource> BuildResource(IDistributedApplicationBuilder builder) =>
		Parent.AddSubnet(Name, "10.0.3.0/24");
}
