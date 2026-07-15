namespace Purview.Aspire.ResourceIsolation;

[ClassDataSource<ExampleAppHostFixture>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHostIntegrationTests(ExampleAppHostFixture fixture)
{
	[Test]
	public async Task AppHost_StartsAndIsReachableThroughFixture()
	{
		await Assert.That(fixture).IsNotNull();
	}
}
