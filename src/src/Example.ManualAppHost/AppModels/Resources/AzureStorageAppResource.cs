using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class AzureStorageAppResource(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzureStorageResource>(hostKit, Platform.ResourceKits.AzureStorage)
{
	public IResourceBuilder<AzureBlobStorageResource> Blobs { get; private set; } = default!;

	protected override IResourceBuilder<AzureStorageResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	) => builder.AddAzureStorage(Name).RunAsEmulator();

	protected override void ConfigureResource() =>
		Blobs = ResourceBuilder.AddBlobs(Platform.ResourceKits.AzureStorageBlob);
}
