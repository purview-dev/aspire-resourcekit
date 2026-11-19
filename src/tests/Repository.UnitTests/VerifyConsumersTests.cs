using System.Xml.Linq;

namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Holds <c>scripts/verify-consumers.proj</c> to building its consumers against the package the same run
/// packed.
/// </summary>
/// <remarks>
/// The packed version is normally unchanged between runs, and NuGet satisfies a version it has already
/// cached without reading the feed. Without an isolated packages folder a consumer builds green against an
/// older package, so a broken generator can pass verification - which happened once. These tests run in
/// the unit tier, so removing the isolation or the guard fails a PR instead of waiting for the next
/// <c>just verify-consumers</c>.
/// </remarks>
public sealed class VerifyConsumersTests
{
	const string RestorePackagesPathArgument = "-p:RestorePackagesPath=";

	static XDocument LoadScript() =>
		XDocument.Load(Path.Combine(RepositoryPaths.Root, "scripts", "verify-consumers.proj"));

	[Test]
	public async Task ConsumerBuilds_GivenScript_RestoreIntoTheRunsOwnPackagesFolder(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var commands = LoadScript()
			.Descendants()
			.Where(static element => element.Name.LocalName == "Exec")
			.Select(static element => element.Attribute("Command")?.Value ?? string.Empty)
			.Where(static command =>
				command.StartsWith("dotnet build", StringComparison.Ordinal)
				|| command.StartsWith("dotnet restore", StringComparison.Ordinal)
			)
			.ToArray();

		// Act
		var unisolated = commands
			.Where(static command =>
				!command.Contains(RestorePackagesPathArgument + "\"$(ConsumerPackagesDir)\"", StringComparison.Ordinal)
			)
			.ToArray();

		// Assert
		await Assert.That(commands).IsNotEmpty();
		await Assert
			.That(unisolated)
			.IsEmpty()
			.Because(
				"every consumer must restore into $(ConsumerPackagesDir), which each run deletes; the shared "
					+ "NuGet cache can hold an older build of the same version"
			);
	}

	[Test]
	public async Task ConsumerPackagesDir_GivenScript_LivesInsideTheDeletedVerifyRoot(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var properties = LoadScript()
			.Descendants()
			.Where(static element => element.Parent?.Name.LocalName == "PropertyGroup")
			.GroupBy(static element => element.Name.LocalName, StringComparer.Ordinal)
			.ToDictionary(
				static group => group.Key,
				static group => group.First().Value.Trim(),
				StringComparer.Ordinal
			);

		// Act
		var packagesDir = properties.GetValueOrDefault("ConsumerPackagesDir");

		// Assert
		await Assert
			.That(packagesDir)
			.StartsWith("$(VerifyRoot)/")
			.Because("only a folder under VerifyRoot is removed by PackPackage at the start of each run");
	}

	[Test]
	public async Task VerifyConsumers_GivenScript_AssertsTheConsumersUsedThisRunsPackage(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var targets = LoadScript()
			.Descendants()
			.Where(static element => element.Name.LocalName == "Target")
			.ToDictionary(static element => element.Attribute("Name")?.Value ?? string.Empty, StringComparer.Ordinal);

		// Act
		var entryDependencies = (targets["VerifyConsumers"].Attribute("DependsOnTargets")?.Value ?? string.Empty).Split(
			';',
			StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
		);
		var guardHashesTheGenerator =
			targets.TryGetValue("AssertConsumersUsedThisPackage", out var guard)
			&& guard.Descendants().Count(static element => element.Name.LocalName == "GetFileHash") == 2;

		// Assert
		await Assert
			.That(entryDependencies)
			.Contains("AssertConsumersUsedThisPackage")
			.Because("a green consumer build proves nothing unless it compiled against this run's package");
		await Assert
			.That(guardHashesTheGenerator)
			.IsTrue()
			.Because("the guard compares the packed and restored generator, not just the restore location");
	}
}
