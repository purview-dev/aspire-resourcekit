using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Purview.Aspire.ResourceKit.Example.AppHost.AppModels;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Exercises the example AppHost's generated parent/child kits without starting the AppHost, plus the
/// configuration paths and group placement that the hierarchy adds.
/// </summary>
public sealed class ExampleAppHostHierarchyModelTests
{
	[Test]
	public async Task AddResourceKit_GivenDefaultMode_CreatesGeneratedChildKits()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();

		// Act
		ExampleAppHostModel.AddResourceKit(builder);

		// Assert
		await Assert.That(builder.Resources.Count(static resource => resource.Name == "postgres")).IsEqualTo(1);
		await Assert.That(builder.Resources.Count(static resource => resource.Name == "db")).IsEqualTo(1);
		await Assert.That(builder.Resources.Count(static resource => resource.Name == "azure-storage")).IsEqualTo(1);
		await Assert.That(builder.Resources.Count(static resource => resource.Name == "blob")).IsEqualTo(1);
	}

	[Test]
	public async Task AddResourceKit_GivenParentDisabled_DoesNotCreateParentOrDescendantResources()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		builder.Configuration.AddInMemoryCollection(
			new Dictionary<string, string?> { ["ExampleHostKit:Postgres:IsEnabled"] = "false" }
		);

		// Act
		ExampleAppHostModel.AddResourceKit(builder);

		// Assert
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "postgres")).IsFalse();
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "db")).IsFalse();
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "api")).IsTrue();
	}

	[Test]
	public async Task AddResourceKit_GivenChildDisabled_KeepsParentAndRemovesOnlyTheChild()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		builder.Configuration.AddInMemoryCollection(
			new Dictionary<string, string?> { ["ExampleHostKit:Postgres:Database:IsEnabled"] = "false" }
		);

		// Act
		ExampleAppHostModel.AddResourceKit(builder);

		// Assert
		await Assert.That(builder.Resources.Count(static resource => resource.Name == "postgres")).IsEqualTo(1);
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "db")).IsFalse();
	}

	[Test]
	public async Task AddResourceKit_GivenPublishMode_CreatesTheGeneratedVnetAndSubnets()
	{
		// Arrange — the example's VNET hierarchy is publish-only so the run-mode AppHost never needs Azure.
		var builder = CreatePublishBuilder();

		// Act
		ExampleAppHostModel.AddResourceKit(builder);

		// Assert
		await Assert.That(builder.Resources.Count(static resource => resource.Name == "vnet")).IsEqualTo(1);
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "snet-web")).IsTrue();
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "snet-data")).IsTrue();
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "snet-private-endpoint")).IsTrue();
	}

	[Test]
	public async Task AddResourceKit_GivenVnetDisabled_DoesNotCreateAnySubnet()
	{
		// Arrange
		var builder = CreatePublishBuilder();
		builder.Configuration.AddInMemoryCollection(
			new Dictionary<string, string?> { ["ExampleHostKit:Vnet:IsEnabled"] = "false" }
		);

		// Act
		ExampleAppHostModel.AddResourceKit(builder);

		// Assert
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "vnet")).IsFalse();
		await Assert
			.That(builder.Resources.Any(static resource => resource.Name.StartsWith("snet-", StringComparison.Ordinal)))
			.IsFalse();
	}

	[Test]
	public async Task AddResourceKit_GivenGroups_AddsDashboardNodesAndParentsKitResources()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();

		// Act
		ExampleAppHostModel.AddResourceKit(builder);

		// Assert
		var groupNodes = builder.Resources.OfType<ResourceKitGroupResource>().ToArray();
		await Assert.That(groupNodes.Any(static group => group.Name == "data")).IsTrue();

		var postgres = builder.Resources.Single(static resource => resource.Name == "postgres");
		var postgresParent = ParentOf(postgres);
		await Assert.That(postgresParent).IsNotNull();
		await Assert.That(postgresParent!.Name).IsEqualTo("data");
	}

	static IDistributedApplicationBuilder CreatePublishBuilder() =>
		DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = ["--publisher", "manifest"] });

	static IResource? ParentOf(IResource resource) =>
		resource
			.Annotations.OfType<ResourceRelationshipAnnotation>()
			.FirstOrDefault(static annotation => annotation.Type == "Parent")
			?.Resource;
}
