using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class KeyVaultKit(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzureKeyVaultResource>(hostKit, Platform.ResourceKits.KeyVault)
{
	protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AzureKeyVaultResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddAzureKeyVault(Name);
}
