using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Release")]
[DependsOn<PackModule>]
[DependsOn<RunTestsModule>]
public class PublishNuGetModule(
	IOptions<BuildSettings> buildSettings,
	IOptions<NuGetSettings> nugetSettings,
	IOptions<ReleaseSettings> releaseSettings
) : Module<CommandResult[]>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				releaseSettings.Value.Mode != ReleaseMode.NuGet
					? SkipDecision.Skip(
						"NuGet publishing is disabled. Set Release__Mode=NuGet to publish packages to nuget.org."
					)
					: SkipDecision.DoNotSkip
			)
			.Build();

	protected override async Task<CommandResult[]?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var apiKey = nugetSettings.Value.GetNuGetAPIKey();
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			throw new InvalidOperationException(
				"NuGet publishing is enabled (Release__Mode=NuGet) but no API key is configured. "
					+ "Set NuGet__ApiKey (or NuGet__NUGET_APIKEY), or configure the NUGET__APIKEY secret in CI."
			);
		}

		var artifactsFolder = buildSettings.Value.ArtifactsFolder;
		if (!Directory.Exists(artifactsFolder))
		{
			throw new InvalidOperationException(
				$"The artifacts folder '{artifactsFolder}' does not exist. "
					+ "Ensure the pack step ran (Release__Mode must not be None) before publishing."
			);
		}

		var packages = Directory.EnumerateFiles(artifactsFolder, "*.nupkg", SearchOption.TopDirectoryOnly).ToList();

		if (packages.Count == 0)
		{
			throw new InvalidOperationException($"No NuGet packages found in {buildSettings.Value.ArtifactsFolder}.");
		}

		var tasks = packages.Select(package =>
			context
				.DotNet()
				.Nuget.Push(
					new()
					{
						Path = package,
						Source = nugetSettings.Value.FeedUrl,
						ApiKey = apiKey,
						SkipDuplicate = true,
					},
					cancellationToken: cancellationToken
				)
		);

		return await Task.WhenAll(tasks);
	}
}
