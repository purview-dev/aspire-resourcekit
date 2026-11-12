using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class ResourceKitHierarchyOrdering
{
	/// <summary>
	/// Orders kits so every parent precedes its children. Siblings are ordered by metadata full name, which
	/// keeps generated output deterministic whatever order discovery produced.
	/// </summary>
	/// <remarks>
	/// A kit whose parent is itself, is missing, or is part of a cycle can never become ready. Those cases are
	/// blocked by diagnostics, and this method returns an empty order rather than looping or emitting an
	/// impossible construction sequence.
	/// </remarks>
	public static EquatableArray<ResourceKitModel> ParentsFirst(
		IEnumerable<ResourceKitModel> kits,
		CancellationToken cancellationToken
	)
	{
		var ordered = kits.OrderBy(static kit => kit.ResourceKitType.MetadataFullName, StringComparer.Ordinal)
			.ToArray();
		if (ordered.Length == 0)
			return EquatableArray<ResourceKitModel>.Empty;

		Dictionary<string, int> indexByType = [with(StringComparer.Ordinal)];
		for (var index = 0; index < ordered.Length; index++)
			indexByType[ordered[index].ResourceKitType.MetadataFullName] = index;

		Dictionary<int, List<int>> children = [];
		SortedSet<int> ready = [];

		for (var index = 0; index < ordered.Length; index++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var parent = ordered[index].ParentType;
			if (parent == TypeIdentity.Empty)
			{
				ready.Add(index);
				continue;
			}

			if (!indexByType.TryGetValue(parent.MetadataFullName, out var parentIndex))
				return EquatableArray<ResourceKitModel>.Empty;

			if (!children.TryGetValue(parentIndex, out var siblings))
				children[parentIndex] = siblings = [];

			siblings.Add(index);
		}

		List<ResourceKitModel> result = [];
		while (ready.Count > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var index = ready.Min;
			ready.Remove(index);
			result.Add(ordered[index]);

			if (!children.TryGetValue(index, out var siblings))
				continue;

			foreach (var child in siblings)
				ready.Add(child);
		}

		return result.Count == ordered.Length
			? EquatableArray<ResourceKitModel>.Create([.. result])
			: EquatableArray<ResourceKitModel>.Empty;
	}
}
