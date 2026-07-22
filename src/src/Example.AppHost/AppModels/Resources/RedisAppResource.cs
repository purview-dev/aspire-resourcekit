using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

[AppResource(Name = "redis")]
sealed partial class RedisAppResource : ExampleHostAppAppResourceBase<AzureManagedRedisResource>
{
	protected override IResourceBuilder<AzureManagedRedisResource> Build(IDistributedApplicationBuilder builder) =>
		builder.AddAzureManagedRedis(Name).RunAsContainer();
}
