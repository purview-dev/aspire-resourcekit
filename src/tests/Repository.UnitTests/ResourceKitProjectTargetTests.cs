using System.Diagnostics;

namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Holds the <c>ResourceKitProject</c> build target to what it promises a consumer: typed, relative
/// project paths emitted into the compilation, and a build failure when a declared path is wrong.
/// </summary>
/// <remarks>
/// This is deliberately an MSBuild test rather than a source-generator test. The paths come from items
/// the AppHost declares, which no Roslyn generator can see; the generated class has to exist before the
/// generator runs so SG0018 can resolve a <c>ProjectPath = AppHostProjects.X.Path</c> constant.
/// </remarks>
public sealed class ResourceKitProjectTargetTests
{
	static readonly string TargetsPath = Path.Combine(
		RepositoryPaths.Root,
		"src",
		"src",
		"ResourceKit",
		"Sdk",
		"buildTransitive",
		"Purview.Aspire.ResourceKit.targets"
	);

	[Test]
	public async Task AppHostProjects_GivenProjectAndVitePaths_GeneratesTypedRelativePaths(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture();

		// Act
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken);

		// Assert
		await Assert.That(result.ExitCode).IsEqualTo(0).Because(result.Output);
		var generated = await File.ReadAllTextAsync(
			Path.Combine(fixture.AppHostProjectDirectory, ReadGeneratedFile(fixture.AppHostProjectDirectory)),
			cancellationToken
		);

		await Assert.That(generated.Contains("namespace Fixture.AppHost;", StringComparison.Ordinal)).IsTrue();
		await Assert.That(generated.Contains("internal static class Api", StringComparison.Ordinal)).IsTrue();
		await Assert.That(generated.Contains("Name = \"api\";", StringComparison.Ordinal)).IsTrue();
		await Assert
			.That(generated.Contains("Path = \"../dotnet-project/Service.csproj\";", StringComparison.Ordinal))
			.IsTrue()
			.Because("a project file keeps the declared relative path with '/' separators");
		await Assert
			.That(generated.Contains("internal static class ViteApp", StringComparison.Ordinal))
			.IsTrue()
			.Because("a Vite directory with no Name or ClassName derives ViteApp from 'vite-app'");
		await Assert
			.That(generated.Contains("Path = \"../web/packages/vite-app\";", StringComparison.Ordinal))
			.IsTrue();
	}

	[Test]
	public async Task AppHostProjects_GivenMissingPath_FailsWithRkp0001(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture(includeMissingPath: true);

		// Act
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken);

		// Assert
		await Assert.That(result.ExitCode).IsNotEqualTo(0);
		await Assert
			.That(result.Output.Contains("RKP0001", StringComparison.Ordinal))
			.IsTrue()
			.Because("a wrong '../..' depth is the mistake the typed class exists to prevent");
	}

	[Test]
	public async Task AppHostProjects_GivenOptionalMissingPath_DoesNotFail(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture(includeMissingPath: true, optionalMissingPath: true);

		// Act
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken);

		// Assert
		await Assert.That(result.ExitCode).IsEqualTo(0).Because(result.Output);
	}

	[Test]
	public async Task AppHostProjects_GivenInvalidResourceName_FailsWithRkp0003(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture(invalidResourceName: true);

		// Act
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken);

		// Assert
		await Assert.That(result.ExitCode).IsNotEqualTo(0);
		await Assert.That(result.Output.Contains("RKP0003", StringComparison.Ordinal)).IsTrue();
	}

	[Test]
	public async Task AppHostProjects_GivenResourceKitPath_GeneratesTypedPath(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture(includeResourceKitPath: true);

		// Act
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken);

		// Assert
		await Assert.That(result.ExitCode).IsEqualTo(0).Because(result.Output);
		var generated = await File.ReadAllTextAsync(
			Path.Combine(fixture.AppHostProjectDirectory, ReadGeneratedFile(fixture.AppHostProjectDirectory)),
			cancellationToken
		);

		await Assert
			.That(generated.Contains("internal static class DeployFile", StringComparison.Ordinal))
			.IsTrue()
			.Because("ResourceKitPath is not limited to projects and app directories");
		await Assert.That(generated.Contains("Path = \"../deploy/Dockerfile\";", StringComparison.Ordinal)).IsTrue();
	}

	[Test]
	public async Task AppHostProjects_GivenKindMismatch_FailsWithRkp0005(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture(kindMismatch: true);

		// Act
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken);

		// Assert
		await Assert.That(result.ExitCode).IsNotEqualTo(0);
		await Assert.That(result.Output.Contains("RKP0005", StringComparison.Ordinal)).IsTrue();
	}

	[Test]
	public async Task AppHostProjects_GivenInvalidKind_FailsWithRkp0006(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture(invalidKind: true);

		// Act
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken);

		// Assert
		await Assert.That(result.ExitCode).IsNotEqualTo(0);
		await Assert.That(result.Output.Contains("RKP0006", StringComparison.Ordinal)).IsTrue();
	}

	[Test]
	public async Task AppHostProjects_GivenDuplicateClassNames_FailsWithRkp0004(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture(duplicateNames: true);

		// Act
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken);

		// Assert
		await Assert.That(result.ExitCode).IsNotEqualTo(0);
		await Assert.That(result.Output.Contains("RKP0004", StringComparison.Ordinal)).IsTrue();
	}

	[Test]
	public async Task AppHostProjects_GivenNonCSharpProject_FailsWithRkp0007(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using var fixture = CreateFixture();

		// Act — a VB/F# AppHost would otherwise receive a .cs file it cannot compile.
		var result = await RunMsBuildAsync(fixture.AppHostProject, cancellationToken, language: "VB");

		// Assert
		await Assert.That(result.ExitCode).IsNotEqualTo(0);
		await Assert.That(result.Output.Contains("RKP0007", StringComparison.Ordinal)).IsTrue();
	}

	static Fixture CreateFixture(
		bool includeMissingPath = false,
		bool optionalMissingPath = false,
		bool invalidResourceName = false,
		bool includeResourceKitPath = false,
		bool kindMismatch = false,
		bool invalidKind = false,
		bool duplicateNames = false
	)
	{
		var root = Path.Combine(Path.GetTempPath(), "resourcekit-projects", Guid.NewGuid().ToString("N"));
		var appHostDirectory = Path.Combine(root, "apphost");
		var serviceDirectory = Path.Combine(root, "dotnet-project");
		var viteDirectory = Path.Combine(root, "web", "packages", "vite-app");
		var deployDirectory = Path.Combine(root, "deploy");
		Directory.CreateDirectory(appHostDirectory);
		Directory.CreateDirectory(serviceDirectory);
		Directory.CreateDirectory(viteDirectory);
		Directory.CreateDirectory(deployDirectory);
		File.WriteAllText(Path.Combine(serviceDirectory, "Service.csproj"), "<Project />");
		File.WriteAllText(Path.Combine(viteDirectory, "package.json"), "{ \"name\": \"vite-app\" }");
		File.WriteAllText(Path.Combine(deployDirectory, "Dockerfile"), "FROM scratch");
		File.WriteAllText(Path.Combine(serviceDirectory, "notes.txt"), "notes");

		List<string> items =
		[
			"<ResourceKitProject Include=\"..\\dotnet-project\\Service.csproj\" Name=\"api\" ClassName=\"Api\" />",
			"<ResourceKitProject Include=\"..\\web\\packages\\vite-app\" />",
		];
		if (includeResourceKitPath)
		{
			items.Add(
				"<ResourceKitPath Include=\"..\\deploy\\Dockerfile\" Name=\"deploy\" ClassName=\"DeployFile\" Kind=\"File\" />"
			);
		}
		if (kindMismatch)
		{
			items.Add("<ResourceKitPath Include=\"..\\web\\packages\\vite-app\" Name=\"wrong\" Kind=\"File\" />");
		}
		if (invalidKind)
		{
			items.Add("<ResourceKitPath Include=\"..\\web\\packages\\vite-app\" Name=\"wrong\" Kind=\"Bogus\" />");
		}
		if (duplicateNames)
		{
			items.Add("<ResourceKitPath Include=\"..\\dotnet-project\\notes.txt\" Name=\"api\" ClassName=\"Api\" />");
		}
		if (includeMissingPath)
		{
			items.Add(
				optionalMissingPath
					? "<ResourceKitProject Include=\"..\\missing\" Name=\"missing\" Optional=\"true\" />"
					: "<ResourceKitProject Include=\"..\\missing\" Name=\"missing\" />"
			);
		}
		if (invalidResourceName)
		{
			items.Add("<ResourceKitProject Include=\"..\\web\\packages\\vite-app\" Name=\"not valid\" />");
		}

		var project = $"""
			<Project Sdk="Microsoft.NET.Sdk">
				<PropertyGroup>
					<TargetFramework>net10.0</TargetFramework>
					<RootNamespace>Fixture.AppHost</RootNamespace>
				</PropertyGroup>
				<ItemGroup>
					{string.Join(Environment.NewLine, items.Select(item => "\t\t" + item))}
				</ItemGroup>
				<Import Project="{TargetsPath}" />
			</Project>
			""";
		var projectPath = Path.Combine(appHostDirectory, "Fixture.AppHost.csproj");
		File.WriteAllText(projectPath, project);

		return new Fixture(root, appHostDirectory, projectPath);
	}

	static string ReadGeneratedFile(string appHostDirectory) =>
		Path.GetRelativePath(
			appHostDirectory,
			Directory.EnumerateFiles(appHostDirectory, "AppHostProjects.g.cs", SearchOption.AllDirectories).Single()
		);

	static async Task<(int ExitCode, string Output)> RunMsBuildAsync(
		string projectPath,
		CancellationToken cancellationToken,
		string? language = null
	)
	{
		using Process process = new()
		{
			StartInfo = new ProcessStartInfo("dotnet")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			},
		};
		process.StartInfo.ArgumentList.Add("msbuild");
		process.StartInfo.ArgumentList.Add(projectPath);
		process.StartInfo.ArgumentList.Add("-t:_ResourceKitGenerateAppHostProjects");
		process.StartInfo.ArgumentList.Add("-nologo");
		process.StartInfo.ArgumentList.Add("-v:q");
		if (language is not null)
		{
			process.StartInfo.ArgumentList.Add($"-p:Language={language}");
		}

		process.Start();
		var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
		var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
		await process.WaitForExitAsync(cancellationToken);

		return (process.ExitCode, await standardOutput + await standardError);
	}

	sealed class Fixture(string root, string appHostProjectDirectory, string appHostProject) : IDisposable
	{
		public string AppHostProjectDirectory { get; } = appHostProjectDirectory;

		public string AppHostProject { get; } = appHostProject;

		public void Dispose()
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}
}
