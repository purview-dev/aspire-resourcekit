using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Covers the interaction between the parent/child ownership graph and ResourceKit groups: inherited group
/// membership, dashboard placement, disabled hierarchies, and the precedence between hierarchy and groups.
/// </summary>
public sealed class ResourceKitHierarchyGroupTests
{
	[Test]
	public async Task Build_GivenChildWithoutAGroup_InheritsTheParentsPrimaryGroup()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		HierarchyHostKit host = new();
		ResourceKitGroup<HierarchyHostKit> data = new("data");
		HierarchyTestKit parent = new(host, "parent", []);
		HierarchyTestKit child = new(host, parent, "child", []);
		host.AddResource(parent, data);
		host.AddResource(child);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert
		await Assert.That(host.GetGroups(child)).Contains(data);
		await Assert.That(data.Resources).Contains(child);
		await Assert.That(data.DashboardResource).IsNotNull();
		await Assert.That(ParentOf(child.Resource)).IsSameReferenceAs(data.DashboardResource!.Resource);
		await Assert.That(ParentOf(parent.Resource)).IsSameReferenceAs(data.DashboardResource.Resource);
	}

	[Test]
	public async Task Build_GivenChildWithAnExplicitGroup_DoesNotInheritTheParentsGroup()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		HierarchyHostKit host = new();
		ResourceKitGroup<HierarchyHostKit> data = new("data");
		ResourceKitGroup<HierarchyHostKit> services = new("services");
		HierarchyTestKit parent = new(host, "parent", []);
		HierarchyTestKit child = new(host, parent, "child", []);
		host.AddResource(parent, data);
		host.AddResource(child, services);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert
		var groups = host.GetGroups(child);
		await Assert.That(groups.Count).IsEqualTo(1);
		await Assert.That(groups[0]).IsSameReferenceAs(services);
		await Assert.That(data.Resources).DoesNotContain(child);
		await Assert.That(services.Resources).Contains(child);
		await Assert.That(parent.IsEnabled).IsTrue();
		await Assert.That(child.IsEnabled).IsTrue();

		// The child renders in its own group, not under the parent's group: ResourceKit parentage is not an
		// Aspire visual parent relationship.
		await Assert.That(ParentOf(parent.Resource)).IsSameReferenceAs(data.DashboardResource!.Resource);
		await Assert.That(ParentOf(child.Resource)).IsSameReferenceAs(services.DashboardResource!.Resource);
	}

	[Test]
	public async Task Configure_GivenChildWithAnAspireParentAndAnotherGroup_RendersUnderParentAndLeavesGroupNode()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		HierarchyHostKit host = new();
		ResourceKitGroup<HierarchyHostKit> data = new("data");
		ResourceKitGroup<HierarchyHostKit> services = new("services");
		HierarchyTestKit parent = new(host, "parent", []);
		HierarchyTestKit child = new(host, parent, "child", [], visualParent: true);
		host.AddResource(parent, data);
		host.AddResource(child, services);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert — Aspire has one visual parent, so the existing chain wins over the child's group.
		await Assert.That(ParentOf(child.Resource)).IsSameReferenceAs(parent.Resource);
		await Assert.That(services.DashboardResource).IsNotNull();
		await Assert.That(services.Resources).Contains(child);
	}

	[Test]
	public async Task Build_GivenParentDisabledByItsGroup_DisablesChildEvenInAnEnabledGroup()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		HierarchyHostKit host = new();
		ResourceKitGroup<HierarchyHostKit> data = new("data") { IsEnabled = false };
		ResourceKitGroup<HierarchyHostKit> services = new("services");
		HierarchyTestKit parent = new(host, "parent", []);
		HierarchyTestKit child = new(host, parent, "child", []);
		host.AddResource(parent, data);
		host.AddResource(child, services);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert
		await Assert.That(parent.IsEnabled).IsFalse();
		await Assert.That(child.IsEnabled).IsFalse();
		await Assert.That(child.BuildCalled).IsFalse();
		await Assert.That(data.DashboardResource).IsNull();
		await Assert.That(services.DashboardResource).IsNull();
	}

	[Test]
	public async Task Build_GivenGrandchildWithoutGroups_InheritsThroughTheParentChain()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		HierarchyHostKit host = new();
		ResourceKitGroup<HierarchyHostKit> data = new("data");
		HierarchyTestKit root = new(host, "root", []);
		HierarchyTestKit child = new(host, root, "child", []);
		HierarchyTestKit grandchild = new(host, child, "grandchild", []);
		host.AddResource(root, data);
		host.AddResource(child);
		host.AddResource(grandchild);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert
		await Assert.That(host.GetGroups(child)).Contains(data);
		await Assert.That(host.GetGroups(grandchild)).Contains(data);
		await Assert.That(data.AllResources).Contains(grandchild);
		await Assert.That(data.DashboardResource).IsNotNull();
	}

	[Test]
	public async Task Build_GivenTwoParentsInOneGroup_EachKeepsItsOwnChildrenAndEnablement()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		HierarchyHostKit host = new();
		ResourceKitGroup<HierarchyHostKit> data = new("data");
		HierarchyTestKit first = new(host, "first", []);
		HierarchyTestKit firstChild = new(host, first, "first-child", []);
		HierarchyTestKit second = new(host, "second", []);
		HierarchyTestKit secondChild = new(host, second, "second-child", [], enabled: false);
		host.AddResource(first, data);
		host.AddResource(firstChild);
		host.AddResource(second, data);
		host.AddResource(secondChild);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert
		await Assert.That(firstChild.IsEnabled).IsTrue();
		await Assert.That(second.IsEnabled).IsTrue();
		await Assert.That(secondChild.IsEnabled).IsFalse();
		await Assert.That(firstChild.BuildCalled).IsTrue();
		await Assert.That(secondChild.BuildCalled).IsFalse();
		await Assert.That(data.Resources).Contains(first);
		await Assert.That(data.Resources).Contains(second);
	}

	[Test]
	public async Task Build_GivenUngroupedHierarchy_DoesNotCreateADashboardNode()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		HierarchyHostKit host = new();
		HierarchyTestKit parent = new(host, "parent", []);
		HierarchyTestKit child = new(host, parent, "child", []);
		host.AddResource(parent);
		host.AddResource(child);

		// Act
		host.Build(builder);
		host.Configure();

		// Assert
		await Assert.That(host.GetGroups(child)).IsEmpty();
		await Assert.That(builder.Resources.OfType<ResourceKitGroupResource>()).IsEmpty();
	}

	sealed class HierarchyHostKit : HostKitBase<HierarchyHostKit>;

	sealed class HierarchyTestKit : ResourceKitBase<HierarchyHostKit, HierarchyResource>
	{
		readonly List<string> _events;
		readonly bool _visualParent;

		public HierarchyTestKit(
			HierarchyHostKit hostKit,
			string name,
			List<string> events,
			bool enabled = true,
			bool visualParent = false
		)
			: base(hostKit, name)
		{
			_events = events;
			_visualParent = visualParent;
			IsEnabled = enabled;
		}

		public HierarchyTestKit(
			HierarchyHostKit hostKit,
			HierarchyTestKit parent,
			string name,
			List<string> events,
			bool enabled = true,
			bool visualParent = false
		)
			: this(hostKit, name, events, enabled, visualParent)
		{
			ParentKit = parent;
		}

		public bool BuildCalled { get; private set; }

		protected override IResourceBuilder<HierarchyResource> BuildResource(IDistributedApplicationBuilder builder)
		{
			BuildCalled = true;
			_events.Add($"Build:{Name}");

			var resource = builder.AddResource(new HierarchyResource(Name));
			if (_visualParent)
			{
				resource.Resource.Annotations.Add(
					new ResourceRelationshipAnnotation(((HierarchyTestKit)ParentKit!).Resource, "Parent")
				);
			}

			return resource;
		}
	}

	sealed class HierarchyResource(string name) : IResource
	{
		public string Name { get; } = name;

		public ResourceAnnotationCollection Annotations { get; } = [];
	}

	static IResource? ParentOf(IResource resource) =>
		resource
			.Annotations.OfType<ResourceRelationshipAnnotation>()
			.FirstOrDefault(static annotation => annotation.Type == "Parent")
			?.Resource;
}
