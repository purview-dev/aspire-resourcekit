using Aspire.Hosting.Azure;
using Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Groups;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureManagedRedisResource>(Platform.ResourceKits.Redis)]
[InGroup<DataGroup>]
sealed partial class RedisKit
{
	protected override IResourceBuilder<AzureManagedRedisResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var redis = builder.AddAzureManagedRedis(Name);

		redis.RunAsContainer(c => c.WithRedisCommander(r => r.WithParentRelationship(redis)));

		return redis;
	}
}
