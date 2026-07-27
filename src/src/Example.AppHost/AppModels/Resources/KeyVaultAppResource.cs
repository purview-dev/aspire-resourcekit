using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureKeyVaultResource>(Platform.ResourceKits.KeyVault)]
sealed partial class KeyVaultAppResource
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AzureKeyVaultResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	) => builder.AddAzureKeyVault(Name);
}
