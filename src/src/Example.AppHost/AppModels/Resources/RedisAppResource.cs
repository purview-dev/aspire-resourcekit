using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels.Resources;

sealed class RedisAppResource : HostAppResource<HostApp, AzureManagedRedisResource>
{
	public override string Name { get; } = "redis";

	protected override IResourceBuilder<AzureManagedRedisResource> Build(IDistributedApplicationBuilder builder) =>
		builder.AddAzureManagedRedis(Name).RunAsContainer();
}
