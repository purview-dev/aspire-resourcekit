using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class AzureStorageAppResource() : HostResourceBase<ExampleHostApp, AzureStorageResource>(name: "azure-storage")
{
	public IResourceBuilder<AzureBlobStorageResource> Blobs { get; private set; } = default!;

	protected override IResourceBuilder<AzureStorageResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddAzureStorage(Name).RunAsEmulator();

	protected override void ConfigureResource(ExampleHostApp app) => Blobs = ResourceBuilder.AddBlobs("blobs");
}
