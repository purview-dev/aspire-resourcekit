using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
[DependsOn<BuildModule>]
[DependsOn<VersionModule>]
public class PackModule(IOptions<BuildSettings> settings) : Module<CommandResult>
{
	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var versionResult = await context.GetModule<VersionModule>();
		var nugetVersion =
			versionResult.ValueOrDefault
			?? throw new InvalidOperationException("The version was not produced by the version module.");

		Directory.CreateDirectory(settings.Value.ArtifactsFolder);

		var version = nugetVersion.ToString();
		return await context
			.DotNet()
			.Pack(
				new DotNetPackOptions
				{
					ProjectSolution = settings.Value.Solution,
					Configuration = settings.Value.Configuration,
					Output = settings.Value.ArtifactsFolder,
					Properties = [("PackageVersion", version), ("Version", version)],
				},
				cancellationToken: cancellationToken
			);
	}
}
