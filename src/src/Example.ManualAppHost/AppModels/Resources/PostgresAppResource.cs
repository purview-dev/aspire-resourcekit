using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class PostgresAppResource(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzurePostgresFlexibleServerResource>(hostKit, Platform.ResourceKits.Postgres)
{
	public IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> Database { get; private set; } = default!;

	protected override IResourceBuilder<AzurePostgresFlexibleServerResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	)
	{
		var sql = builder.AddAzurePostgresFlexibleServer(Name).RunAsContainer();
		Database = sql.AddDatabase(Platform.ResourceKits.PostgresDb);

		return sql;
	}
}
