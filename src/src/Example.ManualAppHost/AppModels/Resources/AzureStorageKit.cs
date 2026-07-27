using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class AzureStorageKit(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzureStorageResource>(hostKit, Platform.ResourceKits.AzureStorage)
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
