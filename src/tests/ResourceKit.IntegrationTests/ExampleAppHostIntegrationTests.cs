using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Purview.Aspire.ResourceKit.Example;
using Purview.Aspire.ResourceKit.Fixtures;
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit;

[ClassDataSource<ExampleAppHostFixture<Projects.Example_AppHost>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHostIntegrationTests(ExampleAppHostFixture<Projects.Example_AppHost> fixture)
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

	[Test]
	public async Task AppHost_WhenServicesStarted_ParentsGroupedResourcesToTheirGroupNode(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// The example declares PlatformGroup > DataGroup > PostgresKit, so both group nodes exist and are nested.
		await fixture.APIIsHealthyAsync(cancellationToken);

		// Act
		var data = SnapshotOf(Platform.ResourceGroups.Data);
		var postgres = SnapshotOf(Platform.ResourceKits.Postgres);

		// Assert
		await Assert.That(data).IsNotNull();
		await Assert.That(ParentOf(data!.Relationships)).IsEqualTo(Platform.ResourceGroups.Platform);
		await Assert.That(ParentOf(postgres!.Relationships)).IsEqualTo(Platform.ResourceGroups.Data);
	}

	[Test]
	public async Task AppHost_WhenServicesStarted_ShowsNamedUrls(CancellationToken cancellationToken)
	{
		// Arrange
		// ExampleAPIKit declares [ResourceUrl("Health", "/health")]; PlatformGroup declares an absolute docs URL.
		await fixture.APIIsHealthyAsync(cancellationToken);

		// Act
		var api = SnapshotOf(Platform.ResourceKits.API);
		var platform = SnapshotOf(Platform.ResourceGroups.Platform);

		// Assert
		await Assert
			.That(
				api!.Urls.Count(static u =>
					u.DisplayProperties.DisplayName == "Health" && u.Url.EndsWith("/health", StringComparison.Ordinal)
				)
			)
			.IsEqualTo(1);
		await Assert
			.That(api.Urls.Any(static u => u.DisplayProperties.DisplayName == "API"))
			.IsTrue()
			.Because("[EndpointUrl(\"https\", DisplayText = \"API\")] renames the https endpoint URL.");
		await Assert.That(platform!.Urls.Any(static u => u.Url == Platform.ResourceGroups.DocsUrl)).IsTrue();
	}

	// The dashboard's view of a resource: the snapshot the resource notification service publishes.
	CustomResourceSnapshot? SnapshotOf(string resourceName) =>
		fixture
			.App.Services.GetRequiredService<ResourceNotificationService>()
			.TryGetCurrentState(resourceName, out var resourceEvent)
			? resourceEvent.Snapshot
			: null;

	static string? ParentOf(ImmutableArray<RelationshipSnapshot> relationships) =>
		relationships.FirstOrDefault(static r => r.Type == "Parent")?.ResourceName;
}
