using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Builds real Aspire models for the resource hierarchies in issue #57. The tests register resources through
/// <c>Parent.ResourceBuilder</c> and inspect the application model; they never provision cloud infrastructure.
/// </summary>
public sealed class ResourceKitHierarchyAspireModelTests
{
	[Test]
	public async Task Build_GivenPostgresServerAndDatabase_CreatesTheDatabaseThroughTheParentBuilder()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		ModelHostKit host = new();
		PostgresServerKit postgres = new(host, "postgres");
		PostgresDatabaseKit database = new(host, postgres, "db");
		host.AddResource(postgres);
		host.AddResource(database);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "postgres")).IsTrue();
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "db")).IsTrue();
		await Assert.That(database.ResourceBuilder.Resource).IsTypeOf<AzurePostgresFlexibleServerDatabaseResource>();
		await Assert
			.That(database.ResourceBuilder.ApplicationBuilder)
			.IsSameReferenceAs(postgres.ResourceBuilder.ApplicationBuilder);
		await Assert.That(database.Parent).IsSameReferenceAs(postgres);
	}

	[Test]
	public async Task Build_GivenVnetAndSeveralSubnets_RegistersEverySubnetThroughTheVnetBuilder()
	{
		// Arrange — VNET/subnet are provisioning resources, so Aspire only adds them to the model in publish mode.
		var builder = DistributedApplication.CreateBuilder(
			new DistributedApplicationOptions { Args = ["--publisher", "manifest"] }
		);
		ModelHostKit host = new();
		VnetKit vnet = new(host, "vnet");
		SubnetKit web = new(host, vnet, "snet-web", "10.0.1.0/24");
		SubnetKit data = new(host, vnet, "snet-data", "10.0.2.0/24");
		SubnetKit privateEndpoint = new(host, vnet, "snet-private-endpoint", "10.0.3.0/24");

		host.AddResource(vnet);
		host.AddResource(web);
		host.AddResource(data);
		host.AddResource(privateEndpoint);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(builder.Resources.Count(static resource => resource.Name == "vnet")).IsEqualTo(1);
		foreach (
			var (name, prefix) in new[]
			{
				("snet-web", "10.0.1.0/24"),
				("snet-data", "10.0.2.0/24"),
				("snet-private-endpoint", "10.0.3.0/24"),
			}
		)
		{
			var subnet = await Assert
				.That(builder.Resources)
				.HasSingleItem(resource => resource.Name == name && resource is AzureSubnetResource);

			await Assert.That(((AzureSubnetResource)subnet).AddressPrefix).IsEqualTo(prefix);
		}

		await Assert.That(web.Parent).IsSameReferenceAs(vnet);
		await Assert.That(web.BuiltWithParentBuilder).IsTrue();
		await Assert.That(data.BuiltWithParentBuilder).IsTrue();
		await Assert.That(privateEndpoint.BuiltWithParentBuilder).IsTrue();
	}

	[Test]
	public async Task Build_GivenStorageBlobContainerHierarchy_RegistersTheGrandchildContainer()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		ModelHostKit host = new();
		StorageAccountKit storage = new(host, "storage");
		BlobContainerKit container = new(host, storage, "documents", "documents");
		host.AddResource(storage);
		host.AddResource(container);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "storage")).IsTrue();
		await Assert.That(container.ResourceBuilder.Resource).IsTypeOf<AzureBlobStorageContainerResource>();
		await Assert.That(container.Parent).IsSameReferenceAs(storage);
	}

	[Test]
	public async Task Build_GivenServiceBusNamespaceTopicQueueAndSubscription_RegistersEveryChild()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		ModelHostKit host = new();
		ServiceBusNamespaceKit serviceBus = new(host, "servicebus");
		ServiceBusTopicKit topic = new(host, serviceBus, "orders");
		ServiceBusSubscriptionKit subscription = new(host, topic, "orders-processor");
		ServiceBusQueueKit queue = new(host, serviceBus, "orders-queue");

		host.AddResource(serviceBus);
		host.AddResource(topic);
		host.AddResource(subscription);
		host.AddResource(queue);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "servicebus")).IsTrue();
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "orders")).IsTrue();
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "orders-processor")).IsTrue();
		await Assert.That(builder.Resources.Any(static resource => resource.Name == "orders-queue")).IsTrue();
		await Assert.That(topic.Parent).IsSameReferenceAs(serviceBus);
		await Assert.That(subscription.Parent).IsSameReferenceAs(topic);
		await Assert.That(queue.Parent).IsSameReferenceAs(serviceBus);
	}

	[Test]
	public async Task Build_GivenDisabledVnet_DoesNotRegisterAnySubnet()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		ModelHostKit host = new();
		VnetKit vnet = new(host, "vnet") { IsEnabled = false };
		SubnetKit web = new(host, vnet, "snet-web", "10.0.1.0/24");
		SubnetKit data = new(host, vnet, "snet-data", "10.0.2.0/24");

		host.AddResource(vnet);
		host.AddResource(web);
		host.AddResource(data);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(vnet.IsEnabled).IsFalse();
		await Assert.That(web.IsEnabled).IsFalse();
		await Assert.That(data.IsEnabled).IsFalse();
		await Assert
			.That(builder.Resources.Any(static resource => resource.Name.StartsWith("snet-", StringComparison.Ordinal)))
			.IsFalse();
	}

	sealed class ModelHostKit : HostKitBase<ModelHostKit>;

	sealed class PostgresServerKit(ModelHostKit hostKit, string name)
		: ResourceKitBase<ModelHostKit, AzurePostgresFlexibleServerResource>(hostKit, name)
	{
		protected override IResourceBuilder<AzurePostgresFlexibleServerResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => builder.AddAzurePostgresFlexibleServer(Name);
	}

	sealed class PostgresDatabaseKit(ModelHostKit hostKit, PostgresServerKit parent, string name)
		: ResourceKitBase<ModelHostKit, AzurePostgresFlexibleServerDatabaseResource>(hostKit, parent, name)
	{
		public PostgresServerKit Parent { get; } = parent;

		protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => Parent.ResourceBuilder.AddDatabase(Name);
	}

	sealed class VnetKit(ModelHostKit hostKit, string name)
		: ResourceKitBase<ModelHostKit, AzureVirtualNetworkResource>(hostKit, name)
	{
		protected override IResourceBuilder<AzureVirtualNetworkResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => builder.AddAzureVirtualNetwork(Name);
	}

	sealed class SubnetKit(ModelHostKit hostKit, VnetKit parent, string name, string prefix)
		: ResourceKitBase<ModelHostKit, AzureSubnetResource>(hostKit, parent, name)
	{
		public VnetKit Parent { get; } = parent;

		public bool BuiltWithParentBuilder { get; private set; }

		protected override IResourceBuilder<AzureSubnetResource> BuildResource(IDistributedApplicationBuilder builder)
		{
			BuiltWithParentBuilder = ReferenceEquals(Parent.ResourceBuilder.ApplicationBuilder, builder);
			return Parent.ResourceBuilder.AddSubnet(Name, prefix);
		}
	}

	sealed class StorageAccountKit(ModelHostKit hostKit, string name)
		: ResourceKitBase<ModelHostKit, AzureStorageResource>(hostKit, name)
	{
		protected override IResourceBuilder<AzureStorageResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => builder.AddAzureStorage(Name);
	}

	sealed class BlobContainerKit(ModelHostKit hostKit, StorageAccountKit parent, string name, string containerName)
		: ResourceKitBase<ModelHostKit, AzureBlobStorageContainerResource>(hostKit, parent, name)
	{
		public StorageAccountKit Parent { get; } = parent;

		protected override IResourceBuilder<AzureBlobStorageContainerResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => Parent.ResourceBuilder.AddBlobContainer(Name, containerName);
	}

	sealed class ServiceBusNamespaceKit(ModelHostKit hostKit, string name)
		: ResourceKitBase<ModelHostKit, AzureServiceBusResource>(hostKit, name)
	{
		protected override IResourceBuilder<AzureServiceBusResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => builder.AddAzureServiceBus(Name);
	}

	sealed class ServiceBusTopicKit(ModelHostKit hostKit, ServiceBusNamespaceKit parent, string name)
		: ResourceKitBase<ModelHostKit, AzureServiceBusTopicResource>(hostKit, parent, name)
	{
		public ServiceBusNamespaceKit Parent { get; } = parent;

		protected override IResourceBuilder<AzureServiceBusTopicResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => Parent.ResourceBuilder.AddServiceBusTopic(Name, Name);
	}

	sealed class ServiceBusSubscriptionKit(ModelHostKit hostKit, ServiceBusTopicKit parent, string name)
		: ResourceKitBase<ModelHostKit, AzureServiceBusSubscriptionResource>(hostKit, parent, name)
	{
		public ServiceBusTopicKit Parent { get; } = parent;

		protected override IResourceBuilder<AzureServiceBusSubscriptionResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => Parent.ResourceBuilder.AddServiceBusSubscription(Name, Name);
	}

	sealed class ServiceBusQueueKit(ModelHostKit hostKit, ServiceBusNamespaceKit parent, string name)
		: ResourceKitBase<ModelHostKit, AzureServiceBusQueueResource>(hostKit, parent, name)
	{
		public ServiceBusNamespaceKit Parent { get; } = parent;

		protected override IResourceBuilder<AzureServiceBusQueueResource> BuildResource(
			IDistributedApplicationBuilder builder
		) => Parent.ResourceBuilder.AddServiceBusQueue(Name, Name);
	}
}
