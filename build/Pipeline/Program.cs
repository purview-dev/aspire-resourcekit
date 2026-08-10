using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines;
using ModularPipelines.Extensions;
using Octokit;
using Octokit.Internal;
using Purview.Aspire.ResourceKit.Pipeline;
using Purview.Aspire.ResourceKit.Pipeline.Modules;
using Purview.Aspire.ResourceKit.Pipeline.Settings;

var pipelineDirectory = PipelineProjectDirectory.Find();
var repositoryRoot = FindRepositoryRoot(pipelineDirectory);

static string FindRepositoryRoot(string startDirectory)
{
	var directory = new DirectoryInfo(startDirectory);
	while (directory is not null)
	{
		if (File.Exists(Path.Combine(directory.FullName, "package.json")))
		{
			return directory.FullName;
		}

		directory = directory.Parent;
	}

	throw new InvalidOperationException(
		"Could not locate the repository root (no package.json found)."
	);
}

var builder = Pipeline.CreateBuilder(args);

builder
	.Configuration.AddJsonFile(Path.Combine(pipelineDirectory, "appsettings.json"), optional: false)
	.AddEnvironmentVariables();

builder.Services.Configure<BuildSettings>(
	builder.Configuration.GetSection(BuildSettings.SectionName)
);
builder.Services.Configure<NuGetSettings>(
	builder.Configuration.GetSection(NuGetSettings.SectionName)
);
builder.Services.Configure<GitHubSettings>(
	builder.Configuration.GetSection(GitHubSettings.SectionName)
);
builder.Services.Configure<ReleaseSettings>(
	builder.Configuration.GetSection(ReleaseSettings.SectionName)
);

builder.Services.AddSingleton<IGitHubClient>(serviceProvider =>
{
	var settings = serviceProvider.GetRequiredService<IOptions<GitHubSettings>>();
	var accessToken =
		settings.Value.AccessToken ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? "token";

	return new GitHubClient(
		new ProductHeaderValue(settings.Value.ProductHeader),
		new InMemoryCredentialStore(new Credentials(accessToken))
	);
});

Environment.CurrentDirectory = repositoryRoot;

builder
	.AddModule<VersionModule>()
	.AddModule<RestoreModule>()
	.AddModule<BuildModule>()
	.AddModule<LintModule>()
	.AddModule<RunTestsModule>()
	.AddModule<PackModule>()
	.AddModule<PublishNuGetModule>()
	.AddModule<CreateGitHubReleaseModule>();

builder.SetLogLevel(LogLevel.Information);

await using var pipeline = await builder.BuildAsync();
await pipeline.RunAsync();
