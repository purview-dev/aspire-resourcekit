namespace Purview.Aspire.ResourceKit.Pipeline.Settings;

public sealed record ReleaseSettings
{
	public const string SectionName = "Release";

	public bool ShouldPublish { get; init; }
}
