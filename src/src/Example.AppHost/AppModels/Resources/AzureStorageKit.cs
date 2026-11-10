using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;
using Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Groups;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureStorageResource>(Platform.ResourceKits.AzureStorage)]
[InGroup<DataGroup>]
sealed partial class AzureStorageKit
{
	public IResourceBuilder<AzureBlobStorageResource> Blobs { get; private set; }

	protected override IResourceBuilder<AzureStorageResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var azureStorage = builder.AddAzureStorage(Name);
		if (builder.Environment.IsDevelopment())
			azureStorage.RunAsEmulator();

		Blobs = azureStorage.AddBlobs(Platform.ResourceKits.AzureStorageBlob);

		return azureStorage;
	}
}
