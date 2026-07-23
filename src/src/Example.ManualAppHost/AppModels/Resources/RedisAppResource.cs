using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class RedisAppResource() : HostResourceBase<ExampleHostApp, AzureManagedRedisResource>("redis")
{
	protected override IResourceBuilder<AzureManagedRedisResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => builder.AddAzureManagedRedis(Name).RunAsContainer();
}
