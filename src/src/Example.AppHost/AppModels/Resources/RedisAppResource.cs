using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[AppResource(Name = "redis")]
sealed partial class RedisAppResource : ExampleHostAppResourceBase<AzureManagedRedisResource>
{
	protected override IResourceBuilder<AzureManagedRedisResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => builder.AddAzureManagedRedis(Name).RunAsContainer();
}
