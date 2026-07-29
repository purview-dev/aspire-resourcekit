using Purview.Aspire.ResourceKit.Example;
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
		var client = fixture.CreateHttpClient(Platform.ResourceKits.API);

		var response = await client.GetAsync("/health", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
	}

	public static async Task ConnectionStringIsUnavailableAsync<TAppHost>(
		AspireFixture<TAppHost> fixture,
		string resourceName,
		CancellationToken cancellationToken
	)
		where TAppHost : class
	{
		bool hasConnectionString;
		try
		{
			var connectionString = await fixture.GetConnectionStringAsync(resourceName, cancellationToken);
			hasConnectionString = !string.IsNullOrWhiteSpace(connectionString);
		}
		catch (InvalidOperationException)
		{
			hasConnectionString = false;
		}
		catch (ArgumentException)
		{
			hasConnectionString = false;
		}

		await Assert.That(hasConnectionString).IsFalse();
	}
}
