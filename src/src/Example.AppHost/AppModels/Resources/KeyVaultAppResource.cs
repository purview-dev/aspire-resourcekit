using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

[AppResource(Name = "keyvault")]
sealed partial class KeyVaultAppResource : ExampleHostAppAppResourceBase<AzureKeyVaultResource>
{
	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AzureKeyVaultResource> Build(IDistributedApplicationBuilder builder) =>
		builder.AddAzureKeyVault(Name);
}
