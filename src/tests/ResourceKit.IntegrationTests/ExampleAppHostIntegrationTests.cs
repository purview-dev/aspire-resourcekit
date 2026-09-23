using Purview.Aspire.ResourceKit.Fixtures;
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit;

[ClassDataSource<ExampleAppHostFixture<Projects.Example_AppHost>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHostIntegrationTests(ExampleAppHostFixture<Projects.Example_AppHost> fixture)
{
	[Test]
	public async Task AppHost_WhenServicesStarted_APIIsHealthy(CancellationToken cancellationToken)
	{
		await fixture.APIIsHealthyAsync(cancellationToken);
	}
}
