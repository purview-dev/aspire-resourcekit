using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class PostgresDatabaseKit(ExampleHostKit hostKit, PostgresKit parent)
	: ResourceKitBase<ExampleHostKit, AzurePostgresFlexibleServerDatabaseResource>(
		hostKit,
		parent,
		Platform.ResourceKits.PostgresDb
	)
{
	public PostgresKit Parent { get; } = parent;

	protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => Parent.AddDatabase(Name);
}
