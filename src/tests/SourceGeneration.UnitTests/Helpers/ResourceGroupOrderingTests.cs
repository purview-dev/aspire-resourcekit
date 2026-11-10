using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

/// <summary>
/// Covers <see cref="ResourceGroupOrdering"/> directly: SG0021/SG0022 block generation before a cycle or a missing
/// parent can reach it, so the generator never exercises its loop guard.
/// </summary>
public sealed class ResourceGroupOrderingTests
{
	// A guard that failed would spin rather than fail; the timeout turns that into a failure.
	const int TimeoutMilliseconds = 30_000;

	[Test]
	public async Task ParentsFirst_GivenChildBeforeParent_OrdersParentFirst(CancellationToken cancellationToken)
	{
		// Arrange
		var data = Group("DataGroup", parent: "PlatformGroup");
		var platform = Group("PlatformGroup");

		// Act
		var ordered = ResourceGroupOrdering.ParentsFirst([data, platform], cancellationToken);

		// Assert
		await Assert.That(Names(ordered)).IsEqualTo("PlatformGroup,DataGroup");
	}

	[Test]
	[Timeout(TimeoutMilliseconds)]
	public async Task ParentsFirst_GivenGroupThatIsItsOwnParent_LeavesItOut(CancellationToken cancellationToken)
	{
		// Arrange
		var self = Group("AGroup", parent: "AGroup");
		var root = Group("RootGroup");

		// Act
		var ordered = ResourceGroupOrdering.ParentsFirst([self, root], cancellationToken);

		// Assert
		await Assert.That(Names(ordered)).IsEqualTo("RootGroup");
	}

	[Test]
	[Timeout(TimeoutMilliseconds)]
	public async Task ParentsFirst_GivenCycle_LeavesTheCycleOutAndKeepsTheRest(CancellationToken cancellationToken)
	{
		// Arrange
		var a = Group("AGroup", parent: "BGroup");
		var b = Group("BGroup", parent: "CGroup");
		var c = Group("CGroup", parent: "AGroup");
		var root = Group("RootGroup");
		var child = Group("ChildGroup", parent: "RootGroup");

		// Act
		var ordered = ResourceGroupOrdering.ParentsFirst([a, b, c, child, root], cancellationToken);

		// Assert
		await Assert.That(Names(ordered)).IsEqualTo("RootGroup,ChildGroup");
	}

	[Test]
	[Timeout(TimeoutMilliseconds)]
	public async Task ParentsFirst_GivenMissingParent_LeavesTheOrphanOut(CancellationToken cancellationToken)
	{
		// Arrange
		var orphan = Group("DataGroup", parent: "MissingGroup");

		// Act
		var ordered = ResourceGroupOrdering.ParentsFirst([orphan], cancellationToken);

		// Assert
		await Assert.That(Names(ordered)).IsEmpty();
	}

	static string Names(EquatableArray<ResourceGroupModel> groups) =>
		string.Join(",", groups.AsImmutableArray().Select(static g => g.Name));

	static ResourceGroupModel Group(string name, string? parent = null) =>
		new(
			GroupType: new(name, "Testing", 0),
			OptionsType: TypeIdentity.Empty,
			ParentType: parent is null ? TypeIdentity.Empty : new(parent, "Testing", 0),
			Accessibility: null,
			Name: name,
			PropertyName: name,
			ShowInDashboard: true,
			Mode: null,
			Urls: EquatableArray<UrlModel>.Empty
		);
}
