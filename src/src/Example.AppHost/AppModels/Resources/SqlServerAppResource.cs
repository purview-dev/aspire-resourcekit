using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

[AppResource(Name = "sql")]
sealed partial class SqlServerAppResource : ExampleHostAppAppResourceBase<AzureSqlServerResource>
{
	public IResourceBuilder<AzureSqlDatabaseResource> Database { get; private set; } = default!;

	protected override IResourceBuilder<AzureSqlServerResource> Build(IDistributedApplicationBuilder builder)
	{
		var sql = builder.AddAzureSqlServer(Name).RunAsContainer();
		Database = sql.AddDatabase("db");

		return sql;
	}
}
