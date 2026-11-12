namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Resolves the repository root once for the repository-level guardrail tests.
/// </summary>
static class RepositoryPaths
{
	public static string Root { get; } = FindRepositoryRoot();

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
