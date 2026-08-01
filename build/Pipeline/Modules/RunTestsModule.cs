using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Purview.Aspire.ResourceKit.Pipeline.Helpers;
using Purview.Aspire.ResourceKit.Pipeline.Settings;

namespace Purview.Aspire.ResourceKit.Pipeline.Modules;

[ModuleCategory("Build")]
[DependsOn<BuildModule>]
public class RunTestsModule(IOptions<BuildSettings> settings) : Module<CommandResult[]>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				settings.Value.RunTests
					? SkipDecision.DoNotSkip
					: SkipDecision.Skip("Tests are disabled. Set Build__RunTests=true to run them.")
			)
			.Build();

	protected override async Task<CommandResult[]?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var testProjects = Directory
			.EnumerateFiles("src/tests", "*.Tests.csproj", SearchOption.AllDirectories)
			.ToList();

		if (testProjects.Count == 0)
		{
			return [];
		}

		var testFilter = TestHelpers.BuildFilter(
			assembly: context.Environment.BuildSystem.IsBuildServer ? "*UnitTests" : null
		);

		var tasks = testProjects.Select(project =>
			context
				.DotNet()
				.Test(
					new DotNetTestOptions
					{
						Project = project,
						Configuration = settings.Value.Configuration,
						NoBuild = true,
						NoRestore = true,
						Arguments = ["--ignore-exit-code", "8", "--treenode-filter", testFilter],
					},
					cancellationToken: cancellationToken
				)
		);

		return await Task.WhenAll(tasks);
	}
}
