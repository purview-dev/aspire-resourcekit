using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed class BuildSettings
{
	public const string SectionName = "Build";

	[Required(AllowEmptyStrings = false)]
	public string Solution { get; init; } = "src/ResourceKit.slnx";

	[Required(AllowEmptyStrings = false)]
	public string Configuration { get; init; } = "Release";

	[Required(AllowEmptyStrings = false)]
	public string ArtifactsFolder { get; init; } = "artifacts";

	public bool RunTests { get; init; } = true;

	public bool RunIntegrationTests { get; init; }

	[Required(AllowEmptyStrings = false)]
	public string IntegrationTestFilter { get; init; } = "/*/*/*/*[Category=Integration]";

	[Required(AllowEmptyStrings = false)]
	public string UnitTestFilter { get; init; } = "/*/*/*/*[Category=Unit]";
}
