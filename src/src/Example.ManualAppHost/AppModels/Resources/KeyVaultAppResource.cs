using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class KeyVaultAppResource(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzureKeyVaultResource>(hostKit, Platform.ResourceKits.KeyVault)
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AzureKeyVaultResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	) => builder.AddAzureKeyVault(Name);
}
