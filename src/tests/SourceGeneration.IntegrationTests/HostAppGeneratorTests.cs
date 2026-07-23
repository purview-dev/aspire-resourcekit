using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

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
		var (result, _) = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.ClassMustBePartial);
	}

	[Test]
	public async Task Generate_GivenHostAppWithNonEmptyConstructor_GeneratesNonEmptyConstructorsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostApp]
partial class TestingHostApp
{
	public TestingHostApp(string value)
	{
	}
}
";

		// Act
		var (result, _) = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.NonEmptyConstructorsNotSupported);
	}

	[Test]
	public async Task Generate_GivenAppResourceWithNonEmptyConstructor_GeneratesNonEmptyConstructorsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostApp]
partial class TestingHostApp;

[AppResource]
partial class RedisAppResource : TestingHostAppResourceBase<object>
{
	public RedisAppResource()
	{
		var value = 42;
	}
}
";

		// Act
		var (result, _) = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.NonEmptyConstructorsNotSupported);
	}
}
