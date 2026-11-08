using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class KeyVaultKit(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzureKeyVaultResource>(hostKit, Platform.ResourceKits.KeyVault)
{
	protected override ResourceKitMode Mode => ResourceKitMode.Publish;

	protected override IResourceBuilder<AzureKeyVaultResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddAzureKeyVault(Name);
}
