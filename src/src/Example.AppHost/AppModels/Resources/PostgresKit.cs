using Aspire.Hosting.Azure;
using Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Groups;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzurePostgresFlexibleServerResource>(Platform.ResourceKits.Postgres)]
[InGroup<DataGroup>]
sealed partial class PostgresKit
{
	public IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> Database { get; private set; }

	protected override IResourceBuilder<AzurePostgresFlexibleServerResource> BuildResource(
		IDistributedApplicationBuilder builder
	)
	{
		var postgres = builder.AddAzurePostgresFlexibleServer(Name);
		postgres.RunAsContainer(c => c.WithPgWeb(p => p.WithParentRelationship(postgres)));

		Database = postgres.AddDatabase(Platform.ResourceKits.PostgresDb);

		return postgres;
	}
}
