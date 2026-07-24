using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition(Platform.ResourceKits.KeyVault, GenerateOptions = true)]
sealed partial class KeyVaultAppResource : ExampleHostAppResourceBase<AzureKeyVaultResource>
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AzureKeyVaultResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddAzureKeyVault(Name);
}
