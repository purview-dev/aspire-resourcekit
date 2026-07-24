using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit;

static class Helpers
{
	public static async Task APIIsHealthyAsync<TAppHost>(
		AspireFixture<TAppHost> fixture,
		CancellationToken cancellationToken
	)
		where TAppHost : class
	{
		var client = fixture.CreateHttpClient("api");

		var response = await client.GetAsync("/health", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
	}
}
