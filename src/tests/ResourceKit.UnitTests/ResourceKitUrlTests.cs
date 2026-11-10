using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

public sealed class ResourceKitUrlTests
{
	[Test]
	public async Task Apply_GivenRelativeUrlWithoutEndpoint_AddsOneUrlAgainstHttpsEndpoint()
	{
		// Arrange
		var urls = CreateEndpointUrls();
		ResourceKitUrl url = new() { Url = "/health", DisplayText = "Health" };

		// Act
		var unresolved = ResourceKitUrl.Apply([url], urls);

		// Assert
		await Assert.That(unresolved).IsEmpty();
		await Assert.That(urls.Count).IsEqualTo(3);

		var added = urls[^1];
		await Assert.That(added.Url).IsEqualTo("https://localhost:5001/health");
		await Assert.That(added.DisplayText).IsEqualTo("Health");
		await Assert.That(added.Endpoint?.EndpointName).IsEqualTo("https");
	}

	[Test]
	public async Task Apply_GivenRelativeUrlWithNamedEndpoint_ResolvesAgainstThatEndpoint()
	{
		// Arrange
		var urls = CreateEndpointUrls();
		ResourceKitUrl url = new() { Url = "/scalar", Endpoint = "http" };

		// Act
		ResourceKitUrl.Apply([url], urls);

		// Assert
		await Assert.That(urls[^1].Url).IsEqualTo("http://localhost:5000/scalar");
	}

	[Test]
	public async Task Apply_GivenRelativeUrl_DoesNotChangeExistingEndpointUrls()
	{
		// Arrange
		var urls = CreateEndpointUrls();
		ResourceKitUrl url = new() { Url = "/health" };

		// Act
		ResourceKitUrl.Apply([url], urls);

		// Assert
		await Assert.That(urls[0].Url).IsEqualTo("http://localhost:5000");
		await Assert.That(urls[1].Url).IsEqualTo("https://localhost:5001");
	}

	[Test]
	public async Task Apply_GivenAbsoluteUrl_AddsItWithoutEndpoint()
	{
		// Arrange
		List<ResourceUrlAnnotation> urls = [];
		ResourceKitUrl url = new()
		{
			Url = "https://example.com/runbook",
			DisplayText = "Runbook",
			DisplayLocation = UrlDisplayLocation.DetailsOnly,
		};

		// Act
		var unresolved = ResourceKitUrl.Apply([url], urls);

		// Assert
		await Assert.That(unresolved).IsEmpty();
		await Assert.That(urls.Count).IsEqualTo(1);
		await Assert.That(urls[0].Url).IsEqualTo("https://example.com/runbook");
		await Assert.That(urls[0].Endpoint).IsNull();
		await Assert.That(urls[0].DisplayLocation).IsEqualTo(UrlDisplayLocation.DetailsOnly);
	}

	[Test]
	public async Task Apply_GivenEndpointUrlWithoutUrl_CustomisesTheEndpointsUrl()
	{
		// Arrange
		var urls = CreateEndpointUrls();
		ResourceKitUrl url = new()
		{
			Endpoint = "http",
			DisplayText = "API (HTTP)",
			DisplayLocation = UrlDisplayLocation.DetailsOnly,
		};

		// Act
		ResourceKitUrl.Apply([url], urls);

		// Assert
		await Assert.That(urls.Count).IsEqualTo(2);
		await Assert.That(urls[0].DisplayText).IsEqualTo("API (HTTP)");
		await Assert.That(urls[0].DisplayLocation).IsEqualTo(UrlDisplayLocation.DetailsOnly);
		await Assert.That(urls[1].DisplayText).IsNull();
	}

	[Test]
	public async Task Apply_GivenDisabledUrl_SkipsIt()
	{
		// Arrange
		var urls = CreateEndpointUrls();
		ResourceKitUrl url = new() { Url = "/health", IsEnabled = false };

		// Act
		var unresolved = ResourceKitUrl.Apply([url], urls);

		// Assert
		await Assert.That(unresolved).IsEmpty();
		await Assert.That(urls.Count).IsEqualTo(2);
	}

	[Test]
	public async Task Apply_GivenRelativeUrlAndNoEndpoints_ReturnsItAsUnresolved()
	{
		// Arrange
		List<ResourceUrlAnnotation> urls = [];
		ResourceKitUrl url = new() { Url = "/health" };

		// Act
		var unresolved = ResourceKitUrl.Apply([url], urls);

		// Assert
		await Assert.That(unresolved).Contains(url);
		await Assert.That(urls).IsEmpty();
	}

	[Test]
	public async Task Apply_GivenUnknownEndpoint_ReturnsItAsUnresolved()
	{
		// Arrange
		var urls = CreateEndpointUrls();
		ResourceKitUrl url = new() { Endpoint = "grpc", DisplayText = "gRPC" };

		// Act
		var unresolved = ResourceKitUrl.Apply([url], urls);

		// Assert
		await Assert.That(unresolved).Contains(url);
	}

	[Test]
	[Arguments("/health", false)]
	[Arguments("health", false)]
	[Arguments("https://example.com", true)]
	public async Task IsAbsolute_GivenUrl_ReportsWhetherItNeedsAnEndpoint(string value, bool expected)
	{
		// Arrange
		ResourceKitUrl url = new() { Url = value };

		// Act
		var isAbsolute = url.IsAbsolute;

		// Assert
		await Assert.That(isAbsolute).IsEqualTo(expected);
	}

	[Test]
	public async Task Build_GivenKitWithUrls_RegistersUrlsCallback()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		UrlResourceKit resource = new(hostKit, [new() { Url = "https://example.com", DisplayText = "Docs" }]);

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.Resource.Annotations.OfType<ResourceUrlsCallbackAnnotation>().Count()).IsEqualTo(1);
	}

	[Test]
	public async Task Build_GivenKitWithOnlyDisabledUrls_DoesNotRegisterUrlsCallback()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		UrlResourceKit resource = new(hostKit, [new() { Url = "https://example.com", IsEnabled = false }]);

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.Resource.Annotations.OfType<ResourceUrlsCallbackAnnotation>()).IsEmpty();
	}

	static List<ResourceUrlAnnotation> CreateEndpointUrls()
	{
		var builder = DistributedApplication.CreateBuilder();
		var container = builder
			.AddContainer("api", "image")
			.WithHttpEndpoint(port: 5000, name: "http")
			.WithHttpsEndpoint(port: 5001, name: "https");

		return
		[
			new() { Url = "http://localhost:5000", Endpoint = container.GetEndpoint("http") },
			new() { Url = "https://localhost:5001", Endpoint = container.GetEndpoint("https") },
		];
	}

	sealed class TestHostKit : HostKitBase<TestHostKit>;

	sealed class UrlResourceKit(TestHostKit hostKit, IEnumerable<ResourceKitUrl> urls)
		: ResourceKitBase<TestHostKit, ContainerResource>(hostKit, "api")
	{
		protected override IEnumerable<ResourceKitUrl> Urls => urls;

		protected override IResourceBuilder<ContainerResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		) => builder.AddContainer(Name, "image");
	}
}
