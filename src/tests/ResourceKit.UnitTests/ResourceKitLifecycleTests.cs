using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Covers the <c>Build</c>/<c>Configure</c> lifecycle contract shared by <c>HostKitBase&lt;THostKit&gt;</c>
/// and <c>ResourceKitBase&lt;THostKit, TResource&gt;</c>: enablement gating, the order the hooks run in,
/// and how <see cref="ResourceKitMode"/> excludes a kit from an execution.
/// </summary>
/// <remarks>
/// A scope-named suite rather than a <c>{ClassUnderTest}Tests</c> one, because no single type owns the
/// contract under test - the resource kit decides, and the host kit fans the call out over its
/// resources.
/// </remarks>
public sealed class ResourceKitLifecycleTests
{
	[Test]
	public async Task Build_WhenDisabledByServices_DoesNotCallBuild()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		TestResourceKit resource = new(hostApp, enabled: false);

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsFalse();
		await Assert.That(resource.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_WhenIsEnabledIsFalse_DoesNotCallIsResourceEnabled()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		TrackingResourceKit resource = new(hostApp) { IsEnabled = false };

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsFalse();
		await Assert.That(resource.IsResourceEnabledCalled).IsFalse();
		await Assert.That(resource.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_WhenIsEnabledIsTrue_CallsIsResourceEnabled()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		TrackingResourceKit resource = new(hostApp) { IsEnabled = true };

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsTrue();
		await Assert.That(resource.IsResourceEnabledCalled).IsTrue();
		await Assert.That(resource.BuildCalled).IsTrue();
	}

	[Test]
	public async Task Build_GivenOptionsSetIsEnabledToFalse_DoesNotCallIsResourceEnabled()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		OptionTrackingResourceKit resource = new(hostApp, new OptionTrackingOptions { IsEnabled = false });

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsFalse();
		await Assert.That(resource.IsResourceEnabledCalled).IsFalse();
		await Assert.That(resource.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenOptionsSetIsEnabledToTrue_StillCallsIsResourceEnabled()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		OptionTrackingResourceKit resource = new(hostApp, new OptionTrackingOptions { IsEnabled = true });

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsTrue();
		await Assert.That(resource.IsResourceEnabledCalled).IsTrue();
		await Assert.That(resource.BuildCalled).IsTrue();
	}

	[Test]
	public async Task Build_WhenEnabled_CallsBuildAndSetsResourceBuilder()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		TestResourceKit resource = new(hostApp, enabled: true);

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsTrue();
		await Assert.That(resource.BuildCalled).IsTrue();
		await Assert.That(resource.ResourceBuilder).IsNotNull();
	}

	[Test]
	public async Task Configure_WhenDisabled_DoesNotCallConfigure()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		TestResourceKit resource = new(hostApp, enabled: false);

		// Act
		resource.Build(builder);
		resource.Configure();

		// Assert
		await Assert.That(resource.ConfigureCalled).IsFalse();
	}

	[Test]
	public async Task IsResourceEnabled_WithServices_DelegatesToBuilderOnlyOverloadByDefault()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		DelegatingTestResourceKit resource = new(hostApp);

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsFalse();
	}

	[Test]
	public async Task Configure_WhenResourceAddedAfterBuild_CallsConfigureOnAddedResource()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		TestResourceKit resource = new(hostApp, enabled: true);

		hostApp.AddResource(resource);
		hostApp.Build(builder);

		// Act
		hostApp.Configure();

		// Assert
		await Assert.That(resource.ConfigureCalled).IsTrue();
	}

	[Test]
	public async Task Build_GivenModeExcludedByExecutionOperation_DoesNotInvokeHookOrBuild()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);
		TestHostKit hostApp = new();
		ModeTrackingResourceKit resource = new(hostApp, ResourceKitMode.Publish);

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsFalse();
		await Assert.That(resource.IsResourceEnabledCalled).IsFalse();
		await Assert.That(resource.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenModeIncludedByExecutionOperation_InvokesHookAndBuilds()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(
			DistributedApplicationOperation.Run,
			environmentName: "Development"
		);
		TestHostKit hostApp = new();
		ModeTrackingResourceKit resource = new(hostApp, ResourceKitMode.LocalDevelopment);

		// Act
		resource.Build(builder);

		// Assert
		await Assert.That(resource.IsEnabled).IsTrue();
		await Assert.That(resource.IsResourceEnabledCalled).IsTrue();
		await Assert.That(resource.BuildCalled).IsTrue();
	}

	[Test]
	public async Task Configure_GivenModeExcludedByExecutionOperation_DoesNotCallConfigure()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Publish);
		TestHostKit hostApp = new();
		ModeTrackingResourceKit resource = new(hostApp, ResourceKitMode.LocalDevelopment);

		resource.Build(builder);

		// Act
		resource.Configure();

		// Assert
		await Assert.That(resource.ConfigureCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenHostKitModeExcludedByExecutionOperation_DisablesAndSkipsEveryResource()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);
		ModeHostKit hostApp = new(ResourceKitMode.Publish);
		ModeHostResourceKit resource = new(hostApp);
		hostApp.AddResource(resource);

		// Act
		hostApp.Build(builder);
		hostApp.Configure();

		// Assert
		await Assert.That(resource.IsEnabled).IsFalse();
		await Assert.That(resource.BuildCalled).IsFalse();
		await Assert.That(resource.ConfigureCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenHostKitModeIncludedByExecutionOperation_BuildsEveryResource()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);
		ModeHostKit hostApp = new(ResourceKitMode.Run);
		ModeHostResourceKit resource = new(hostApp);
		hostApp.AddResource(resource);

		// Act
		hostApp.Build(builder);
		hostApp.Configure();

		// Assert
		await Assert.That(resource.IsEnabled).IsTrue();
		await Assert.That(resource.BuildCalled).IsTrue();
		await Assert.That(resource.ConfigureCalled).IsTrue();
	}

	[Test]
	public async Task AddResource_WhenSealed_ThrowsInvalidOperationException()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostApp = new();
		TestResourceKit resource = new(hostApp);
		hostApp.Build(builder);

		// Act
		var exception = await Assert
			.That(() => hostApp.AddResource(resource))
			.ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception).IsNotNull();
	}

	sealed class TestHostKit : HostKitBase<TestHostKit>;

	sealed class ModeHostKit(ResourceKitMode mode) : HostKitBase<ModeHostKit>
	{
		protected override ResourceKitMode Mode => mode;
	}

	sealed class ModeHostResourceKit(ModeHostKit hostKit)
		: ResourceKitBase<ModeHostKit, ParameterResource>(hostKit, "test")
	{
		public bool BuildCalled { get; private set; }

		public bool ConfigureCalled { get; private set; }

		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		)
		{
			BuildCalled = true;
			return builder.AddParameter(Name, "value", secret: false);
		}

		protected override void ConfigureResource() => ConfigureCalled = true;
	}

	sealed class ModeTrackingResourceKit(TestHostKit hostKit, ResourceKitMode mode)
		: ResourceKitBase<TestHostKit, ParameterResource>(hostKit, "test")
	{
		public bool IsResourceEnabledCalled { get; private set; }

		public bool BuildCalled { get; private set; }

		public bool ConfigureCalled { get; private set; }

		protected override ResourceKitMode Mode => mode;

		protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder)
		{
			IsResourceEnabledCalled = true;
			return true;
		}

		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		)
		{
			BuildCalled = true;
			return builder.AddParameter(Name, "value", secret: false);
		}

		protected override void ConfigureResource() => ConfigureCalled = true;
	}

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

	sealed class TrackingResourceKit(TestHostKit hostKit)
		: ResourceKitBase<TestHostKit, ParameterResource>(hostKit, "test")
	{
		public bool IsResourceEnabledCalled { get; private set; }

		public bool BuildCalled { get; private set; }

		protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder)
		{
			IsResourceEnabledCalled = true;
			return true;
		}

		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		)
		{
			BuildCalled = true;
			return builder.AddParameter(Name, "value", secret: false);
		}
	}

	sealed class OptionTrackingOptions
	{
		public bool IsEnabled { get; set; } = true;
	}

	sealed class OptionTrackingResourceKit : ResourceKitBase<TestHostKit, ParameterResource>
	{
		public OptionTrackingResourceKit(TestHostKit hostKit, OptionTrackingOptions options)
			: base(hostKit, "test")
		{
			Options = options;
			IsEnabled = options.IsEnabled;
		}

		public OptionTrackingOptions Options { get; }

		public bool IsResourceEnabledCalled { get; private set; }

		public bool BuildCalled { get; private set; }

		protected override bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder)
		{
			IsResourceEnabledCalled = true;
			return true;
		}

		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		)
		{
			BuildCalled = true;
			return builder.AddParameter(Name, "value", secret: false);
		}
	}
}
