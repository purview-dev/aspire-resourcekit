using ModularPipelines.Attributes;

namespace Purview.Aspire.ResourceKit.Pipeline.Settings;

public sealed record NuGetSettings
{
	public const string SectionName = "NuGet";

	[SecretValue]
	public string? ApiKey { get; init; }

	public string FeedUrl { get; init; } = "https://api.nuget.org/v3/index.json";
}
