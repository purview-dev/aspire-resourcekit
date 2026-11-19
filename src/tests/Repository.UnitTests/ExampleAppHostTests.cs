using System.Xml.Linq;

namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Holds the two example families to their build contracts. The Pre17 pair uses the legacy
/// <c>Aspire.AppHost.Sdk</c> and deliberately stays buildable without the Aspire CLI. The Aspire 17
/// pair uses <c>Microsoft.NET.Sdk</c> plus <c>Aspire.Hosting.AppHost</c>, which makes the CLI bundle a
/// hard requirement (ASPIRE013), so each pipeline must arrange for the pinned CLI before it builds.
/// </summary>
public sealed class ExampleAppHostTests
{
	static readonly string RepositoryRoot = RepositoryPaths.Root;

	[Test]
	[Arguments("src/src/Example.Pre17.AppHost/Example.Pre17.AppHost.csproj")]
	[Arguments("src/src/Example.Pre17.ManualAppHost/Example.Pre17.ManualAppHost.csproj")]
	public async Task Pre17AppHost_DoesNotOptIntoTheAspireCLIBundle(
		string relativeProjectPath,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var project = LoadProject(relativeProjectPath);

		// Act
		var optIns = project
			.Descendants()
			.Where(element => element.Name.LocalName == "AspireUseCliBundle")
			.Select(element => element.Value.Trim())
			.Where(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
			.ToArray();

		// Assert
		await Assert
			.That(optIns)
			.IsEmpty()
			.Because(
				$"'{relativeProjectPath}' is the pre-17, CLI-free example. It must keep "
					+ "AspireUseCliBundle=false so dotnet build does not need a machine-global Aspire CLI."
			);
	}

	[Test]
	[Arguments("src/src/Example.AppHost17/Example.AppHost17.csproj")]
	[Arguments("src/src/Example.ManualAppHost17/Example.ManualAppHost17.csproj")]
	public async Task Aspire17AppHost_UsesMicrosoftNetSdkAndTheAppHostPackages(
		string relativeProjectPath,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var project = LoadProject(relativeProjectPath);

		// Act
		var sdk = (string?)project.Root?.Attribute("Sdk");
		var packageIds = PackageIds(project);
		var disablesBundle = project
			.Descendants()
			.Where(element => element.Name.LocalName == "AspireUseCliBundle")
			.Any(element => string.Equals(element.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase));

		// Assert
		await Assert
			.That(sdk)
			.IsEqualTo("Microsoft.NET.Sdk")
			.Because("Aspire 17 AppHosts use Microsoft.NET.Sdk, not the obsolete Aspire.AppHost.Sdk.");
		await Assert.That(packageIds).Contains("Aspire.Hosting.AppHost");
		await Assert.That(packageIds).Contains("Aspire.Hosting.Dotnet");
		await Assert
			.That(disablesBundle)
			.IsFalse()
			.Because(
				$"'{relativeProjectPath}' is an Aspire 17 C# AppHost. ASPIRE013 cannot be suppressed, and "
					+ "setting AspireUseCliBundle=false would make it unbuildable."
			);
	}

	[Test]
	public async Task AspireCLIVersion_MatchesTheAspire17PackageVersion(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var cliVersionPath = Path.Combine(RepositoryRoot, ".config", "aspire-version");
		var packagesPath = Path.Combine(RepositoryRoot, "Directory.Packages.props");

		// Act
		var cliVersion = (await File.ReadAllTextAsync(cliVersionPath, cancellationToken)).Trim();
		var packages = XDocument.Load(packagesPath);
		var aspire17Version = packages
			.Descendants()
			.FirstOrDefault(element => element.Name.LocalName == "Aspire17Version")
			?.Value.Trim();

		// Assert
		await Assert
			.That(cliVersion)
			.IsEqualTo(aspire17Version)
			.Because(
				"the CLI installs from .config/aspire-version, and the 17 AppHosts resolve "
					+ "Aspire.Hosting.AppHost at Directory.Packages.props' Aspire17Version. A mismatch builds "
					+ "against a bundle the AppHost did not select."
			);
	}

	[Test]
	public async Task Workflows_GivenTheAspire17Examples_EnableThePinnedAspireCLIBeforeBuilding(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var pullRequest = ReadWorkflow("pr.yml");
		var release = ReadWorkflow("release.yml");

		// Act
		// PR validation delegates to the shared workflow, so the CLI is that workflow's job and this
		// file's only obligation is to enable it. Release cannot delegate to a reusable workflow -
		// nuget.org's Trusted Publishing policy checks the job_workflow_ref claim against this file -
		// so it installs the CLI itself.
		var pullRequestEnablesSharedCli = pullRequest.Any(static line =>
			line.Trim().Contains("aspire-cli: true", StringComparison.Ordinal)
		);
		var releaseInstallsPinnedCli =
			release.Any(static line => line.Contains("Aspire.Cli", StringComparison.Ordinal))
			&& release.Any(static line => line.Contains(".config/aspire-version", StringComparison.Ordinal))
			&& release.Any(static line =>
				line.Contains("aspire", StringComparison.OrdinalIgnoreCase)
				&& line.Contains("setup", StringComparison.Ordinal)
			);

		// Assert
		await Assert
			.That(pullRequestEnablesSharedCli)
			.IsTrue()
			.Because(
				"the PR pipeline calls the shared workflow, whose `aspire-cli` input is what installs "
					+ "Aspire.Cli from .config/aspire-version and runs `aspire setup` before the build: "
					+ "ASPIRE013 cannot be suppressed, so leaving it off fails the build instead."
			);
		await Assert
			.That(releaseInstallsPinnedCli)
			.IsTrue()
			.Because(
				"the release pipeline builds the Aspire 17 examples through the purview-release composite "
					+ "action and must install Aspire.Cli from .config/aspire-version, then run `aspire setup`, "
					+ "before it runs: ASPIRE013 cannot be suppressed."
			);
	}

	static string[] ReadWorkflow(string fileName) =>
		File.ReadAllLines(Path.Combine(RepositoryRoot, ".github", "workflows", fileName));

	[Test]
	public async Task RepositoryRoot_ResolvesToThisRepository(CancellationToken cancellationToken)
	{
		// Arrange
		// Every sibling assertion is vacuous if the marker walk landed somewhere the AppHosts are not.
		cancellationToken.ThrowIfCancellationRequested();

		// Act
		var solution = Path.Combine(RepositoryRoot, "src", "ResourceKit.slnx");

		// Assert
		await Assert
			.That(File.Exists(solution))
			.IsTrue()
			.Because($"the marker walk resolved '{RepositoryRoot}', which does not look like this repository");
	}

	static XDocument LoadProject(string relativeProjectPath) =>
		XDocument.Load(Path.Combine(RepositoryRoot, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar)));

	static string[] PackageIds(XDocument project) =>
		[
			.. project
				.Descendants()
				.Where(element => element.Name.LocalName == "PackageReference")
				.Select(element => (string?)element.Attribute("Include"))
				.Where(static id => id is not null)
				.Select(static id => id!),
		];
}
