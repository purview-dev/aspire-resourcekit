using ModularPipelines.Attributes;

namespace Purview.Aspire.ResourceKit.Pipeline.Settings;

public sealed record GitHubSettings
{
	public const string SectionName = "GitHub";

	[SecretValue]
	public string? AccessToken { get; init; }

	public string ProductHeader { get; init; } = "Purview.Aspire.ResourceKit.Pipeline";
}
