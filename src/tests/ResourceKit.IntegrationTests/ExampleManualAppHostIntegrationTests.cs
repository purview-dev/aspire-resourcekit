using Projects;
using Purview.Aspire.ResourceKit.Fixtures;

namespace Purview.Aspire.ResourceKit;

[ClassDataSource<ExampleAppHostFixture<Example_ManualAppHost>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleManualAppHostIntegrationTests(ExampleAppHostFixture<Example_ManualAppHost> fixture)
{
	[Test]
	public async Task AppHost_StartsAndIsReachableThroughFixture(CancellationToken cancellationToken)
	{
		await Helpers.AppHostStartedAsync(fixture, cancellationToken);
	}
}
