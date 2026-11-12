using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// The enablement matrix for a two-level hierarchy: host mode, parent and child modes, the configured flags,
/// and the runtime hooks. It proves that a disabled ancestor suppresses descendant hooks and builds.
/// </summary>
public sealed class ResourceKitHierarchyEnablementTests
{
	[Test]
	[Arguments(false, true, true, true, true, false, false)]
	[Arguments(true, false, true, true, true, false, false)]
	[Arguments(true, true, false, true, true, true, false)]
	[Arguments(true, true, true, false, true, false, false)]
	[Arguments(true, true, true, true, false, true, false)]
	[Arguments(true, true, true, true, true, true, true)]
	[Arguments(true, true, false, true, false, true, false)]
	public async Task Build_GivenEnablementCombination_BuildsExactlyWhatTheGatesAllow(
		bool hostEnabled,
		bool parentEnabled,
		bool childEnabled,
		bool parentHook,
		bool childHook,
		bool parentExpected,
		bool childExpected
	)
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);
		EnablementHostKit host = new(hostEnabled ? ResourceKitMode.All : ResourceKitMode.Publish);
		EnablementKit parent = new(host, "parent", hook: parentHook) { IsEnabled = parentEnabled };
		EnablementKit child = new(host, parent, "child", hook: childHook) { IsEnabled = childEnabled };
		host.AddResource(parent);
		host.AddResource(child);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert
		await Assert.That(parent.BuildCalled).IsEqualTo(parentExpected);
		await Assert.That(parent.ConfigureCalled).IsEqualTo(parentExpected);
		await Assert.That(child.BuildCalled).IsEqualTo(childExpected);
		await Assert.That(child.ConfigureCalled).IsEqualTo(childExpected);

		// The separate tests below assert hook suppression; this matrix asserts the resulting build decisions.
	}

	[Test]
	public async Task Build_GivenParentExcludedByMode_DoesNotInvokeChildHook()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);
		EnablementHostKit host = new(ResourceKitMode.All);
		EnablementKit parent = new(host, "parent", mode: ResourceKitMode.Publish);
		EnablementKit child = new(host, parent, "child");
		host.AddResource(parent);
		host.AddResource(child);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(child.HookCalled).IsFalse();
		await Assert.That(child.BuildCalled).IsFalse();
		await Assert.That(child.EffectiveIsEnabled).IsFalse();
	}

	[Test]
	public async Task Build_GivenReEnabledParentInAFreshHost_ChildKeepsItsConfiguredValue()
	{
		// Arrange
		EnablementHostKit firstHost = new(ResourceKitMode.All);
		EnablementKit firstParent = new(firstHost, "parent") { IsEnabled = false };
		EnablementKit firstChild = new(firstHost, firstParent, "child");
		firstHost.AddResource(firstParent);
		firstHost.AddResource(firstChild);
		firstHost.Build(TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run));

		EnablementHostKit secondHost = new(ResourceKitMode.All);
		EnablementKit secondParent = new(secondHost, "parent");
		EnablementKit secondChild = new(secondHost, secondParent, "child");
		secondHost.AddResource(secondParent);
		secondHost.AddResource(secondChild);

		// Act
		secondHost.Build(TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run));

		// Assert
		await Assert.That(firstChild.EffectiveIsEnabled).IsFalse();
		await Assert.That(firstChild.ConfiguredIsEnabled).IsTrue();
		await Assert.That(secondChild.EffectiveIsEnabled).IsTrue();
		await Assert.That(secondChild.BuildCalled).IsTrue();
	}

	sealed class EnablementHostKit(ResourceKitMode mode) : HostKitBase<EnablementHostKit>
	{
		protected override ResourceKitMode Mode => mode;
	}

	sealed class EnablementKit : ResourceKitBase<EnablementHostKit, EnablementResource>
	{
		readonly bool _hook;
		readonly ResourceKitMode _mode;

		public EnablementKit(
			EnablementHostKit hostKit,
			string name,
			bool hook = true,
			ResourceKitMode mode = ResourceKitMode.All
		)
			: base(hostKit, name)
		{
			_hook = hook;
			_mode = mode;
		}

		public EnablementKit(
			EnablementHostKit hostKit,
			EnablementKit parent,
			string name,
			bool hook = true,
			ResourceKitMode mode = ResourceKitMode.All
		)
			: this(hostKit, name, hook, mode)
		{
			ParentKit = parent;
		}

		public bool HookCalled { get; private set; }

		public bool BuildCalled { get; private set; }

		public bool ConfigureCalled { get; private set; }

		protected override ResourceKitMode Mode => _mode;

		protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder)
		{
			HookCalled = true;
			return _hook;
		}

		protected override IResourceBuilder<EnablementResource> BuildResource(IDistributedApplicationBuilder builder)
		{
			BuildCalled = true;
			return default!;
		}

		protected override void ConfigureResource() => ConfigureCalled = true;
	}

	sealed class EnablementResource : IResource
	{
		public string Name => nameof(EnablementResource);

		public ResourceAnnotationCollection Annotations { get; } = [];
	}
}
