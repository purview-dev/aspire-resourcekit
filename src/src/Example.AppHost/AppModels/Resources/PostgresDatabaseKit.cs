using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzurePostgresFlexibleServerDatabaseResource>(
	Platform.ResourceKits.PostgresDb,
	PropertyName = "Database",
	Parent = typeof(PostgresKit)
)]
sealed partial class PostgresDatabaseKit
{
	protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => Parent.ResourceBuilder.AddDatabase(Name);
}
