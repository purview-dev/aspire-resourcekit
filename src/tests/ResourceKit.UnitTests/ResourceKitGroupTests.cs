using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Covers <see cref="ResourceKitGroup{THostKit}"/> and how <see cref="HostKitBase{THostKit}"/> uses groups to
/// gate resources and to nest them in the dashboard.
/// </summary>
public sealed class ResourceKitGroupTests
{
	[Test]
	public async Task Constructor_GivenParent_AddsGroupToParentsChildren()
	{
		// Arrange
		ResourceKitGroup<TestHostKit> platform = new("platform");

		// Act
		ResourceKitGroup<TestHostKit> data = new("data", platform);

		// Assert
		await Assert.That(platform.Children).Contains(data);
		await Assert.That(data.Parent).IsSameReferenceAs(platform);
		await Assert.That(data.ToString()).IsEqualTo("platform/data");
	}

	[Test]
	public async Task IsEnabledFor_GivenDisabledAncestor_ReturnsFalse()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		ResourceKitGroup<TestHostKit> platform = new("platform") { IsEnabled = false };
		ResourceKitGroup<TestHostKit> data = new("data", platform);

		// Act
		var isEnabled = data.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsFalse();
	}

	[Test]
	public async Task IsEnabledFor_GivenAncestorModeExcludedByOperation_ReturnsFalse()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);
		ResourceKitGroup<TestHostKit> platform = new("platform") { Mode = ResourceKitMode.Publish };
		ResourceKitGroup<TestHostKit> data = new("data", platform);

		// Act
		var isEnabled = data.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsFalse();
	}

	[Test]
	public async Task IsEnabledFor_GivenEnabledChain_ReturnsTrue()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		ResourceKitGroup<TestHostKit> platform = new("platform");
		ResourceKitGroup<TestHostKit> data = new("data", platform);

		// Act
		var isEnabled = data.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsTrue();
	}

	[Test]
	public async Task AllResources_GivenNestedGroups_ReturnsEachResourceOnce()
	{
		// Arrange
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> platform = new("platform");
		ResourceKitGroup<TestHostKit> data = new("data", platform);
		TestResourceKit postgres = new(hostKit, "postgres");
		TestResourceKit redis = new(hostKit, "redis");
		hostKit.AddResource(postgres, data, platform);
		hostKit.AddResource(redis, data);

		// Act
		var resources = platform.AllResources.ToArray();

		// Assert
		await Assert.That(resources.Length).IsEqualTo(2);
		await Assert.That(resources).Contains(postgres);
		await Assert.That(resources).Contains(redis);
	}

	[Test]
	public async Task GetGroups_GivenSeveralGroups_ReturnsPrimaryGroupFirst()
	{
		// Arrange
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		ResourceKitGroup<TestHostKit> backup = new("backup");
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data, backup);

		// Act
		var groups = hostKit.GetGroups(postgres);

		// Assert
		await Assert.That(groups.Count).IsEqualTo(2);
		await Assert.That(groups[0]).IsSameReferenceAs(data);
	}

	[Test]
	public async Task Build_GivenResourceInDisabledGroup_DoesNotBuildIt()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data") { IsEnabled = false };
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(postgres.IsEnabled).IsFalse();
		await Assert.That(postgres.BuildCalled).IsFalse();
		await Assert.That(postgres.ConfigureCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenResourceInOneDisabledGroupOfSeveral_DoesNotBuildIt()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		ResourceKitGroup<TestHostKit> backup = new("backup") { IsEnabled = false };
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data, backup);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(postgres.IsEnabled).IsFalse();
		await Assert.That(postgres.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Build_GivenResourceInGroupWithDisabledAncestor_DoesNotBuildIt()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> platform = new("platform") { IsEnabled = false };
		ResourceKitGroup<TestHostKit> data = new("data", platform);
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(postgres.BuildCalled).IsFalse();
	}

	[Test]
	public async Task Configure_GivenEnabledGroupInRunMode_AddsDashboardNodeAndParentsMembers()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(data.DashboardResource).IsNotNull();
		await Assert.That(builder.Resources.OfType<ResourceKitGroupResource>().Single().Name).IsEqualTo("data");
		await Assert.That(ParentOf(postgres.Resource)).IsSameReferenceAs(data.DashboardResource!.Resource);
	}

	[Test]
	public async Task Configure_GivenNestedGroups_ParentsChildNodeToParentNode()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> platform = new("platform");
		ResourceKitGroup<TestHostKit> data = new("data", platform);
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert
			.That(ParentOf(data.DashboardResource!.Resource))
			.IsSameReferenceAs(platform.DashboardResource!.Resource);
		await Assert.That(ParentOf(postgres.Resource)).IsSameReferenceAs(data.DashboardResource.Resource);
	}

	[Test]
	public async Task Configure_GivenGroupHiddenFromDashboard_ParentsMembersToNearestShownAncestor()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> platform = new("platform");
		ResourceKitGroup<TestHostKit> data = new("data", platform) { ShowInDashboard = false };
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(data.DashboardResource).IsNull();
		await Assert.That(ParentOf(postgres.Resource)).IsSameReferenceAs(platform.DashboardResource!.Resource);
	}

	[Test]
	public async Task Configure_GivenGroupWithNoBuiltResources_DoesNotAddDashboardNode()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		TestResourceKit postgres = new(hostKit, "postgres") { IsEnabled = false };
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(data.DashboardResource).IsNull();
		await Assert.That(builder.Resources.OfType<ResourceKitGroupResource>()).IsEmpty();
	}

	[Test]
	public async Task Configure_GivenPublishMode_DoesNotAddDashboardNode()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder([
			"--publisher",
			"manifest",
			"--output-path",
			"manifest.json",
		]);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(builder.ExecutionContext.IsPublishMode).IsTrue();
		await Assert.That(postgres.BuildCalled).IsTrue();
		await Assert.That(data.DashboardResource).IsNull();
	}

	[Test]
	public async Task Configure_GivenResourceWithOwnParent_KeepsItAndNestsTheTopOfTheChain()
	{
		// Arrange
		// The AspireC4 shape: the kit's resource hangs off a row the integration created, which has no parent.
		var builder = DistributedApplication.CreateBuilder();
		var owner = builder.AddParameter("owner", "value", secret: false);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		TestResourceKit postgres = new(hostKit, "postgres", owner);
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(ParentOf(postgres.Resource)).IsSameReferenceAs(owner.Resource);
		await Assert.That(ParentOf(owner.Resource)).IsSameReferenceAs(data.DashboardResource!.Resource);
	}

	[Test]
	public async Task Configure_GivenDeepOwnChain_NestsOnlyTheTopmostAncestor()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var top = builder.AddParameter("top", "value", secret: false);
		var middle = builder.AddParameter("middle", "value", secret: false).WithParentRelationship(top);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		TestResourceKit postgres = new(hostKit, "postgres", middle);
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(ParentOf(postgres.Resource)).IsSameReferenceAs(middle.Resource);
		await Assert.That(ParentOf(middle.Resource)).IsSameReferenceAs(top.Resource);
		await Assert.That(ParentOf(top.Resource)).IsSameReferenceAs(data.DashboardResource!.Resource);
	}

	[Test]
	public async Task Configure_GivenKitParentedToAnotherKit_LeavesTheOtherKitInItsOwnGroup()
	{
		// Arrange
		// The child kit is added first, so placement must not depend on the order kits are added in.
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		ResourceKitGroup<TestHostKit> tools = new("tools");
		TestResourceKit server = new(hostKit, "server");
		ParentedToKitResourceKit admin = new(hostKit, "admin", server);
		hostKit.AddResource(admin, tools);
		hostKit.AddResource(server, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(ParentOf(server.Resource)).IsSameReferenceAs(data.DashboardResource!.Resource);
		await Assert.That(ParentOf(admin.Resource)).IsSameReferenceAs(server.Resource);
	}

	[Test]
	public async Task Configure_GivenChainEndingInAGroupNode_LeavesItThere()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		ResourceKitGroup<TestHostKit> tools = new("tools");
		TestResourceKit server = new(hostKit, "server");
		ParentedToKitResourceKit admin = new(hostKit, "admin", server);
		hostKit.AddResource(server, data);
		hostKit.AddResource(admin, tools);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		// admin -> server -> data's node: the chain is already in a group, so tools' node gains nothing.
		await Assert.That(ParentOf(admin.Resource)).IsSameReferenceAs(server.Resource);
		await Assert.That(builder.Resources.Any(r => ParentOf(r) == tools.DashboardResource!.Resource)).IsFalse();
	}

	[Test]
	[Timeout(60_000)]
	public async Task Configure_GivenParentCycleBuiltOutsideTheHostKit_TerminatesAndLeavesItAlone(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// ResourceKit never creates a cycle, but an integration might; the walk must still stop.
		var builder = DistributedApplication.CreateBuilder();
		var first = builder.AddParameter("first", "value", secret: false);
		var second = builder.AddParameter("second", "value", secret: false).WithParentRelationship(first);
		first.WithParentRelationship(second);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		TestResourceKit postgres = new(hostKit, "postgres", first);
		hostKit.AddResource(postgres, data);

		// Act
		cancellationToken.ThrowIfCancellationRequested();
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(ParentOf(postgres.Resource)).IsSameReferenceAs(first.Resource);
		await Assert.That(ParentOf(first.Resource)).IsSameReferenceAs(second.Resource);
		await Assert.That(ParentOf(second.Resource)).IsSameReferenceAs(first.Resource);
	}

	[Test]
	public async Task Configure_GivenGroupUrls_AnnotatesEnabledAbsoluteUrlsOnly()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data")
		{
			Urls =
			[
				new() { Url = "https://example.com/runbook", DisplayText = "Runbook" },
				new() { Url = "https://example.com/off", IsEnabled = false },
				new() { Url = "/health" },
			],
		};
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		var urls = data.DashboardResource!.Resource.Annotations.OfType<ResourceUrlAnnotation>().ToArray();
		await Assert.That(urls.Length).IsEqualTo(1);
		await Assert.That(urls[0].Url).IsEqualTo("https://example.com/runbook");
		await Assert.That(urls[0].DisplayText).IsEqualTo("Runbook");
	}

	[Test]
	public async Task Configure_GivenGroupNamedLikeOneOfItsResources_SuffixesTheNodeName()
	{
		// Arrange
		// The shape that failed in practice: a "minder" group holding a "minder" database.
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> minder = new("minder");
		TestResourceKit database = new(hostKit, "Minder");
		hostKit.AddResource(database, minder);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(minder.Name).IsEqualTo("minder");
		await Assert.That(minder.DashboardResource!.Resource.Name).IsEqualTo("minder-group");
		await Assert.That(ParentOf(database.Resource)).IsSameReferenceAs(minder.DashboardResource.Resource);
	}

	[Test]
	public async Task Configure_GivenSuffixedNameAlsoTaken_AppendsANumber()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		builder.AddParameter("MINDER-GROUP", "value", secret: false);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> minder = new("minder");
		hostKit.AddResource(new TestResourceKit(hostKit, "minder"), minder);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(minder.DashboardResource!.Resource.Name).IsEqualTo("minder-group-2");
	}

	[Test]
	public async Task Configure_GivenResourceAddedInConfigureResource_DoesNotCollideWithTheGroupNode()
	{
		// Arrange
		// The node is added after every ConfigureResource, so a resource added there claims the name first.
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		hostKit.AddResource(new ConfigureAddsResourceKit(hostKit, "postgres", "data"), data);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(builder.Resources.Any(static r => r is ParameterResource { Name: "data" })).IsTrue();
		await Assert.That(data.DashboardResource!.Resource.Name).IsEqualTo("data-group");
	}

	[Test]
	public async Task Build_WithoutConfigure_AddsNoDashboardNode()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		hostKit.AddResource(new TestResourceKit(hostKit, "postgres"), data);

		// Act
		hostKit.Build(builder);

		// Assert
		await Assert.That(data.DashboardResource).IsNull();
		await Assert.That(builder.Resources.OfType<ResourceKitGroupResource>()).IsEmpty();
	}

	[Test]
	public async Task Configure_GivenResourceIncludedByAKit_NestsItBesideTheKit()
	{
		// Arrange
		// The DbGate shape: a kit adds an admin tool next to its own resource and includes it in its group.
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> persistence = new("persistence");
		IncludingResourceKit sql = new(hostKit, "sql", "dbgate", persistence);
		hostKit.AddResource(sql, persistence);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		var node = persistence.DashboardResource!.Resource;
		await Assert.That(ParentOf(sql.Resource)).IsSameReferenceAs(node);
		await Assert.That(ParentOf(sql.Included!.Resource)).IsSameReferenceAs(node);
		await Assert.That(persistence.IncludedResources).Contains(sql.Included.Resource);
	}

	[Test]
	public async Task Configure_GivenGroupWithOnlyIncludedResources_AddsItsNode()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var dbGate = builder.AddParameter("dbgate", "value", secret: false);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> tools = new("tools");
		dbGate.WithResourceGroup(tools);
		hostKit.AddGroup(tools);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(tools.DashboardResource).IsNotNull();
		await Assert.That(ParentOf(dbGate.Resource)).IsSameReferenceAs(tools.DashboardResource!.Resource);
	}

	[Test]
	public async Task Configure_GivenResourceIncludedInTwoGroups_KeepsItInTheFirst()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var dbGate = builder.AddParameter("dbgate", "value", secret: false);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> first = new("first");
		ResourceKitGroup<TestHostKit> second = new("second");
		dbGate.WithResourceGroup(first);
		dbGate.WithResourceGroup(second);
		hostKit.AddGroup(first);
		hostKit.AddGroup(second);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		var parents = dbGate
			.Resource.Annotations.OfType<ResourceRelationshipAnnotation>()
			.Where(static a => a.Type == "Parent");
		await Assert.That(parents.Count()).IsEqualTo(1);
		await Assert.That(ParentOf(dbGate.Resource)).IsSameReferenceAs(first.DashboardResource!.Resource);
	}

	[Test]
	public async Task Configure_GivenIncludedResourceInHiddenGroup_NestsItUnderTheNearestShownAncestor()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var dbGate = builder.AddParameter("dbgate", "value", secret: false);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> platform = new("platform");
		ResourceKitGroup<TestHostKit> tools = new("tools", platform) { ShowInDashboard = false };
		dbGate.WithResourceGroup(tools);
		hostKit.AddGroup(tools);

		// Act
		hostKit.Build(builder);
		hostKit.Configure();

		// Assert
		await Assert.That(tools.DashboardResource).IsNull();
		await Assert.That(ParentOf(dbGate.Resource)).IsSameReferenceAs(platform.DashboardResource!.Resource);
	}

	[Test]
	public async Task WithResourceGroup_GivenAGroupDashboardNode_ThrowsArgumentException()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		ResourceKitGroup<TestHostKit> data = new("data");
		var node = builder.AddResource(new ResourceKitGroupResource("other"));

		// Act
		var exception = await Assert.That(() => node.WithResourceGroup(data)).ThrowsExactly<ArgumentException>();

		// Assert
		await Assert.That(exception).IsNotNull();
	}

	[Test]
	public async Task WithResourceGroup_AfterConfigure_ThrowsInvalidOperationException()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var dbGate = builder.AddParameter("dbgate", "value", secret: false);
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		hostKit.AddResource(new TestResourceKit(hostKit, "postgres"), data);
		hostKit.Build(builder);
		hostKit.Configure();

		// Act
		var exception = await Assert
			.That(() => dbGate.WithResourceGroup(data))
			.ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception).IsNotNull();
	}

	[Test]
	public async Task Parent_GivenAGroup_CannotBeReassigned()
	{
		// Arrange
		// A parent is set only by the constructor, before the instance can be anyone's ancestor, which is what
		// makes the parent chain acyclic. A setter would let a group be made its own ancestor.
		var parent = typeof(ResourceKitGroup<TestHostKit>).GetProperty(nameof(ResourceKitGroup<>.Parent));

		// Act
		var setter = parent!.SetMethod;

		// Assert
		await Assert.That(setter).IsNull();
	}

	[Test]
	public async Task Children_WhenCastToAMutableList_CannotBeChanged()
	{
		// Arrange
		ResourceKitGroup<TestHostKit> platform = new("platform");
		ResourceKitGroup<TestHostKit> data = new("data", platform);

		// Act
		var children = (ICollection<ResourceKitGroup<TestHostKit>>)platform.Children;

		// Assert
		await Assert.That(platform.Children is List<ResourceKitGroup<TestHostKit>>).IsFalse();
		await Assert.That(children.IsReadOnly).IsTrue();
		await Assert.That(() => children.Add(platform)).ThrowsExactly<NotSupportedException>();
		await Assert.That(platform.Children).IsEquivalentTo([data]);
	}

	[Test]
	public async Task Resources_WhenCastToAMutableList_CannotBeChanged()
	{
		// Arrange
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		hostKit.AddResource(new TestResourceKit(hostKit, "postgres"), data);

		// Act
		var resources = (ICollection<IResourceKit<TestHostKit>>)data.Resources;

		// Assert
		await Assert.That(resources.IsReadOnly).IsTrue();
	}

	[Test]
	public async Task GetGroups_WhenCastToAMutableList_CannotBeChanged()
	{
		// Arrange
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> data = new("data");
		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, data);

		// Act
		var groups = (ICollection<ResourceKitGroup<TestHostKit>>)hostKit.GetGroups(postgres);

		// Assert
		await Assert.That(groups.IsReadOnly).IsTrue();
	}

	[Test]
	[Timeout(60_000)]
	public async Task Walks_GivenAVeryDeepHierarchy_CompleteWithoutExhaustingTheStack(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Deep enough that any recursive walk would overflow the stack.
		const int depth = 100_000;
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		ResourceKitGroup<TestHostKit> root = new("g0");
		var leaf = root;
		for (var i = 1; i < depth; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			leaf = new($"g{i}", leaf);
		}

		TestResourceKit postgres = new(hostKit, "postgres");
		hostKit.AddResource(postgres, leaf);

		// Act
		var allResources = root.AllResources.ToArray();
		var isEnabled = leaf.IsEnabledFor(builder);
		var path = leaf.ToString();

		// Assert
		await Assert.That(allResources.Length).IsEqualTo(1);
		await Assert.That(allResources[0]).IsSameReferenceAs(postgres);
		await Assert.That(isEnabled).IsTrue();
		await Assert.That(path).StartsWith("g0/g1/");
		await Assert.That(path).EndsWith($"/g{depth - 1}");
	}

	[Test]
	public async Task AddGroup_WhenSealed_ThrowsInvalidOperationException()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		TestHostKit hostKit = new();
		hostKit.Build(builder);

		// Act
		var exception = await Assert
			.That(() => hostKit.AddGroup(new("data")))
			.ThrowsExactly<InvalidOperationException>();

		// Assert
		await Assert.That(exception).IsNotNull();
	}

	static IResource? ParentOf(IResource resource) =>
		resource
			.Annotations.OfType<ResourceRelationshipAnnotation>()
			.SingleOrDefault(static a => a.Type == "Parent")
			?.Resource;

	sealed class TestHostKit : HostKitBase<TestHostKit>;

	// Adds a second, non-kit resource beside its own and includes it in a group, as SqlServerKit does with DbGate.
	sealed class IncludingResourceKit(
		TestHostKit hostKit,
		string name,
		string includedName,
		ResourceKitGroup<TestHostKit> group
	) : ResourceKitBase<TestHostKit, ParameterResource>(hostKit, name)
	{
		public IResourceBuilder<ParameterResource>? Included { get; private set; }

		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		)
		{
			Included = builder.AddParameter(includedName, "value", secret: false).WithResourceGroup(group);
			return builder.AddParameter(Name, "value", secret: false);
		}
	}

	// Parents its resource to another kit's resource, which only exists once that kit has been built.
	sealed class ParentedToKitResourceKit(TestHostKit hostKit, string name, TestResourceKit parent)
		: ResourceKitBase<TestHostKit, ParameterResource>(hostKit, name)
	{
		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		) => builder.AddParameter(Name, "value", secret: false);

		protected override void ConfigureResource() => ResourceBuilder.WithParentRelationship(parent.Resource);
	}

	// Adds a second resource from ConfigureResource, as a kit that adds a database there does.
	sealed class ConfigureAddsResourceKit(TestHostKit hostKit, string name, string configuredName)
		: ResourceKitBase<TestHostKit, ParameterResource>(hostKit, name)
	{
		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		) => builder.AddParameter(Name, "value", secret: false);

		protected override void ConfigureResource() =>
			ApplicationBuilder.AddParameter(configuredName, "value", secret: false);
	}

	sealed class TestResourceKit(TestHostKit hostKit, string name, IResourceBuilder<IResource>? parent = null)
		: ResourceKitBase<TestHostKit, ParameterResource>(hostKit, name)
	{
		public bool BuildCalled { get; private set; }

		public bool ConfigureCalled { get; private set; }

		protected override IResourceBuilder<ParameterResource> BuildResource(
			[NotNull] IDistributedApplicationBuilder builder
		)
		{
			BuildCalled = true;
			var resource = builder.AddParameter(Name, "value", secret: false);
			return parent is null ? resource : resource.WithParentRelationship(parent);
		}

		protected override void ConfigureResource() => ConfigureCalled = true;
	}
}
