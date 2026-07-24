using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class PostgresAppResource()
	: ResourceKitBase<ExampleHostApp, AzurePostgresFlexibleServerResource>(Platform.ResourceKits.Postgres)
{
	public IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> Database { get; private set; } = default!;

	protected override IResourceBuilder<AzurePostgresFlexibleServerResource> BuildResource(
		IDistributedApplicationBuilder builder
	)
	{
		var sql = builder.AddAzurePostgresFlexibleServer(Name).RunAsContainer();
		Database = sql.AddDatabase(Platform.ResourceKits.PostgresDb);

		return sql;
	}
}
