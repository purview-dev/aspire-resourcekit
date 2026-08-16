using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

public sealed class HostResourceKitTests
{
	[Test]
	public async Task Build_WhenDisabledByServices_DoesNotCallBuild()
	{
		var builder = DistributedApplication.CreateBuilder();
		var hostApp = new TestHostKit();
		var resource = new TestResourceKit(hostApp, enabled: false);

		resource.Build(builder);

		await Assert.That(resource.IsEnabled).IsFalse();
		await Assert.That(resource.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_WhenEnabled_CallsBuildAndSetsResourceBuilder()
	{
		var builder = DistributedApplication.CreateBuilder();
		var hostApp = new TestHostKit();
		var resource = new TestResourceKit(hostApp, enabled: true);

		resource.Build(builder);

		await Assert.That(resource.IsEnabled).IsTrue();
		await Assert.That(resource.BuildCalled).IsTrue();
		await Assert.That(resource.ResourceBuilder).IsNotNull();
	}

	[Test]
	public async Task Configure_WhenDisabled_DoesNotCallConfigure()
	{
		var builder = DistributedApplication.CreateBuilder();
		var hostApp = new TestHostKit();
		var resource = new TestResourceKit(hostApp, enabled: false);

		resource.Build(builder);
		resource.Configure();

		await Assert.That(resource.ConfigureCalled).IsFalse();
	}

	[Test]
	public async Task IsResourceEnabled_WithServices_DelegatesToBuilderOnlyOverloadByDefault()
	{
		var builder = DistributedApplication.CreateBuilder();
		var hostApp = new TestHostKit();
		var resource = new DelegatingTestResourceKit(hostApp);

		resource.Build(builder);

		await Assert.That(resource.IsEnabled).IsFalse();
	}

	[Test]
	public async Task Configure_WhenResourceAddedAfterBuild_CallsConfigureOnAddedResource()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var hostApp = new TestHostKit();
		var resource = new TestResourceKit(hostApp, enabled: true);

		hostApp.AddResource(resource);
		hostApp.Build(builder);

		// Act
		hostApp.Configure();

		// Assert
		await Assert.That(resource.ConfigureCalled).IsTrue();
	}

	[Test]
	public async Task AddResource_WhenSealed_ThrowsInvalidOperationException()
	{
		// Arrange
		var hostApp = new TestHostKit();
		var resource = new TestResourceKit(hostApp);
		hostApp.Build(IDistributedApplicationBuilder.Mock());

		// Act/Assert
		await Assert.That(() => hostApp.AddResource(resource)).ThrowsExactly<InvalidOperationException>();
	}

	sealed class TestHostKit : HostKitBase<TestHostKit>;

	sealed class TestResourceKit(TestHostKit hostKit, bool enabled = true)
		: ResourceKitBase<TestHostKit, ParameterResource>(hostKit, "test")
	{
		public bool BuildCalled { get; private set; }

		public bool ConfigureCalled { get; private set; }

		protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) => enabled;

		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		)
		{
			BuildCalled = true;
			return builder.AddParameter(Name, "value", secret: false);
		}

		protected override void ConfigureResource()
		{
			ConfigureCalled = true;
		}
	}

	sealed class DelegatingTestResourceKit(TestHostKit hostKit)
		: ResourceKitBase<TestHostKit, ParameterResource>(hostKit, "test")
	{
		// Does NOT override IsResourceEnabled(builder, services) — should delegate to builder-only.
		protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) => false;

		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		) => builder.AddParameter(Name, "value", secret: false);
	}
}
