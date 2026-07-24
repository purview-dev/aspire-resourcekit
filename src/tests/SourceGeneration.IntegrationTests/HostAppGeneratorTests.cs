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

[ResourceDefinition]
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

	[Test]
	public async Task Generate_GivenResourceWithExplicitIncorrectBase_GeneratesMustDeriveFromBaseDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostApp]
partial class TestingHostApp;

[ResourceDefinition]
partial class RedisAppResource : global::Purview.Aspire.ResourceKit.ResourceKitBase<TestingHostApp, global::Aspire.Hosting.ApplicationModel.Resource>
{
	protected override global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.Resource> BuildResource(global::Aspire.Hosting.IDistributedApplicationBuilder builder)
		=> throw new global::System.NotImplementedException();
}
";

		// Act
		var (result, _) = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.ResourceMustDeriveFromBase);
	}

	[Test]
	public async Task Generate_GivenResourceWithoutExplicitBaseAndNonGenericResourceDefinition_GeneratesNonGenericRequiresExplicitBaseDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostApp]
partial class TestingHostApp;

[ResourceDefinition]
partial class RedisAppResource;
";

		// Act
		var (result, _) = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.NonGenericResourceDefinitionRequiresExplicitBase);
	}

	[Test]
	public async Task Generate_GivenResourceWithExplicitBaseAndGenericResourceDefinition_GeneratesGenericCannotHaveExplicitBaseDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostApp]
partial class TestingHostApp;

[ResourceDefinition<global::Aspire.Hosting.ApplicationModel.Resource>]
partial class RedisAppResource : TestingHostAppResourceBase<global::Aspire.Hosting.ApplicationModel.Resource>;
";

		// Act
		var (result, _) = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.GenericResourceDefinitionCannotHaveExplicitBase);
	}

	[Test]
	public async Task Generate_GivenResourceWithGenericAndNonGenericResourceDefinition_GeneratesMixedUsageDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostApp]
partial class TestingHostApp;

[ResourceDefinition]
[ResourceDefinition<global::Aspire.Hosting.ApplicationModel.Resource>]
partial class RedisAppResource;
";

		// Act
		var (result, _) = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.MixedResourceDefinitionAttributesNotSupported);
	}
}
