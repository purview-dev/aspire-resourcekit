using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class PostgresKit(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzurePostgresFlexibleServerResource>(
		hostKit,
		Platform.ResourceKits.Postgres
	)
{
	public IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> Database
	{
		get;
		private set;
	} = default!;

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
