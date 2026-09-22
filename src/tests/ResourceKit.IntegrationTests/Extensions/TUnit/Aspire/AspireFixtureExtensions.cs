using System.ComponentModel;
using Purview.Aspire.ResourceKit.Example;

namespace TUnit.Aspire;

[EditorBrowsable(EditorBrowsableState.Never)]
static class AspireFixtureExtensions
{
	extension<TAppHost>(AspireFixture<TAppHost> fixture)
		where TAppHost : class
	{
		public async Task APIIsHealthyAsync(CancellationToken cancellationToken)
		{
			var client = fixture.CreateHttpClient(Platform.ResourceKits.API);
			var response = await client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

			await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
		}

		public async Task ConnectionStringIsUnavailableAsync(string resourceName, CancellationToken cancellationToken)
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
}
