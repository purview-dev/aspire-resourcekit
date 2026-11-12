using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureBlobStorageResource>(
	Platform.ResourceKits.AzureStorageBlob,
	PropertyName = "Blobs",
	Parent = typeof(AzureStorageKit)
)]
sealed partial class AzureStorageBlobKit
{
	protected override IResourceBuilder<AzureBlobStorageResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => Parent.AddBlobs(Name);
}
