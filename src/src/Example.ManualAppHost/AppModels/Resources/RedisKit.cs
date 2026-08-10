using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class RedisKit(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzureManagedRedisResource>(
		hostKit,
		Platform.ResourceKits.Redis
	)
{
	protected override IResourceBuilder<AzureManagedRedisResource> BuildResource(
		IDistributedApplicationBuilder builder
	)
	{
		var redis = builder.AddAzureManagedRedis(Name);

		redis.RunAsContainer(c => c.WithRedisCommander(r => r.WithParentRelationship(redis)));

		return redis;
	}
}
