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
[DependsOn<RestoreModule>]
public class BuildModule(IOptions<BuildSettings> settings) : Module<CommandResult>
{
	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		return await context
			.DotNet()
			.Build(
				new DotNetBuildOptions
				{
					ProjectSolution = settings.Value.Solution,
					Configuration = settings.Value.Configuration,
					NoRestore = true,
				},
				cancellationToken: cancellationToken
			);
	}
}
