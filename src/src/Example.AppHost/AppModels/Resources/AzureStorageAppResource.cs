using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[AppResource(Name = "azure-storage")]
sealed partial class AzureStorageAppResource : ExampleHostAppResourceBase<AzureStorageResource>
{
	public IResourceBuilder<AzureBlobStorageResource> Blobs { get; private set; } = default!;

	protected override IResourceBuilder<AzureStorageResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddAzureStorage(Name).RunAsEmulator();

	protected override void ConfigureResource(ExampleHostApp app) => Blobs = ResourceBuilder.AddBlobs("blobs");
}
