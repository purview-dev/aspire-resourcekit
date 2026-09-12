using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies that every diagnostic message is fully formatted: all <c>{n}</c> placeholders in the
/// descriptor message formats are populated, so rendering a diagnostic never throws or leaks a literal
/// <c>{0}</c>-style token.
/// </summary>
public partial class DiagnosticMessageRenderingTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	[GeneratedRegex(@"\{\d+\}")]
	private static partial Regex UnfilledPlaceholder();

	[Test]
	public async Task Generate_GivenResourceKitStructuralErrors_RendersAllMessages(CancellationToken cancellationToken)
	{
		// Arrange — resource kits that trigger SG0006, SG0007, SG0008, SG0012, SG0013, SG0014, SG0015.
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition]
				partial class RedisResourceKit;

				[ResourceDefinition]
				partial class SqlResourceKit : NotAValidBase<DefaultAspireResource>;

				[ResourceDefinition("   ")]
				partial class KeyVaultResourceKit : ResourceKitBase<DefaultAspireResource>;

				[ResourceDefinition(PropertyName = "not-valid")]
				partial class StorageResourceKit : ResourceKitBase<DefaultAspireResource>;

				[ResourceDefinition]
				[ResourceDefinition<DefaultAspireResource>]
				partial class MixedResourceKit;

				[ResourceDefinition<DefaultAspireResource>]
				partial class GenericResourceKit : ResourceKitBase<DefaultAspireResource>;

				[ResourceDefinition<DefaultAspireResource>]
				partial class ConstructorResourceKit
				{
					public ConstructorResourceKit(string value) { }

					protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
						throw new global::System.NotImplementedException();
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await AssertMessagesRenderAsync(result);
	}

	[Test]
	public async Task Generate_GivenNonPartialDeclarations_RendersClassMustBePartialMessage(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				class TestingHostKit;

				[ResourceDefinition]
				class RedisResourceKit : ResourceKitBase<DefaultAspireResource>;
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await AssertMessagesRenderAsync(result);
	}

	[Test]
	public async Task Generate_GivenDuplicatePropertyNamesAndMultipleHostKits_RendersMessages(
		CancellationToken cancellationToken
	)
	{
		// Arrange — triggers SG0004 (multiple host kits) and SG0005 (duplicate property names).
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[HostKit]
				partial class SecondHostKit;

				[ResourceDefinition(PropertyName = "Shared")]
				partial class RedisResourceKit : ResourceKitBase<DefaultAspireResource>;

				[ResourceDefinition(PropertyName = "Shared")]
				partial class CacheResourceKit : ResourceKitBase<DefaultAspireResource>;
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await AssertMessagesRenderAsync(result);
	}

	[Test]
	public async Task Generate_GivenHostKitWithNoResources_RendersNoResourceKitsMessage(
		CancellationToken cancellationToken
	)
	{
		// Arrange — triggers SG0002.
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await AssertMessagesRenderAsync(result);
	}

	[Test]
	public async Task Generate_GivenProjectKitIssues_RendersMessages(CancellationToken cancellationToken)
	{
		// Arrange — triggers SG0018 (different AddProject) and SG0019 (non-ProjectResource base).
		const string source = """
			namespace Projects
			{
				public class Example_Service : global::Aspire.Hosting.IProjectMetadata
				{
					public string ProjectPath => "";
					public bool SuppressBuild => true;
				}

				public class Other_Service : global::Aspire.Hosting.IProjectMetadata
				{
					public string ProjectPath => "";
					public bool SuppressBuild => true;
				}
			}

			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<Projects.Example_Service>]
				sealed partial class ApiKit
				{
					protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
						builder.AddProject<Projects.Other_Service>(Name);
				}

				[ResourceDefinition<Projects.Example_Service>]
				sealed partial class StorageKit : ResourceKitBase<DefaultAspireResource>
				{
					protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
						throw new global::System.NotImplementedException();
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await AssertMessagesRenderAsync(result);
	}

	[Test]
	public async Task Generate_GivenResourceBuilderPropertyNeverSet_RendersMessage(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			namespace Testing
			{
				[HostKit]
				partial class TestingHostKit;

				[ResourceDefinition<DefaultAspireResource>]
				sealed partial class RedisResourceKit
				{
					public IResourceBuilder<DefaultAspireResource> Cache { get; private set; }

					protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
						throw new global::System.NotImplementedException();
				}
			}
			""";

		// Act
		var result = await GenerateAsync(source, ResourceKitSourceGeneratorTestOptions.NoValidation, cancellationToken);

		// Assert
		await AssertMessagesRenderAsync(result);
	}

	static async Task AssertMessagesRenderAsync(DriverRunResult result)
	{
		var diagnostics = result
			.DriverResult.Diagnostics.Concat(result.AnalyzerResult?.Diagnostics ?? [])
			.Where(static diagnostic => diagnostic.Location.SourceTree is not null)
			.ToArray();

		await Assert.That(diagnostics).IsNotEmpty();

		foreach (var diagnostic in diagnostics)
		{
			var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
			await Assert.That(UnfilledPlaceholder().IsMatch(message)).IsFalse();
		}
	}
}
