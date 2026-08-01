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
public class RestoreModule(IOptions<BuildSettings> settings) : Module<CommandResult>
{
	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		return await context
			.DotNet()
			.Restore(
				new DotNetRestoreOptions { ProjectSolution = settings.Value.Solution },
				cancellationToken: cancellationToken
			);
	}
}
