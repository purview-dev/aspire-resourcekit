using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies that <c>SG0017</c> is reported by <see cref="ResourceKitDiagnosticAnalyzer"/> when an
/// <c>IResourceBuilder&lt;T&gt;</c> property on a resource kit is never assigned in either
/// <c>BuildResource</c> or <c>ConfigureResource</c>.
/// </summary>
public class ResourcePropertyNeverSetTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	[Test]
	public async Task Generate_GivenNonNullableResourceBuilderPropertyNeverSet_ReportsResourcePropertyNeverSet(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class RedisResourceKit
			{
				public IResourceBuilder<DefaultAspireResource> Cache { get; private set; }

				protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
					throw new global::System.NotImplementedException();
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourcePropertyNeverSet);
	}

	[Test]
	public async Task Generate_GivenNullableResourceBuilderPropertyNeverSet_ReportsResourcePropertyNeverSet(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class RedisResourceKit
			{
				public IResourceBuilder<DefaultAspireResource>? Cache { get; private set; }

				protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
					throw new global::System.NotImplementedException();
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourcePropertyNeverSet);
	}

	[Test]
	public async Task Generate_GivenResourceBuilderPropertySetInBuildResource_DoesNotReportResourcePropertyNeverSet(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class RedisResourceKit
			{
				public IResourceBuilder<DefaultAspireResource> Cache { get; private set; }

				protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder)
				{
					Cache = ResourceBuilder;
					return ResourceBuilder;
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ResourcePropertyNeverSet);
	}

	[Test]
	public async Task Generate_GivenResourceBuilderPropertySetInConfigureResource_DoesNotReportResourcePropertyNeverSet(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class RedisResourceKit
			{
				public IResourceBuilder<DefaultAspireResource> Cache { get; private set; }

				protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
					throw new global::System.NotImplementedException();

				protected override void ConfigureResource()
				{
					Cache = ResourceBuilder;
					base.ConfigureResource();
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ResourcePropertyNeverSet);
	}

	[Test]
	public async Task Generate_GivenNonResourceBuilderPropertyNeverSet_DoesNotReportResourcePropertyNeverSet(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class RedisResourceKit
			{
				public string DisplayName { get; private set; }

				protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
					throw new global::System.NotImplementedException();
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ResourcePropertyNeverSet);
	}

	[Test]
	public async Task Generate_GivenResourceBuilderPropertyNeverSet_StillGeneratesKit(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			sealed partial class RedisResourceKit
			{
				public IResourceBuilder<DefaultAspireResource> Cache { get; private set; }

				protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
					throw new global::System.NotImplementedException();
			}
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert — SG0017 is an execution-only warning, so it is reported but generation still proceeds.
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ResourcePropertyNeverSet);
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert
			.That(generated)
			.Contains(
				$"partial class RedisResourceKit : {TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase}<{TestingTypeLibrary.Purview.Aspire.ResourceKit.DefaultAspireResource}>"
			);
		await Assert.That(generated).Contains("AddAspireResourceKit");
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
				.WithAdditionalNamespaces(TestingTypeLibrary.Purview.Aspire.ResourceKit.DefaultAspireResource),
			cancellationToken
		);
	}
}
