using System.Xml.Linq;

namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Holds the repository to the two package-floor rules that are easy to break silently: no
/// out-of-support target framework may be reintroduced, and a <c>Microsoft.Extensions.*</c> package
/// must stay on the servicing band of the lowest supported framework.
/// </summary>
/// <remarks>
/// A higher-band <c>Microsoft.Extensions.*</c> reference is not a compile error in this repository -
/// it compiles because the .NET 11 SDK and runtime are installed - but it raises the dependency floor
/// a .NET 10 consumer inherits from the published package. The failure belongs here, not in a
/// consumer's restore.
/// </remarks>
public sealed class PackageToolchainTests
{
	static readonly string[] TargetFrameworkDeclarationNames =
	[
		"DefaultTargetFramework",
		"TargetFramework",
		"TargetFrameworks",
	];

	[Test]
	public async Task ShippedTargetFrameworks_GivenRepository_DeclareTheFloorAndNewestLeg(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var versions = XDocument.Load(Path.Combine(RepositoryPaths.Root, "src", "Versions.props"));
		var declarations = versions
			.Descendants()
			.Where(element => element.Name.LocalName is "DefaultTargetFramework" or "LatestTargetFramework")
			.ToDictionary(element => element.Name.LocalName, element => element.Value.Trim(), StringComparer.Ordinal);

		// Act
		var floor = declarations["DefaultTargetFramework"];
		var latest = declarations["LatestTargetFramework"];

		// Assert
		await Assert.That(floor).IsEqualTo("net10.0");
		await Assert
			.That(latest)
			.IsEqualTo("net11.0")
			.Because(
				"Purview.BuildSdk 2.x publishes net10.0 and net11.0; a change to the shipped set must be a "
					+ "deliberate edit to Versions.props and the package-shape assertion in verify-consumers."
			);
	}

	[Test]
	public async Task TargetFrameworkDeclarations_GivenRepository_DoNotReferenceOutOfSupportFrameworks(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var sourceRoot = Path.Combine(RepositoryPaths.Root, "src");
		var documents = Directory
			.EnumerateFiles(sourceRoot, "*.*proj", SearchOption.AllDirectories)
			.Concat(Directory.EnumerateFiles(sourceRoot, "*.props", SearchOption.AllDirectories))
			.Where(static path => !IsBuildOutput(path));

		// Act
		var offenders = documents
			.SelectMany(path =>
				XDocument
					.Load(path)
					.Descendants()
					.Where(element =>
						TargetFrameworkDeclarationNames.Contains(element.Name.LocalName, StringComparer.Ordinal)
					)
					.SelectMany(element =>
						element
							.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
							.Where(static framework => framework is "net8.0" or "net9.0")
							.Select(framework =>
								$"{Path.GetRelativePath(RepositoryPaths.Root, path)}: {element.Name.LocalName}={framework}"
							)
					)
			)
			.ToArray();

		// Assert
		await Assert
			.That(offenders)
			.IsEmpty()
			.Because(
				".NET 8 and .NET 9 are out of support. The shipped set is net10.0 (the floor) and net11.0 "
					+ "(the newest leg); reintroducing net8.0 or net9.0 silently commits the package to a "
					+ "runtime nobody is servicing."
			);
	}

	[Test]
	public async Task MicrosoftExtensionsPackages_GivenRepository_StayOnTheFloorServicingBand(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var packages = XDocument.Load(Path.Combine(RepositoryPaths.Root, "Directory.Packages.props"));
		var properties = packages
			.Descendants()
			.Where(element => element.Parent?.Name.LocalName == "PropertyGroup")
			.GroupBy(element => element.Name.LocalName, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.Last().Value.Trim(), StringComparer.Ordinal);
		var floorVersion = properties["DotNetRuntimeVersion"];
		var floorMajor = floorVersion.Split('.')[0];

		// Act
		var offenders = packages
			.Descendants()
			.Where(element => element.Name.LocalName == "PackageVersion")
			.Select(element => new
			{
				Include = element.Attribute("Include")?.Value,
				Version = Resolve(element.Attribute("Version")?.Value, properties),
			})
			.Where(package => package.Include?.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) == true)
			.Where(package =>
				package.Version is null || !package.Version.StartsWith($"{floorMajor}.", StringComparison.Ordinal)
			)
			.Select(package => $"{package.Include} {package.Version}")
			.ToArray();

		// Assert
		await Assert
			.That(offenders)
			.IsEmpty()
			.Because(
				$"The .NET 10 floor must not inherit a Microsoft.Extensions.* reference from the .NET 11 "
					+ $"band, even though the package also ships a net11.0 asset. Every such package must "
					+ $"resolve to {floorMajor}.x (the lowest common servicing band); otherwise a .NET 10 "
					+ "consumer is forced to download and use packages built for a framework it does not "
					+ "target. See docs/wiki/Toolchain.md."
			);
	}

	static string? Resolve(string? value, Dictionary<string, string> properties)
	{
		if (value is null || value.Length < 3 || !value.StartsWith("$(", StringComparison.Ordinal) || value[^1] != ')')
			return value;

		var propertyName = value[2..^1];
		return properties.TryGetValue(propertyName, out var resolved) ? resolved : value;
	}

	static bool IsBuildOutput(string path)
	{
		var segments = path.Split(Path.DirectorySeparatorChar);
		return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
			|| segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
	}
}
