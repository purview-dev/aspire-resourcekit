using Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration;

public class HostAppGeneratorTests : IncrementalSourceGeneratorTestBase<HostAppGenerator>
{
	[Test]
	public async Task Generate_GivenEmptySource_GeneratesAttributesOnly(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	public class Empty { }
}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result.GeneratedTrees).Count().IsEqualTo(ExpectedGeneratedFileCount);
	}

	[Test]
	public async Task Generate_GivenBasicHostApp_GeneratesExpectedHostApp(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[HostApp]
	partial class TestingHostApp
	{
	}
}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);

		// Assert — attribute files + 1 generated host app
		await Assert.That(result.GeneratedTrees).Count().IsEqualTo(ExpectedFileCountPlusGen);
	}

	[Test]
	public async Task Generate_GivenMultipleHostApps_GeneratesMultipleHostAppDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[HostApp]
	partial class TestingHostApp1
	{
	}

	[HostApp]
	partial class TestingHostApp2
	{
	}
}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.MultipleHostAppsFoundnfo);
	}

	[Test]
	public async Task Generate_GivenHostAppClassIsNotPartial_GeneratesClassMustBePartialDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostApp]
class TestingHostApp
{
}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.ClassMustBePartial);
	}
}
