using ModularPipelines.Attributes;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed record NuGetSettings
{
	public const string SectionName = "NuGet";

	[SecretValue]
	public string? APIKey { get; init; }

	public string FeedUrl { get; init; } = "https://api.nuget.org/v3/index.json";
}
