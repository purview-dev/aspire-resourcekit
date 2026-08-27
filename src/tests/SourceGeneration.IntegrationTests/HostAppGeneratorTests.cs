using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

public class HostKitGeneratorTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	[Test]
	public async Task Generate_GivenEmptySource_GeneratesAttributesOnly(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

public class Empty { }
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result.DriverResult.GeneratedTrees.Length).IsEqualTo(ExpectedGeneratedFileCount);
	}

	[Test]
	public async Task Generate_GivenBasicHostKit_GeneratesExpectedHostKit(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostKit]
partial class TestingHostKit;
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert — attribute files + 1 generated host app
		await Assert.That(result.DriverResult.GeneratedTrees.Length).IsEqualTo(ExpectedFileCountPlusGen);
	}

	[Test]
	public async Task Generate_GivenMultipleHostKits_GeneratesMultipleHostKitDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing;

[HostKit]
partial class TestingHostKit1;

[HostKit]
partial class TestingHostKit2;
";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.MultipleHostKitsFoundInfo);
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
class TestingHostKit;
";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ClassMustBePartial);
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
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.NonEmptyConstructorsNotSupported);
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
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.NonEmptyConstructorsNotSupported);
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
partial class RedisResourceKit : {TypeLibrary.ResourceKitBase.Name}_INVALID<TestingHostKit, {TestHelper.DefaultAspireResource}>
{{
	{TestHelper.GenerateBuildResourceMethod()}
}}
";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourceMustDeriveFromResourceKitBase);
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
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.NonGenericResourceDefinitionRequiresExplicitBase);
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

[ResourceDefinition<DefaultAspireResource>]
partial class RedisResourceKit : ResourceKitBase<DefaultAspireResource>;
";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.GenericResourceDefinitionCannotHaveExplicitBase);
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
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.MixedResourceDefinitionAttributesNotSupported);
	}

	protected override ResourceKitSourceGeneratorTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		ResourceKitSourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	)
	{
		return base.OnBeforeRun(
			sources,
			options
				.WithAdditionalAssemblyTypes(typeof(DefaultAspireResource))
				.WithAdditionalNamespaces(TestHelper.DefaultAspireResource),
			cancellationToken
		);
	}
}
