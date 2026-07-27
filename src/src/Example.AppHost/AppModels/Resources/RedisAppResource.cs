using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources;

[ResourceDefinition<AzureManagedRedisResource>(Platform.ResourceKits.Redis)]
sealed partial class RedisAppResource
{
	protected override IResourceBuilder<AzureManagedRedisResource> BuildResource(
		[NotNull] IDistributedApplicationBuilder builder
	) => builder.AddAzureManagedRedis(Name).RunAsContainer();
}
