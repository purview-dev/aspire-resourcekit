using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureKeyVaultResource>(Platform.ResourceKits.KeyVault, ResourceKitMode.Publish)]
sealed partial class KeyVaultKit
{
	protected override IResourceBuilder<AzureKeyVaultResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddAzureKeyVault(Name);
}
