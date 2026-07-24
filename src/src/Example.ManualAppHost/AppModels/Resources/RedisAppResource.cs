using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class RedisAppResource()
	: HostResourceBase<ExampleHostApp, AzureManagedRedisResource>(Platform.ResourceKits.Redis)
{
	protected override IResourceBuilder<AzureManagedRedisResource> BuildResource(
		IDistributedApplicationBuilder builder
	) => builder.AddAzureManagedRedis(Name).RunAsContainer();
}
