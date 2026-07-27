using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzurePostgresFlexibleServerResource>(Platform.ResourceKits.Postgres)]
sealed partial class PostgresAppResource
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
