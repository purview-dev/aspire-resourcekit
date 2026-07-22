using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

public sealed class HostAppResourceTests
{
	[Test]
	public async Task Build_WhenDisabledByServices_DoesNotCallBuild()
	{
		var builder = DistributedApplication.CreateBuilder();
		var resource = new TestAppResource(enabled: false);

		resource.Build(builder);

		await Assert.That(resource.IsEnabled).IsFalse();
		await Assert.That(resource.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_WhenEnabled_CallsBuildAndSetsResourceBuilder()
	{
		var builder = DistributedApplication.CreateBuilder();
		var resource = new TestAppResource(enabled: true);

		resource.Build(builder);

		await Assert.That(resource.IsEnabled).IsTrue();
		await Assert.That(resource.BuildCalled).IsTrue();
		await Assert.That(resource.ResourceBuilder).IsNotNull();
	}

	[Test]
	public async Task Configure_WhenDisabled_DoesNotCallConfigure()
	{
		var builder = DistributedApplication.CreateBuilder();
		var hostApp = new TestHostApp();
		var resource = new TestAppResource(enabled: false);

		resource.Build(builder);
		resource.Configure(hostApp);

		await Assert.That(resource.ConfigureCalled).IsFalse();
	}

	[Test]
	public async Task IsResourceEnabled_WithServices_DelegatesToBuilderOnlyOverloadByDefault()
	{
		var builder = DistributedApplication.CreateBuilder();
		var resource = new DelegatingTestAppResource();

		resource.Build(builder);

		await Assert.That(resource.IsEnabled).IsFalse();
	}

	sealed class TestHostApp;

	sealed class TestAppResource(bool enabled = true) : HostAppResource<TestHostApp, ParameterResource>
	{
		public bool BuildCalled { get; private set; }

		public bool ConfigureCalled { get; private set; }

		public override string Name => "test";

		protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) => enabled;

		protected override IResourceBuilder<ParameterResource> BuildResource(IDistributedApplicationBuilder builder)
		{
			BuildCalled = true;
			return builder.AddParameter(Name, "value", secret: false);
		}

		protected override void ConfigureResource(TestHostApp app)
		{
			ConfigureCalled = true;
		}
	}

	sealed class DelegatingTestAppResource : HostAppResource<TestHostApp, ParameterResource>
	{
		public override string Name => "test";

		// Does NOT override IsResourceEnabled(builder, services) — should delegate to builder-only.
		protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) => false;

		protected override IResourceBuilder<ParameterResource> BuildResource(IDistributedApplicationBuilder builder) =>
			builder.AddParameter(Name, "value", secret: false);
	}
}
