namespace Purview.Aspire.ResourceKit.Pipeline.Settings;

public sealed class BuildSettings
{
	public const string SectionName = "Build";

	public string Solution { get; init; } = "src/ResourceKit.slnx";

	public string Configuration { get; init; } = "Release";

	public string ArtifactsFolder { get; init; } = "artifacts";

	public bool RunTests { get; init; } = true;
}
