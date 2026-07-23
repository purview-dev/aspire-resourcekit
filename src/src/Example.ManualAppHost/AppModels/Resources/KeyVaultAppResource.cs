using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class KeyVaultAppResource() : HostResourceBase<ExampleHostApp, AzureKeyVaultResource>("key-vault")
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AzureKeyVaultResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddAzureKeyVault(Name);
}
