using Purview.Aspire.ResourceKit.Example;
using Purview.Aspire.ResourceKit.Fixtures;

namespace Purview.Aspire.ResourceKit;

[ClassDataSource<CustomOptionsExampleAppHostFixture>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHostCustomOptionsIntegrationTests(
	CustomOptionsExampleAppHostFixture fixture
)
{
	[Test]
	public async Task AppHost_WithCustomOptions_IsPassedToTheHostKit(
		CancellationToken cancellationToken
	)
	{
		await Helpers.ConnectionStringIsUnavailableAsync(
			fixture,
			Platform.ResourceKits.Redis,
			cancellationToken
		);

		await Assert
			.That(fixture.GetResourceSnapshot(CustomOptionsExampleAppHostFixture.AzureStorageName))
			.IsNotNull()
			.Because("The custom Azure Storage name should be passed to the host kit.");
		await Assert
			.That(fixture.GetResourceSnapshot(Platform.ResourceKits.AzureStorage))
			.IsNull()
			.Because("The default Azure Storage name should not be passed to the host kit.");
	}
}
