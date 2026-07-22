using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[AppResource(Name = "sql")]
sealed partial class SqlServerAppResource : ExampleHostAppResourceBase<AzureSqlServerResource>
{
	public IResourceBuilder<AzureSqlDatabaseResource> Database { get; private set; } = default!;

	protected override IResourceBuilder<AzureSqlServerResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var sql = builder.AddAzureSqlServer(Name).RunAsContainer();
		Database = sql.AddDatabase("db");

		return sql;
	}
}
