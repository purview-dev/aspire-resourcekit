using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Describes a named URL shown for a resource, or a resource kit group, in the Aspire dashboard.
/// </summary>
/// <remarks>
/// <para>
/// A URL with a <see cref="Url"/> adds a link. An absolute URL is shown as-is; a relative URL is resolved
/// against the <see cref="Endpoint"/> it names, or - when no endpoint is named - against the resource's
/// <c>https</c> endpoint, then its <c>http</c> endpoint, then its first endpoint.
/// </para>
/// <para>
/// A URL without a <see cref="Url"/> customises the URL Aspire already shows for <see cref="Endpoint"/>,
/// changing its <see cref="DisplayText"/> and <see cref="DisplayLocation"/>.
/// </para>
/// <para>
/// The type is mutable so generated options can bind it from configuration, for example
/// <c>ExampleHostKit:API:Urls:Health:IsEnabled=false</c>.
/// </para>
/// </remarks>
public sealed class ResourceKitUrl
{
	/// <summary>
	/// Gets or sets the absolute or relative URL to add, or <see langword="null"/> to customise the URL of
	/// <see cref="Endpoint"/>.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Design",
		"CA1056:URI-like properties should not be strings",
		Justification = "A relative path such as '/health' is not a Uri, and the value binds from configuration; it mirrors ResourceUrlAnnotation.Url."
	)]
	public string? Url { get; set; }

	/// <summary>
	/// Gets or sets the text the dashboard shows for the URL.
	/// </summary>
	public string? DisplayText { get; set; }

	/// <summary>
	/// Gets or sets the name of the endpoint a relative URL is resolved against, or whose URL is customised.
	/// </summary>
	public string? Endpoint { get; set; }

	/// <summary>
	/// Gets or sets where the dashboard shows the URL.
	/// </summary>
	public UrlDisplayLocation DisplayLocation { get; set; } = UrlDisplayLocation.SummaryAndDetails;

	/// <summary>
	/// Gets or sets a value indicating whether the URL is applied.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets a value indicating whether <see cref="Url"/> is absolute, and therefore needs no endpoint.
	/// </summary>
	public bool IsAbsolute => TryGetAbsoluteUri(Url, out _);

	/// <summary>
	/// Applies <paramref name="urls"/> to the URLs Aspire has computed for a resource.
	/// </summary>
	/// <param name="urls">The URLs to apply. Disabled URLs are skipped.</param>
	/// <param name="resourceUrls">The resource's URLs, as exposed by <see cref="ResourceUrlsCallbackContext.Urls"/>.</param>
	/// <returns>The URLs that could not be applied because no matching endpoint URL exists.</returns>
	public static IReadOnlyList<ResourceKitUrl> Apply(
		IEnumerable<ResourceKitUrl> urls,
		IList<ResourceUrlAnnotation> resourceUrls
	)
	{
		ArgumentNullException.ThrowIfNull(urls);
		ArgumentNullException.ThrowIfNull(resourceUrls);

		// Resolve against the endpoint URLs as they were before this call, so one added URL is never the
		// base of another.
		ResourceUrlAnnotation[] endpointUrls = [.. resourceUrls.Where(static u => u.Endpoint is not null)];
		List<ResourceKitUrl> unresolved = [];

		foreach (var url in urls)
		{
			if (!url.IsEnabled)
				continue;

			if (url.Url is null)
			{
				var matched = false;
				foreach (var endpointUrl in endpointUrls.Where(u => IsEndpoint(u, url.Endpoint)))
				{
					matched = true;
					if (url.DisplayText is not null)
						endpointUrl.DisplayText = url.DisplayText;

					endpointUrl.DisplayLocation = url.DisplayLocation;
				}

				if (!matched)
					unresolved.Add(url);

				continue;
			}

			if (TryGetAbsoluteUri(url.Url, out var absolute))
			{
				resourceUrls.Add(
					new()
					{
						Url = absolute.ToString(),
						DisplayText = url.DisplayText,
						DisplayLocation = url.DisplayLocation,
					}
				);
				continue;
			}

			var baseUrl = SelectBaseUrl(endpointUrls, url.Endpoint);
			if (baseUrl is null || !Uri.TryCreate(baseUrl.Url, UriKind.Absolute, out var baseUri))
			{
				unresolved.Add(url);
				continue;
			}

			resourceUrls.Add(
				new()
				{
					Url = new Uri(baseUri, url.Url).ToString(),
					DisplayText = url.DisplayText,
					Endpoint = baseUrl.Endpoint,
					DisplayLocation = url.DisplayLocation,
				}
			);
		}

		return unresolved;
	}

	static ResourceUrlAnnotation? SelectBaseUrl(ResourceUrlAnnotation[] endpointUrls, string? endpoint)
	{
		if (endpoint is not null)
			return endpointUrls.FirstOrDefault(u => IsEndpoint(u, endpoint));

		// Prefer the https endpoint, then http, then any.
		return endpointUrls.FirstOrDefault(static u => IsEndpoint(u, "https"))
			?? endpointUrls.FirstOrDefault(static u => IsEndpoint(u, "http"))
			?? endpointUrls.FirstOrDefault();
	}

	static bool IsEndpoint(ResourceUrlAnnotation url, string? endpoint) =>
		endpoint is not null && string.Equals(url.Endpoint?.EndpointName, endpoint, StringComparison.OrdinalIgnoreCase);

	// A path such as "/health" parses as an absolute file URI on Unix, so only accept a URI with a host.
	static bool TryGetAbsoluteUri(string? url, out Uri uri)
	{
		if (url is not null && Uri.TryCreate(url, UriKind.Absolute, out var parsed) && !parsed.IsFile)
		{
			uri = parsed;
			return true;
		}

		uri = default!;
		return false;
	}
}
