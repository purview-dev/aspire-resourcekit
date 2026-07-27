using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

sealed partial class RedisAppResource(ExampleHostKit hostKit)
	: ResourceKitBase<ExampleHostKit, AzureManagedRedisResource>(hostKit, Platform.ResourceKits.Redis)
{
	protected override IResourceBuilder<AzureManagedRedisResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	) => builder.AddAzureManagedRedis(Name).RunAsContainer();
}
