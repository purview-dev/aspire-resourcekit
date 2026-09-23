using Purview.Aspire.ResourceKit.Fixtures;
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit;

[ClassDataSource<ExampleAppHostFixture<Projects.Example_ManualAppHost>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleManualAppHostIntegrationTests(ExampleAppHostFixture<Projects.Example_ManualAppHost> fixture)
{
	[Test]
	public async Task AppHost_WhenServicesStarted_APIIsHealthy(CancellationToken cancellationToken)
	{
		await fixture.APIIsHealthyAsync(cancellationToken);
	}
}
