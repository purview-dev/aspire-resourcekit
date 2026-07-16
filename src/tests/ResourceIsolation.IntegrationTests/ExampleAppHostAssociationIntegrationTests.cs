using Microsoft.Extensions.DependencyInjection;
using Purview.Aspire.ResourceIsolation.Example.AppHost.AppModels;

namespace Purview.Aspire.ResourceIsolation;

[ClassDataSource<ExampleAppHostFixture>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHostAssociationIntegrationTests(ExampleAppHostFixture fixture)
{
	[Test]
	public async Task AppHost_RegistersAllGeneratedResourceServices()
	{
		var expectedServiceType = new[]
		{
			typeof(ExampleHostApp),
			typeof(Example.AppHost.AppModels.Resources.ExampleApiAppResource),
			typeof(Example.AppHost.AppModels.Resources.SqlServerAppResource),
			typeof(Example.AppHost.AppModels.Resources.AzureStorageAppResource),
			typeof(Example.AppHost.AppModels.Resources.KeyVaultAppResource),
			typeof(Example.AppHost.AppModels.Resources.RedisAppResource),
			typeof(Example.AppHost.AppModels.Resources.PublishMarkerAppResource),
		};

		foreach (var expected in expectedServiceType)
		{
			var instance = fixture.App.Services.GetService(expected);

			await Assert.That(instance).IsNotNull();
		}
	}

	[Test]
	public async Task AppHost_InitializesAndAssociatesAllGeneratedResources()
	{
		// Arrange
		ExampleHostApp? hostApp = null;

		// Act
		hostApp = fixture.App.Services.GetService<ExampleHostApp>();

		// Assert
		await Assert.That(hostApp).IsNotNull();

		await Assert.That(hostApp.PublishMarker).IsNotNull();
		await Assert.That(hostApp.SqlServer).IsNotNull();
		await Assert.That(hostApp.AzureStorage).IsNotNull();

		await Assert.That(hostApp.KeyVault.IsEnabled).IsFalse();
		await Assert.That(hostApp.KeyVault).IsNull();

		await Assert.That(hostApp.ExampleApi).IsNotNull();
	}
}
