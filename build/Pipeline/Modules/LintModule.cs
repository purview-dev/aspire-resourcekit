using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Purview.Aspire.ResourceKit.Pipeline.Helpers;

namespace Purview.Aspire.ResourceKit.Pipeline.Modules;

[ModuleCategory("Build")]
public class LintModule : Module<CommandResult>
{
	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		await context.DotNet().Tool.Restore(new DotNetToolRestoreOptions(), cancellationToken: cancellationToken);

		return await context.Shell.Command.ExecuteCommandLineTool(
			DotNetCliOptions.Create("tool", "run", "csharpier", "check", "."),
			cancellationToken: cancellationToken
		);
	}
}
