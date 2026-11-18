using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.Pre17.AppHost.AppModels.Resources;

[ResourceDefinition<AzurePostgresFlexibleServerDatabaseResource>(
	Platform.ResourceKits.PostgresDb,
	PropertyName = "Database",
	Parent = typeof(PostgresKit)
)]
sealed partial class PostgresDatabaseKit
{
	protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => Parent.AddDatabase(Name);
}
