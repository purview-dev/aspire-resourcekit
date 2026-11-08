using System.Xml.Linq;

namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Holds the example AppHosts to the one build contract the shared pipelines cannot check for
/// themselves: that <c>dotnet build</c> on this repository needs no machine-global Aspire CLI.
/// </summary>
/// <remarks>
/// <para>
/// Opting an AppHost into the Aspire CLI bundle (<c>AspireUseCliBundle=true</c>) makes a <em>build</em>
/// require a resolvable bundle. Where none exists, <c>Aspire.Hosting.AppHost.targets</c> runs
/// <c>dnx aspire.cli@&lt;version&gt; -- setup</c> mid-build - a network install inside MSBuild - and
/// reports only ASPIRE009 when it fails, which is how the release pipeline broke. Both pipelines invoke
/// <c>dotnet build</c> themselves and cannot install the CLI first, so the opt-in staying out of the
/// project files is the fix rather than an environment step.
/// </para>
/// <para>
/// A repository test rather than an assertion inside the AppHost, which would be asserting a property it
/// had just set. The cost of the choice - ASPIRE010 - is suppressed in the project for the same reason it
/// is made there, so this suite guards the decision, not the suppression.
/// </para>
/// </remarks>
public sealed class ExampleAppHostTests
{
	static readonly string RepositoryRoot = FindRepositoryRoot();

	[Test]
	[Arguments("src/src/Example.AppHost/Example.AppHost.csproj")]
	[Arguments("src/src/Example.ManualAppHost/Example.ManualAppHost.csproj")]
	public async Task AppHost_DoesNotOptIntoTheAspireCLIBundle(
		string relativeProjectPath,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var projectPath = Path.Combine(RepositoryRoot, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));

		// Act
		var optIns = XDocument
			.Load(projectPath)
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
				$"'{relativeProjectPath}' opts into the Aspire CLI bundle, which makes 'dotnet build' need a "
					+ "machine-global Aspire CLI: with no bundle, MSBuild runs 'dnx ... aspire setup' mid-build "
					+ "and fails with ASPIRE009 when that setup fails, which is what a clean Linux runner does. "
					+ "Pass -p:AspireUseCliBundle=true for a one-off build instead of pinning it in the project."
			);
	}

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

	static string FindRepositoryRoot()
	{
		// The same '.git/' marker walk Purview.BuildSdk uses to find the repository root.
		for (
			var directory = new DirectoryInfo(AppContext.BaseDirectory);
			directory is not null;
			directory = directory.Parent
		)
		{
			if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
			{
				return directory.FullName;
			}
		}

		throw new InvalidOperationException(
			$"No '.git' directory was found above '{AppContext.BaseDirectory}', so the repository root is unresolvable."
		);
	}
}
