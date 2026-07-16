using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

[HostResource]
sealed partial class KeyVaultAppResource : HostAppResource<ExampleHostApp, AzureKeyVaultResource>
{
	public override string Name { get; } = "keyvault";

	protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) =>
		builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AzureKeyVaultResource> Build(IDistributedApplicationBuilder builder) =>
		builder.AddAzureKeyVault(Name);
}
