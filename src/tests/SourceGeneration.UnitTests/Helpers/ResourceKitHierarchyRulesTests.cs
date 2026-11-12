using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

/// <summary>
/// Covers the graph rules directly, including cases the generator blocks before emission: cycles, missing
/// parents, duplicate child property names, and deep/wide ordering.
/// </summary>
public sealed class ResourceKitHierarchyRulesTests
{
	const int TimeoutMilliseconds = 30_000;

	[Test]
	public async Task ParentsFirst_GivenChildBeforeParent_OrdersParentFirst()
	{
		// Arrange
		var child = Kit("ChildKit", parent: "ParentKit");
		var parent = Kit("ParentKit");

		// Act
		var ordered = ResourceKitHierarchyOrdering.ParentsFirst([child, parent], CancellationToken.None);

		// Assert
		await Assert.That(Names(ordered)).IsEqualTo("ParentKit,ChildKit");
	}

	[Test]
	public async Task ParentsFirst_GivenIndependentSiblings_OrdersDeterministically()
	{
		// Arrange
		var siblingB = Kit("BKit");
		var siblingA = Kit("AKit");

		// Act
		var ordered = ResourceKitHierarchyOrdering.ParentsFirst([siblingB, siblingA], CancellationToken.None);

		// Assert
		await Assert.That(Names(ordered)).IsEqualTo("AKit,BKit");
	}

	[Test]
	[Timeout(TimeoutMilliseconds)]
	public async Task ParentsFirst_GivenSelfParent_ReturnsEmpty(CancellationToken cancellationToken)
	{
		// Arrange
		var self = Kit("SelfKit", parent: "SelfKit");

		// Act
		var ordered = ResourceKitHierarchyOrdering.ParentsFirst([self], cancellationToken);

		// Assert
		await Assert.That(ordered.IsEmpty).IsTrue();
	}

	[Test]
	[Timeout(TimeoutMilliseconds)]
	public async Task ParentsFirst_GivenCycle_ReturnsEmpty(CancellationToken cancellationToken)
	{
		// Arrange
		var a = Kit("AKit", parent: "BKit");
		var b = Kit("BKit", parent: "CKit");
		var c = Kit("CKit", parent: "AKit");
		var root = Kit("RootKit");

		// Act
		var ordered = ResourceKitHierarchyOrdering.ParentsFirst([a, b, c, root], cancellationToken);

		// Assert
		await Assert.That(ordered.IsEmpty).IsTrue();
	}

	[Test]
	[Timeout(TimeoutMilliseconds)]
	public async Task ParentsFirst_GivenMissingParent_ReturnsEmpty(CancellationToken cancellationToken)
	{
		// Arrange
		var orphan = Kit("ChildKit", parent: "MissingKit");

		// Act
		var ordered = ResourceKitHierarchyOrdering.ParentsFirst([orphan], cancellationToken);

		// Assert
		await Assert.That(ordered.IsEmpty).IsTrue();
	}

	[Test]
	[Timeout(TimeoutMilliseconds)]
	public async Task ParentsFirst_GivenDeepChain_OrdersWithoutStackOverflow(CancellationToken cancellationToken)
	{
		// Arrange
		const int depth = 2_000;
		List<ResourceKitModel> kits = [];
		for (var index = 0; index < depth; index++)
			kits.Add(Kit($"Kit{index:D4}", parent: index == 0 ? null : $"Kit{index - 1:D4}"));

		// Act
		var ordered = ResourceKitHierarchyOrdering.ParentsFirst(kits, cancellationToken);

		// Assert
		await Assert.That(ordered.Count).IsEqualTo(depth);
		await Assert.That(ordered[0].ResourceKitType.Name).IsEqualTo("Kit0000");
		await Assert.That(ordered[depth - 1].ResourceKitType.Name).IsEqualTo($"Kit{depth - 1:D4}");
	}

	[Test]
	public async Task EvaluateHierarchy_GivenSelfParent_ReportsSelfParenting()
	{
		// Arrange
		var node = Node("SelfKit", "SelfKit");

		// Act
		var findings = ResourceKitRules.EvaluateHierarchy([node]);

		// Assert
		await Assert.That(findings).HasSingleItem(static f => f.Descriptor.Id == "SG0032");
	}

	[Test]
	public async Task EvaluateHierarchy_GivenThreeNodeCycle_ReportsOneCycleWithPath()
	{
		// Arrange
		ResourceKitRules.HierarchyNode[] nodes = [Node("AKit", "BKit"), Node("BKit", "CKit"), Node("CKit", "AKit")];

		// Act
		var findings = ResourceKitRules.EvaluateHierarchy([.. nodes]);

		// Assert
		var cycle = await Assert.That(findings).HasSingleItem(static f => f.Descriptor.Id == "SG0033");
		var message = string.Join("|", cycle.MessageArgs);
		await Assert.That(message).Contains("AKit");
		await Assert.That(message).Contains("BKit");
		await Assert.That(message).Contains("CKit");
	}

	[Test]
	public async Task EvaluateHierarchy_GivenMissingParent_ReportsUnavailable()
	{
		// Arrange
		var node = Node("ChildKit", "MissingKit");

		// Act
		var findings = ResourceKitRules.EvaluateHierarchy([node]);

		// Assert
		await Assert.That(findings).HasSingleItem(static f => f.Descriptor.Id == "SG0035");
	}

	[Test]
	public async Task EvaluateHierarchy_GivenDuplicateChildPropertyNames_ReportsTheDuplicate()
	{
		// Arrange
		ResourceKitRules.HierarchyNode[] nodes =
		[
			Node("ParentKit", null, "Parent"),
			Node("FirstKit", "ParentKit", "Database"),
			Node("SecondKit", "ParentKit", "Database"),
		];

		// Act
		var findings = ResourceKitRules.EvaluateHierarchy([.. nodes]);

		// Assert
		await Assert.That(findings).HasSingleItem(static f => f.Descriptor.Id == "SG0037");
	}

	static string Names(EquatableArray<ResourceKitModel> kits) =>
		string.Join(",", kits.AsImmutableArray().Select(static kit => kit.ResourceKitType.Name));

	static ResourceKitModel Kit(string name, string? parent = null) =>
		new(
			ResourceKitType: new(name, "Testing", 0),
			OptionsType: TypeIdentity.Empty,
			AspireResourceType: TypeIdentity.Empty,
			ParentType: parent is null ? TypeIdentity.Empty : new(parent, "Testing", 0),
			Accessibility: null,
			PropertyName: name,
			ResourceName: name,
			HasExplicitBaseType: false,
			Mode: null,
			Groups: EquatableArray<TypeIdentity>.Empty,
			Urls: EquatableArray<UrlModel>.Empty,
			Location: default
		);

	static ResourceKitRules.HierarchyNode Node(string name, string? parent, string? propertyName = null) =>
		new(
			new(name, "Testing", 0),
			parent is null ? TypeIdentity.Empty : new(parent, "Testing", 0),
			name,
			propertyName ?? name
		);
}
