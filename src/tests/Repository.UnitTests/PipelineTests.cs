using System.Text.Json;

namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Holds both shared pipelines to running the whole suite: every discovered test project, on every
/// target framework, in PR validation and in the release.
/// </summary>
/// <remarks>
/// The repository previously ran only the unit tier in CI. Turning that back on is a one-line edit to
/// <c>purview-build.json</c> or a <c>test-filter</c> argument in a workflow, and both are easy to add
/// while debugging a slow or failing suite. The integration tests are the suite most likely to be
/// dropped this way - they start Docker containers and real AppHosts - so the contract belongs in a
/// failing test rather than in a comment.
/// </remarks>
public sealed class PipelineTests
{
	static readonly string RepositoryRoot = RepositoryPaths.Root;

	static readonly string[] WorkflowPaths = [".github/workflows/pr.yml", ".github/workflows/release.yml"];

	[Test]
	public async Task PurviewBuildConfig_GivenTestFilter_RunsEveryDiscoveredProject(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var configPath = Path.Combine(RepositoryRoot, ".config", "purview-build.json");
		using var document = JsonDocument.Parse(
			await File.ReadAllTextAsync(configPath, cancellationToken),
			new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }
		);
		var build = document.RootElement.GetProperty("Build");

		// Act
		var filter = build.GetProperty("TestFilter").GetString();
		var patterns = build.GetProperty("TestPatterns").GetString();
		var projects = build.TryGetProperty("TestProjects", out var projectsElement)
			? projectsElement.GetString()
			: null;
		var runTests = !build.TryGetProperty("RunTests", out var runTestsElement) || runTestsElement.GetBoolean();

		// Assert
		await Assert
			.That(filter)
			.IsEqualTo("/*/*/*/*")
			.Because(
				"PR validation and the release must run every test project, including the AppHost "
					+ "integration tests; a category filter such as '[Category=Unit]' silently removes them."
			);
		await Assert
			.That(patterns)
			.IsEqualTo("*Tests.csproj")
			.Because("the integration test projects match the same pattern as the unit suites");
		await Assert
			.That(projects is null or "*")
			.IsTrue()
			.Because("an explicit project list would stop a newly added test project from running");
		await Assert
			.That(runTests)
			.IsTrue()
			.Because("Build:RunTests=false would make the pipeline succeed without running anything");
	}

	[Test]
	public async Task Workflows_GivenSharedPipelineCalls_DoNotNarrowOrDisableTests(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var workflows = WorkflowPaths
			.Select(relative => Path.Combine(RepositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar)))
			.Select(path => (Path: path, Lines: File.ReadAllLines(path)))
			.ToArray();

		// Act
		var narrowed = workflows
			.SelectMany(workflow =>
				workflow
					.Lines.Select(static line => line.Trim())
					.Where(static line =>
						line.Contains("[Category=Unit]", StringComparison.Ordinal)
						|| line.StartsWith("test-filter:", StringComparison.Ordinal)
						|| line.StartsWith("run-tests: false", StringComparison.OrdinalIgnoreCase)
					)
					.Select(line => $"{Path.GetRelativePath(RepositoryRoot, workflow.Path)}: {line}")
			)
			.ToArray();
		var withoutTestsEnabled = workflows
			.Where(workflow =>
				!workflow.Lines.Any(static line =>
					string.Equals(line.Trim(), "run-tests: true", StringComparison.Ordinal)
				)
			)
			.Select(workflow => Path.GetRelativePath(RepositoryRoot, workflow.Path))
			.ToArray();

		// Assert
		await Assert
			.That(narrowed)
			.IsEmpty()
			.Because(
				"the test filter belongs in .config/purview-build.json so both pipelines run the same "
					+ "full suite; a workflow-level filter or RunTests=false makes one pipeline weaker."
			);
		await Assert
			.That(withoutTestsEnabled)
			.IsEmpty()
			.Because(
				"both pipelines should state that they run tests, rather than depending on a shared "
					+ "workflow or action default that could change."
			);
	}
}
