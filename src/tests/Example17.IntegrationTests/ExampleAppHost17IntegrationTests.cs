using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dotnet;
using Microsoft.Extensions.DependencyInjection;
using Purview.Aspire.ResourceKit.Example;
using Purview.Aspire.ResourceKit.Example17.Fixtures;

namespace Purview.Aspire.ResourceKit.Example17;

/// <summary>
/// Aspire 17 Project V2 example: the API is added by path with <c>AddDotnetProject</c>, so the
/// running model must contain a <see cref="DotnetProjectResource"/> rather than a
/// <c>ProjectResource</c>.
/// </summary>
[ClassDataSource<Example17AppHostFixture<Projects.Example_AppHost17>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHost17IntegrationTests(Example17AppHostFixture<Projects.Example_AppHost17> fixture)
{
	[Test]
	public async Task AppHost_WhenServicesStarted_APIIsHealthy(CancellationToken cancellationToken)
	{
		// Arrange
		// The fixture is a running AppHost, started once for the test session by TUnit.Aspire.

		// Act
		await APIIsHealthyAsync(cancellationToken);

		// Assert
		// APIIsHealthyAsync asserts the API resource's /health endpoint answered 200 OK.
	}

	[Test]
	public async Task AppHost_WhenServicesStarted_APIIsADotnetProjectResource(CancellationToken cancellationToken)
	{
		// Arrange
		await APIIsHealthyAsync(cancellationToken);

		// Act
		var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
		var api = model.Resources.Single(resource => resource.Name == Platform.ResourceKits.API);

		// Assert
		await Assert.That(api).IsTypeOf<DotnetProjectResource>();
	}

	[Test]
	public async Task AppHost_WhenServicesStarted_ParentsGroupedResourcesToTheirGroupNode(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		await APIIsHealthyAsync(cancellationToken);

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
		await APIIsHealthyAsync(cancellationToken);

		// Act
		var api = SnapshotOf(Platform.ResourceKits.API);

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
	}

	async Task APIIsHealthyAsync(CancellationToken cancellationToken)
	{
		var client = fixture.CreateHttpClient(Platform.ResourceKits.API, "http");
		var response = await client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
		await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
	}

	CustomResourceSnapshot? SnapshotOf(string resourceName) =>
		fixture
			.App.Services.GetRequiredService<ResourceNotificationService>()
			.TryGetCurrentState(resourceName, out var resourceEvent)
			? resourceEvent.Snapshot
			: null;

	static string? ParentOf(ImmutableArray<RelationshipSnapshot> relationships) =>
		relationships.FirstOrDefault(static r => r.Type == "Parent")?.ResourceName;
}
