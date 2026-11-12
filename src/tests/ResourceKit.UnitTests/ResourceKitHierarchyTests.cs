using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Covers the parent/child ownership contract added for issue #57: construction ordering, the parent builder
/// being available while a child builds, graph validation, inherited enablement, and lifecycle failure policy.
/// </summary>
public sealed class ResourceKitHierarchyTests
{
	[Test]
	public async Task Build_GivenParentAndChild_BuildsParentFirstAndExposesParentBuilder()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit parent = new(host, "parent", events);
		InstrumentedKit child = new(host, parent, "child", events);

		// Register the child first on purpose; the host must still construct and build the parent first.
		host.AddResource(child);
		host.AddResource(parent);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert
		await Assert
			.That(events)
			.IsEquivalentTo(["Build:parent", "Build:child", "Configure:parent", "Configure:child"]);
		await Assert.That(child.ParentKit).IsSameReferenceAs(parent);
		await Assert.That(child.ResourceBuilder.Resource.Name).IsEqualTo("child");
		await Assert.That(parent.ResourceBuilder.Resource.Name).IsEqualTo("parent");
		await Assert.That(builder.Resources.Contains(parent.ResourceBuilder.Resource)).IsTrue();
		await Assert.That(builder.Resources.Contains(child.ResourceBuilder.Resource)).IsTrue();
	}

	[Test]
	public async Task Build_GivenDeepHierarchy_BuildsEveryAncestorBeforeItsDescendants()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit root = new(host, "root", events);
		InstrumentedKit level1 = new(host, root, "level1", events);
		InstrumentedKit level2 = new(host, level1, "level2", events);
		InstrumentedKit level3 = new(host, level2, "level3", events);
		InstrumentedKit level4 = new(host, level3, "level4", events);

		host.AddResource(level4);
		host.AddResource(level3);
		host.AddResource(level2);
		host.AddResource(level1);
		host.AddResource(root);

		// Act
		host.Build(builder);

		// Assert
		await Assert
			.That(events)
			.IsEquivalentTo(["Build:root", "Build:level1", "Build:level2", "Build:level3", "Build:level4"]);
	}

	[Test]
	public async Task Build_GivenParentDisabled_DoesNotBuildOrInvokeHookOnDescendants()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit parent = new(host, "parent", events) { IsEnabled = false };
		InstrumentedKit child = new(host, parent, "child", events);
		InstrumentedKit grandchild = new(host, child, "grandchild", events);

		host.AddResource(parent);
		host.AddResource(child);
		host.AddResource(grandchild);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(events).IsEmpty();
		await Assert.That(parent.IsEnabled).IsFalse();
		await Assert.That(child.IsEnabled).IsFalse();
		await Assert.That(grandchild.IsEnabled).IsFalse();
		await Assert.That(child.HookCalled).IsFalse();
		await Assert.That(grandchild.HookCalled).IsFalse();
		await Assert.That(child.ConfiguredIsEnabled).IsTrue();
		await Assert.That(child.EffectiveIsEnabled).IsFalse();
	}

	[Test]
	public async Task Build_GivenParentModeExcluded_DisablesEveryDescendant()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit parent = new(host, "parent", events, mode: ResourceKitMode.Publish);
		InstrumentedKit child = new(host, parent, "child", events);

		host.AddResource(parent);
		host.AddResource(child);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(parent.IsEnabled).IsFalse();
		await Assert.That(child.IsEnabled).IsFalse();
		await Assert.That(child.HookCalled).IsFalse();
		await Assert.That(events).IsEmpty();
	}

	[Test]
	public async Task Build_GivenChildModeExcluded_LeavesParentAndSiblingEligible()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit parent = new(host, "parent", events);
		InstrumentedKit excluded = new(host, parent, "excluded", events, mode: ResourceKitMode.Publish);
		InstrumentedKit sibling = new(host, parent, "sibling", events);

		host.AddResource(parent);
		host.AddResource(excluded);
		host.AddResource(sibling);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(parent.IsEnabled).IsTrue();
		await Assert.That(excluded.IsEnabled).IsFalse();
		await Assert.That(sibling.IsEnabled).IsTrue();
		await Assert.That(excluded.HookCalled).IsFalse();
		await Assert.That(events).IsEquivalentTo(["Build:parent", "Build:sibling"]);
	}

	[Test]
	public async Task Build_GivenChildHookDisabled_DisablesChildAndGrandchildOnly()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit parent = new(host, "parent", events);
		InstrumentedKit child = new(host, parent, "child", events, hook: false);
		InstrumentedKit grandchild = new(host, child, "grandchild", events);

		host.AddResource(parent);
		host.AddResource(child);
		host.AddResource(grandchild);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(parent.IsEnabled).IsTrue();
		await Assert.That(child.IsEnabled).IsFalse();
		await Assert.That(grandchild.IsEnabled).IsFalse();
		await Assert.That(grandchild.HookCalled).IsFalse();
		await Assert.That(events).IsEquivalentTo(["Build:parent"]);
	}

	[Test]
	public async Task Build_GivenParentBuildThrows_ChildCannotBuildOrAccessItsBuilder()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit parent = new(host, "parent", events, throwOnBuild: true);
		InstrumentedKit child = new(host, parent, "child", events);

		host.AddResource(parent);
		host.AddResource(child);

		// Act
		var exception = await Assert.That(() => host.Build(builder)).ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception!.Message).Contains("parent");
		await Assert.That(child.BuildCalled).IsFalse();
		await Assert.That(() => _ = child.ResourceBuilder).ThrowsExactly<InvalidOperationException>();
	}

	[Test]
	public async Task Build_GivenParentFailsDuringConfigure_ChildDoesNotConfigure()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit parent = new(host, "parent", events, throwOnConfigure: true);
		InstrumentedKit child = new(host, parent, "child", events);

		host.AddResource(parent);
		host.AddResource(child);
		host.Build(builder);

		// Act
		await Assert.That(host.Configure).ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(child.ConfigureCalled).IsFalse();
	}

	[Test]
	public async Task Build_WhenCalledTwice_DoesNotCreateResourcesTwice()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		List<string> events = [];
		TestHostKit host = new(events);
		InstrumentedKit parent = new(host, "parent", events);
		InstrumentedKit child = new(host, parent, "child", events);

		host.AddResource(parent);
		host.AddResource(child);

		// Act
		host.Build(builder);
		host.Build(builder);
		host.Configure();
		host.Configure();

		// Assert
		await Assert
			.That(events)
			.IsEquivalentTo(["Build:parent", "Build:child", "Configure:parent", "Configure:child"]);
		await Assert.That(parent.BuildCount).IsEqualTo(1);
		await Assert.That(child.ConfigureCount).IsEqualTo(1);
	}

	[Test]
	public async Task Build_GivenSelfParenting_ThrowsBeforeCreatingResources()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit host = new([]);
		MutableKit kit = new() { HostKit = host, Name = "self" };
		kit.ParentKit = kit;
		host.AddResource(kit);

		// Act
		var exception = await Assert.That(() => host.Build(builder)).ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception!.Message).Contains("own parent");
		await Assert.That(kit.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenParentCycle_ThrowsBeforeCreatingResources()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit host = new([]);
		MutableKit first = new() { HostKit = host, Name = "first" };
		MutableKit second = new() { HostKit = host, Name = "second" };
		first.ParentKit = second;
		second.ParentKit = first;
		host.AddResource(first);
		host.AddResource(second);

		// Act
		var exception = await Assert.That(() => host.Build(builder)).ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception!.Message).Contains("first");
		await Assert.That(exception!.Message).Contains("second");
		await Assert.That(exception!.Message).Contains("cycle");
		await Assert.That(first.BuildCalled).IsFalse();
		await Assert.That(second.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenUnregisteredParent_ThrowsBeforeCreatingResources()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit host = new([]);
		MutableKit parent = new() { HostKit = host, Name = "parent" };
		MutableKit child = new() { HostKit = host, Name = "child" };
		child.ParentKit = parent;
		host.AddResource(child);

		// Act
		var exception = await Assert.That(() => host.Build(builder)).ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception!.Message).Contains("not registered");
		await Assert.That(child.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenDuplicateRegistration_ThrowsBeforeCreatingResources()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit host = new([]);
		InstrumentedKit kit = new(host, "kit", []);
		host.AddResource(kit);
		host.AddResource(kit);

		// Act
		var exception = await Assert.That(() => host.Build(builder)).ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception!.Message).Contains("more than once");
	}

	[Test]
	public async Task Build_GivenForeignHostParent_ThrowsBeforeCreatingResources()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit host = new([]);
		TestHostKit otherHost = new([]);
		InstrumentedKit parent = new(otherHost, "parent", []);

		// The child's parent has to be the parent instance, but its HostKit differs.
		InstrumentedKit child = new(host, parent, "child", []);
		host.AddResource(parent);
		host.AddResource(child);

		// Act
		var exception = await Assert.That(() => host.Build(builder)).ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception!.Message).Contains("different host kit");
	}

	[Test]
	[Timeout(30_000)]
	public async Task Build_GivenDeepGraph_ValidatesAndBuildsWithoutStackOverflow(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		const int depth = 2_000;
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);
		TestHostKit host = new([]);
		MutableKit? previous = null;
		List<MutableKit> resources = [];

		for (var index = 0; index < depth; index++)
		{
			MutableKit kit = new() { HostKit = host, Name = $"kit-{index:D4}" };
			if (previous is not null)
				kit.ParentKit = previous;

			resources.Add(kit);
			previous = kit;
		}

		// Register in reverse so the host has to resolve the entire chain.
		for (var index = resources.Count - 1; index >= 0; index--)
			host.AddResource(resources[index]);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(resources.All(static kit => kit.BuildCalled)).IsTrue();
	}

	[Test]
	[Timeout(30_000)]
	public async Task Build_GivenWideGraph_BuildsEveryChildOnce(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		const int parents = 3;
		const int children = 100;
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);
		TestHostKit host = new([]);
		List<MutableKit> roots = [];
		List<MutableKit> all = [];

		for (var parentIndex = 0; parentIndex < parents; parentIndex++)
		{
			MutableKit root = new() { HostKit = host, Name = $"parent-{parentIndex}" };
			roots.Add(root);
			all.Add(root);

			for (var childIndex = 0; childIndex < children; childIndex++)
			{
				MutableKit child = new()
				{
					HostKit = host,
					Name = $"child-{parentIndex}-{childIndex}",
					ParentKit = root,
				};
				all.Add(child);
			}
		}

		foreach (var kit in all)
			host.AddResource(kit);

		// Act
		host.Build(builder);

		// Assert
		await Assert.That(all.All(static kit => kit.BuildCalled)).IsTrue();
		await Assert
			.That(all.Select(static kit => kit.Name).Distinct(StringComparer.Ordinal).Count())
			.IsEqualTo(all.Count);
	}

	[Test]
	public async Task Build_GivenUnbuiltKit_ResourceBuilderThrowsAClearException()
	{
		// Arrange
		TestHostKit host = new([]);
		InstrumentedKit kit = new(host, "kit", []);

		// Act
		var exception = await Assert.That(() => _ = kit.ResourceBuilder).ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception!.Message).Contains("has not been built");
	}

	sealed class TestHostKit(List<string> events) : HostKitBase<TestHostKit>
	{
		public List<string> Events { get; } = events;
	}

	sealed class InstrumentedKit : ResourceKitBase<TestHostKit, TestResource>
	{
		readonly bool _hook;
		readonly ResourceKitMode _mode;
		readonly bool _throwOnBuild;
		readonly bool _throwOnConfigure;
		readonly List<string> _events;

		public InstrumentedKit(
			TestHostKit hostKit,
			string name,
			List<string> events,
			bool hook = true,
			ResourceKitMode mode = ResourceKitMode.All,
			bool throwOnBuild = false,
			bool throwOnConfigure = false
		)
			: base(hostKit, name)
		{
			_hook = hook;
			_mode = mode;
			_throwOnBuild = throwOnBuild;
			_throwOnConfigure = throwOnConfigure;
			_events = events;
		}

		public InstrumentedKit(
			TestHostKit hostKit,
			InstrumentedKit parent,
			string name,
			List<string> events,
			bool hook = true,
			ResourceKitMode mode = ResourceKitMode.All,
			bool throwOnBuild = false,
			bool throwOnConfigure = false
		)
			: this(hostKit, name, events, hook, mode, throwOnBuild, throwOnConfigure)
		{
			ParentKit = parent;
		}

		public bool BuildCalled { get; private set; }

		public int BuildCount { get; private set; }

		public bool ConfigureCalled { get; private set; }

		public int ConfigureCount { get; private set; }

		public bool HookCalled { get; private set; }

		protected override ResourceKitMode Mode => _mode;

		protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder)
		{
			HookCalled = true;
			return _hook;
		}

		protected override IResourceBuilder<TestResource> BuildResource(IDistributedApplicationBuilder builder)
		{
			BuildCalled = true;
			BuildCount++;
			_events.Add($"Build:{Name}");

			if (_throwOnBuild)
				throw new InvalidOperationException($"The '{Name}' parent failed to build.");

			return builder.AddResource(new TestResource(Name));
		}

		protected override void ConfigureResource()
		{
			ConfigureCalled = true;
			ConfigureCount++;
			_events.Add($"Configure:{Name}");

			if (_throwOnConfigure)
				throw new InvalidOperationException($"The '{Name}' parent failed to configure.");
		}
	}

	sealed class TestResource(string name) : IResource
	{
		public string Name { get; } = name;

		public ResourceAnnotationCollection Annotations { get; } = [];
	}

	// A hand-written IResourceKit used to prove the host validates graphs that the generator never saw.
	sealed class MutableKit : IResourceKit<TestHostKit>
	{
		public TestHostKit HostKit { get; init; } = null!;

		public string Name { get; init; } = string.Empty;

		public bool IsEnabled { get; set; } = true;

		public IResourceKit<TestHostKit>? ParentKit { get; set; }

		public bool BuildCalled { get; private set; }

		public void Build(IDistributedApplicationBuilder builder) => BuildCalled = true;

		public void Configure() { }
	}
}
