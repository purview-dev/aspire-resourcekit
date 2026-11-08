using Purview.Aspire.ResourceKit.Fixtures;
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit;

[ClassDataSource<ExampleAppHostFixture<Projects.Example_ManualAppHost>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleManualAppHostIntegrationTests(ExampleAppHostFixture<Projects.Example_ManualAppHost> fixture)
{
	[Test]
	public async Task AppHost_WhenServicesStarted_APIIsHealthy(CancellationToken cancellationToken)
	{
		// Arrange
		// The fixture is a running AppHost, started once for the test session by TUnit.Aspire.

		// Act
		await fixture.APIIsHealthyAsync(cancellationToken);

		// Assert
		// APIIsHealthyAsync asserts the API resource's /health endpoint answered 200 OK.
	}
}
