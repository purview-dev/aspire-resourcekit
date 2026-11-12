using Aspire.Hosting.Azure;
using Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Groups;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureVirtualNetworkResource>(Platform.ResourceKits.Vnet, ResourceKitMode.Publish)]
[InGroup<DataGroup>]
sealed partial class VnetKit
{
	protected override IResourceBuilder<AzureVirtualNetworkResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => builder.AddAzureVirtualNetwork(Name);
}
