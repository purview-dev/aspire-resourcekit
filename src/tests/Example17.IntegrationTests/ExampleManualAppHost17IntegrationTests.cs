using Microsoft.Extensions.DependencyInjection;
using Purview.Aspire.ResourceKit.Example;
using Purview.Aspire.ResourceKit.Example17.Fixtures;

namespace Purview.Aspire.ResourceKit.Example17;

/// <summary>
/// Aspire 17 Project V2 without the source generator: the manual host composes the same
/// <c>DotnetProjectResource</c> surface by hand.
/// </summary>
[ClassDataSource<Example17AppHostFixture<Projects.Example_ManualAppHost17>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleManualAppHost17IntegrationTests(
	Example17AppHostFixture<Projects.Example_ManualAppHost17> fixture
)
{
	[Test]
	public async Task AppHost_WhenServicesStarted_APIIsHealthy(CancellationToken cancellationToken)
	{
		// Arrange
		// The fixture is a running AppHost, started once for the test session by TUnit.Aspire.

		// Act
		var client = fixture.CreateHttpClient(Platform.ResourceKits.API, "http");
		var response = await client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

		// Assert
		await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
	}

	[Test]
	public async Task AppHost_WhenServicesStarted_APIIsADotnetProjectResource(CancellationToken cancellationToken)
	{
		// Arrange
		var client = fixture.CreateHttpClient(Platform.ResourceKits.API, "http");
		_ = await client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

		// Act
		var model =
			fixture.App.Services.GetRequiredService<global::Aspire.Hosting.ApplicationModel.DistributedApplicationModel>();
		var api = model.Resources.Single(resource => resource.Name == Platform.ResourceKits.API);

		// Assert
		await Assert.That(api).IsTypeOf<global::Aspire.Hosting.Dotnet.DotnetProjectResource>();
	}
}
