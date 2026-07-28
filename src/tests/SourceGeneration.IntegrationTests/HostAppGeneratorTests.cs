using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

public class HostKitGeneratorTests : IncrementalSourceGeneratorTestBase<HostKitGenerator>
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
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result.SyntaxTrees.Count()).IsEqualTo(ExpectedGeneratedFileCount);
	}

	[Test]
	public async Task Generate_GivenBasicHostKit_GeneratesExpectedHostKit(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[HostKit]
	partial class TestingHostKit
	{
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert — attribute files + 1 generated host app
		await Assert.That(result.SyntaxTrees.Count()).IsEqualTo(ExpectedFileCountPlusGen);
	}

	[Test]
	public async Task Generate_GivenMultipleHostKits_GeneratesMultipleHostKitDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[HostKit]
	partial class TestingHostKit1
	{
	}

	[HostKit]
	partial class TestingHostKit2
	{
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.MultipleHostKitsFoundInfo);
	}

	[Test]
	public async Task Generate_GivenHostKitClassIsNotPartial_GeneratesClassMustBePartialDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostKit]
class TestingHostKit
{
}
";

		// Act
		var result = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.ClassMustBePartial);
	}

	[Test]
	public async Task Generate_GivenHostKitWithNonEmptyConstructor_GeneratesNonEmptyConstructorsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostKit]
partial class TestingHostKit
{
	public TestingHostKit(string value)
	{
	}
}
";

		// Act
		var result = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.NonEmptyConstructorsNotSupported);
	}

	[Test]
	public async Task Generate_GivenResourceKitWithNonEmptyConstructor_GeneratesNonEmptyConstructorsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostKit]
partial class TestingHostKit;

[ResourceDefinition]
partial class RedisResourceKit : ResourceKitBase<TestResource>
{
	public RedisResourceKit()
	{
		var value = 42;
	}
}
";

		// Act
		var result = await GenerateAsync(
			source,
			new(ThrowOnGenerationException: false, CompileToAssembly: false),
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
		var source =
			@$"
namespace Testing;

[HostKit]
partial class TestingHostKit;

[ResourceDefinition]
partial class RedisResourceKit : {TypeHelpers.ResourceKitBase.TypeName}_INVALID<TestingHostKit, {TestHelper.DefaultAspireResource}>
{{
	{TestHelper.GenerateBuildResource()}
}}
";

		// Act
		var result = await GenerateAsync(
			source,
			new GenerationDriverContext(ThrowOnGenerationException: false, CompileToAssembly: false),
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

[HostKit]
partial class TestingHostKit;

[ResourceDefinition]
partial class RedisResourceKit;
";

		// Act
		var result = await GenerateAsync(
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

[HostKit]
partial class TestingHostKit;

[ResourceDefinition<TestResource>]
partial class RedisResourceKit : ResourceKitBase<TestResource>;
";

		// Act
		var result = await GenerateAsync(
			source,
			new GenerationDriverContext(ThrowOnGenerationException: false, CompileToAssembly: false),
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

[HostKit]
partial class TestingHostKit;

[ResourceDefinition]
[ResourceDefinition<global::Aspire.Hosting.ApplicationModel.Resource>]
partial class RedisResourceKit;
";

		// Act
		var result = await GenerateAsync(
			source,
			GenerationDriverContext.DoNotThrowOnGenerationException,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(GeneratorDiagnostics.MixedResourceDefinitionAttributesNotSupported);
	}
}
