using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class PostgresKit(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzurePostgresFlexibleServerResource>(hostKit, Platform.ResourceKits.Postgres)
{
	protected override IResourceBuilder<AzurePostgresFlexibleServerResource> BuildResource(
		IDistributedApplicationBuilder builder
	)
	{
		var postgres = builder.AddAzurePostgresFlexibleServer(Name);
		postgres.RunAsContainer(c => c.WithPgWeb(p => p.WithParentRelationship(postgres)));

		return postgres;
	}
}
