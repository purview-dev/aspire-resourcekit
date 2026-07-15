using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

sealed class SqlServerAppResource : HostAppResource<HostApp, AzureSqlServerResource>
{
	public override string Name { get; } = "sql";

	public IResourceBuilder<AzureSqlDatabaseResource> Database { get; private set; } = default!;

	protected override IResourceBuilder<AzureSqlServerResource> Build(IDistributedApplicationBuilder builder)
	{
		var sql = builder.AddAzureSqlServer(Name).RunAsContainer();

		return sql;
	}

	protected override void Configure(HostApp app)
	{
		Database = ResourceBuilder.AddDatabase("db");

		base.Configure(app);
	}
}
