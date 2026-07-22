using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Purview.Aspire.ResourceIsolation;

public sealed class HostAppResourceTests
{
	[Test]
	public async Task BuildResource_WhenDisabledByServices_DoesNotCallBuild()
	{
		var builder = DistributedApplication.CreateBuilder();
		var services = new ServiceCollection().BuildServiceProvider();
		var resource = new TestAppResource(enabled: false);

		resource.BuildResource(builder, services);

		await Assert.That(resource.IsEnabled).IsFalse();
		await Assert.That(resource.BuildCalled).IsFalse();
	}

	[Test]
	public async Task BuildResource_WhenEnabled_CallsBuildAndSetsResourceBuilder()
	{
		var builder = DistributedApplication.CreateBuilder();
		var services = new ServiceCollection().BuildServiceProvider();
		var resource = new TestAppResource(enabled: true);

		resource.BuildResource(builder, services);

		await Assert.That(resource.IsEnabled).IsTrue();
		await Assert.That(resource.BuildCalled).IsTrue();
		await Assert.That(resource.ResourceBuilder).IsNotNull();
	}

	[Test]
	public async Task ConfigureResource_WhenDisabled_DoesNotCallConfigure()
	{
		var builder = DistributedApplication.CreateBuilder();
		var services = new ServiceCollection().BuildServiceProvider();
		var hostApp = new TestHostApp();
		var resource = new TestAppResource(enabled: false);

		resource.BuildResource(builder, services);
		resource.ConfigureResource(hostApp, services);

		await Assert.That(resource.ConfigureCalled).IsFalse();
	}

	[Test]
	public async Task ConfigureResource_WhenEnabled_CallsConfigureWithServices()
	{
		var builder = DistributedApplication.CreateBuilder();
		var services = new ServiceCollection().BuildServiceProvider();
		var hostApp = new TestHostApp();
		var resource = new TestAppResource(enabled: true);

		resource.BuildResource(builder, services);
		resource.ConfigureResource(hostApp, services);

		await Assert.That(resource.ConfigureCalled).IsTrue();
		await Assert.That(resource.ConfigureServicesReceived).IsNotNull();
	}

	[Test]
	public async Task IsResourceEnabled_WithServices_DelegatesToBuilderOnlyOverloadByDefault()
	{
		var builder = DistributedApplication.CreateBuilder();
		var services = new ServiceCollection().BuildServiceProvider();
		var resource = new DelegatingTestAppResource();

		resource.BuildResource(builder, services);

		await Assert.That(resource.IsEnabled).IsFalse();
	}

	sealed class TestHostApp;

	sealed class TestAppResource(bool enabled = true) : HostAppResource<TestHostApp, ParameterResource>
	{
		public bool BuildCalled { get; private set; }
		public bool ConfigureCalled { get; private set; }
		public IServiceProvider? ConfigureServicesReceived { get; private set; }

		public override string Name => "test";

		protected override bool IsResourceEnabled(
			[NotNull] IDistributedApplicationBuilder builder,
			IServiceProvider services
		) => enabled;

		protected override IResourceBuilder<ParameterResource> Build(IDistributedApplicationBuilder builder)
		{
			BuildCalled = true;
			return builder.AddParameter(Name, "value", secret: false);
		}

		protected override void Configure(TestHostApp app, IServiceProvider services)
		{
			ConfigureCalled = true;
			ConfigureServicesReceived = services;
		}
	}

	sealed class DelegatingTestAppResource : HostAppResource<TestHostApp, ParameterResource>
	{
		public override string Name => "test";

		// Does NOT override IsResourceEnabled(builder, services) — should delegate to builder-only.
		protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder) => false;

		protected override IResourceBuilder<ParameterResource> Build(IDistributedApplicationBuilder builder) =>
			builder.AddParameter(Name, "value", secret: false);
	}
}
