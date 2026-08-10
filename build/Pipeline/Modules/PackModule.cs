using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Purview.Aspire.ResourceKit.Pipeline.Settings;

namespace Purview.Aspire.ResourceKit.Pipeline.Modules;

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
		var version =
			versionResult.ValueOrDefault
			?? throw new InvalidOperationException(
				"The version was not produced by the version module."
			);

		Directory.CreateDirectory(settings.Value.ArtifactsFolder);

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
