using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies what <see cref="HostKitGenerator"/> emits for <c>[ResourceUrl]</c> and <c>[EndpointUrl]</c>, and the
/// URL diagnostics <see cref="ResourceKitDiagnosticAnalyzer"/> reports (SG0026-SG0028).
/// </summary>
public sealed class ResourceUrlGenerationTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string ContainerKit = """
		protected override IResourceBuilder<ContainerResource> BuildResource(IDistributedApplicationBuilder builder) =>
			builder.AddContainer(Name, "image");
		""";

	const string ResourceKit = """
		protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
			throw new global::System.NotImplementedException();
		""";

	[Test]
	public async Task Generate_GivenResourceUrl_EmitsUrlOptionAndUrlsOverride(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<ContainerResource>]
			[ResourceUrl("Health", "/health", DisplayText = "Health", Endpoint = "http")]
			sealed partial class ApiKit
			{
				{{ContainerKit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert
			.That(generated)
			.Contains(
				$"public {TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitUrl} Health {{ get; set; }} = new {TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitUrl}() {{ Url = \"/health\", DisplayText = \"Health\", Endpoint = \"http\", DisplayLocation = {TypeLibrary.Aspire.Hosting.ApplicationModel.UrlDisplayLocation}.SummaryAndDetails }};"
			);
		await Assert.That(generated).Contains("Urls => [Options.Urls.Health];");
	}

	[Test]
	public async Task Generate_GivenEndpointUrl_NamesOptionAfterEndpointInPascalCase(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<ContainerResource>]
			[EndpointUrl("otlp-grpc", DisplayText = "OTLP", DisplayLocation = {{TypeLibrary.Aspire.Hosting.ApplicationModel.UrlDisplayLocation}}.DetailsOnly)]
			sealed partial class CollectorKit
			{
				{{ContainerKit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("ResourceKitUrl OtlpGrpc { get; set; }");
		await Assert
			.That(generated)
			.Contains(
				$"DisplayText = \"OTLP\", Endpoint = \"otlp-grpc\", DisplayLocation = {TypeLibrary.Aspire.Hosting.ApplicationModel.UrlDisplayLocation}.DetailsOnly"
			);
	}

	[Test]
	public async Task Generate_GivenGenerateOptionsDisabled_EmitsUrlsInline(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit(GenerateOptions = false)]
			partial class TestingHostKit;

			[ResourceDefinition<ContainerResource>]
			[ResourceUrl("Docs", "https://example.com/docs")]
			sealed partial class ApiKit
			{
				{{ContainerKit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).DoesNotContain("UrlsOptions");
		await Assert
			.That(generated)
			.Contains(
				$"Urls => [new {TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitUrl}() {{ Url = \"https://example.com/docs\""
			);
	}

	[Test]
	public async Task Generate_GivenKitWithoutUrls_DoesNotOverrideUrls(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<ContainerResource>]
			sealed partial class ApiKit
			{
				{{ContainerKit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.GetSource()).DoesNotContain("Urls =>");
	}

	[Test]
	public async Task Generate_GivenGroupUrl_AssignsGroupUrlsFromOptions(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			[ResourceUrl("Runbook", "https://example.com/runbook")]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.GetSource()).Contains("Urls = [Options.Urls.Runbook];");
	}

	[Test]
	public async Task Generate_GivenTwoUrlsWithTheSameName_ReportsDuplicateUrlName(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<ContainerResource>]
			[ResourceUrl("Health", "/health")]
			[ResourceUrl("Health", "/alive")]
			sealed partial class ApiKit
			{
				{{ContainerKit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.DuplicateUrlName);
	}

	[Test]
	public async Task Generate_GivenEndpointUrlOnResourceWithoutEndpoints_ReportsEndpointUrlRequiresEndpoints(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			[EndpointUrl("http")]
			sealed partial class ParameterKit
			{
				{{ResourceKit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.EndpointUrlRequiresEndpoints);
	}

	[Test]
	public async Task Generate_GivenEndpointUrlOnGroup_ReportsEndpointUrlRequiresEndpoints(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			[EndpointUrl("http")]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.EndpointUrlRequiresEndpoints);
	}

	[Test]
	public async Task Generate_GivenRelativeUrlOnGroup_ReportsRelativeUrlWithoutEndpointsAndStillGenerates(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			[ResourceUrl("Health", "/health")]
			partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.RelativeUrlWithoutEndpoints);
		await Assert.That(result).HasNoErrorDiagnostics();
	}

	[Test]
	public async Task Generate_GivenAbsoluteUrlOnResourceWithoutEndpoints_DoesNotReportRelativeUrlWithoutEndpoints(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>]
			[ResourceUrl("Docs", "https://example.com/docs")]
			sealed partial class ParameterKit
			{
				{{ResourceKit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RelativeUrlWithoutEndpoints);
		await Assert.That(result).HasNoErrorDiagnostics();
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
