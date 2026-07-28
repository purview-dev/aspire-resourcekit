using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition] //<AzureStorageResource>(Platform.ResourceKits.AzureStorage)]
sealed partial class AzureStorageKit : ResourceKitBase<AzureStorageResource>
{
	public IResourceBuilder<AzureBlobStorageResource> Blobs { get; private set; } = default!;

	protected override IResourceBuilder<AzureStorageResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var azureStorage = builder.AddAzureStorage(Name);
		if (builder.Environment.IsDevelopment())
			azureStorage.RunAsEmulator();

		Blobs = azureStorage.AddBlobs(Platform.ResourceKits.AzureStorageBlob);

		return azureStorage;
	}
}
