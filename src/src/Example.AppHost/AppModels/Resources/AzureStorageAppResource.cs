using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

sealed class AzureStorageAppResource : HostAppResource<HostApp, AzureStorageResource>
{
	public override string Name { get; } = "azure-storage";

	public IResourceBuilder<AzureBlobStorageResource> Blobs { get; private set; } = default!;

	protected override IResourceBuilder<AzureStorageResource> Build(IDistributedApplicationBuilder builder) =>
		builder.AddAzureStorage(Name).RunAsEmulator();

	protected override void Configure(HostApp app)
	{
		Blobs = ResourceBuilder.AddBlobs("blobs");

		base.Configure(app);
	}
}
