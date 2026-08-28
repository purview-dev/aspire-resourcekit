namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed record ReleaseSettings
{
	public const string SectionName = "Release";

	public bool ShouldPublish { get; init; }
}
