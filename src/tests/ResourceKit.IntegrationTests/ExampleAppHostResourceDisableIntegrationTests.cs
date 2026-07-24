using Purview.Aspire.ResourceKit.Example;
using Purview.Aspire.ResourceKit.Fixtures;

namespace Purview.Aspire.ResourceKit;

[ClassDataSource<RedisDisabledExampleAppHostFixture>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHostResourceDisableIntegrationTests(RedisDisabledExampleAppHostFixture fixture)
{
	[Test]
	public async Task AppHost_WhenRedisDisabledByConfiguration_RedisConnectionStringIsUnavailable(
		CancellationToken cancellationToken
	)
	{
		await Helpers.ConnectionStringIsUnavailableAsync(fixture, Platform.ResourceKits.Redis, cancellationToken);
	}
}
