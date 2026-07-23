using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class SqlServerAppResource() : HostResourceBase<ExampleHostApp, AzureSqlServerResource>("sql")
{
	public IResourceBuilder<AzureSqlDatabaseResource> Database { get; private set; } = default!;

	protected override IResourceBuilder<AzureSqlServerResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var sql = builder.AddAzureSqlServer(Name).RunAsContainer();
		Database = sql.AddDatabase("db");

		return sql;
	}
}
