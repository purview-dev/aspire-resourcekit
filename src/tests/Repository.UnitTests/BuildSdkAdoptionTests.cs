using System.Xml.Linq;

namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Guards the repository's explicit <c>Purview.BuildSdk</c> feature opt-ins.
/// </summary>
/// <remarks>
/// A patch upgrade can change a default without failing anything. <c>Purview.BuildSdk</c> 2.0.1 made
/// Runtime Async opt-in, where 2.0.0 defaulted it on, so the <c>net11.0</c> leg silently stopped
/// compiling with the feature the toolchain documentation describes. Nothing in the build reported
/// that; this test does, and it fails on the configuration rather than on the compiler switch so the
/// message names the property to restore.
/// </remarks>
public sealed class BuildSdkAdoptionTests
{
	[Test]
	public async Task RuntimeAsync_GivenRepository_IsExplicitlyOptedInForTheNet11Leg(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var props = XDocument.Load(Path.Combine(RepositoryPaths.Root, "src", "Directory.Build.props"));

		// Act
		var value = props
			.Descendants()
			.FirstOrDefault(element => element.Name.LocalName == "PurviewRuntimeAsync")
			?.Value.Trim();

		// Assert
		await Assert
			.That(value)
			.IsEqualTo("true")
			.Because(
				"Runtime Async is opt-in in Purview.BuildSdk 2.0.1 (2.0.0 defaulted it on). The net11.0 "
					+ "leg is the leg that exercises the .NET 11 feature set, so the repository must opt in "
					+ "explicitly rather than inherit whichever default the next patch release ships. The "
					+ "SDK applies it only to net11.0+ targets, so the net10.0 floor is unaffected."
			);
	}
}
