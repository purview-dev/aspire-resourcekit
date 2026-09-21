using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Purview.Aspire.ResourceKit.Models;

namespace Purview.Aspire.ResourceKit;

public sealed class IResourceBuilderExtensionsTests
{
	[Test]
	public async Task WithEnvironment_GivenIReadOnlyDictionary_AppliesEntriesVerbatim(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var resource = builder.AddContainer("test", "nginx");

		IReadOnlyDictionary<string, string> values = new Dictionary<string, string>
		{
			["Services__ServiceName"] = "migrator",
			["Services__DisplayName"] = "ChangeOps DbContext Migrator Service",
		};

		// Act
		resource.WithEnvironment(values);

		EnvironmentCallbackContext context = new(
			builder.ExecutionContext,
			resource.Resource,
			cancellationToken: cancellationToken
		);
		foreach (var annotation in resource.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
			await annotation.Callback(context);

		// Assert
		await Assert.That(context.EnvironmentVariables.Keys).IsEquivalentTo(values.Keys);
		await Assert.That(context.EnvironmentVariables["Services__ServiceName"]).IsEqualTo("migrator");
		await Assert
			.That(context.EnvironmentVariables["Services__DisplayName"])
			.IsEqualTo("ChangeOps DbContext Migrator Service");
		await Assert
			.That(context.EnvironmentVariables.Keys)
			.DoesNotContain("IReadOnlyDictionary__Services__ServiceName");
	}

	[Test]
	public async Task WithEnvironment_GivenOptionsHelperBuildResult_AppliesEntriesVerbatim(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var resource = builder.AddContainer("test", "nginx");

		ChangeOpsServiceOptions options = new()
		{
			ServiceName = "migrator",
			DisplayName = "ChangeOps DbContext Migrator Service",
		};

		// Act
		resource.WithEnvironment(OptionsHelper.Environment(options).Build());

		EnvironmentCallbackContext context = new(
			builder.ExecutionContext,
			resource.Resource,
			cancellationToken: cancellationToken
		);
		foreach (var annotation in resource.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
			await annotation.Callback(context);

		// Assert
		await Assert.That(context.EnvironmentVariables["Services__ServiceName"]).IsEqualTo("migrator");
		await Assert
			.That(context.EnvironmentVariables.Keys)
			.DoesNotContain("IReadOnlyDictionary__Services__ServiceName");
	}
}
