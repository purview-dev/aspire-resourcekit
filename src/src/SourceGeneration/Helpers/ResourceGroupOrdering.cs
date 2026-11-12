using System.Collections.Immutable;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

static class ResourceGroupOrdering
{
	/// <summary>
	/// Orders groups so every parent precedes its children, as the generated <c>Groups</c> accessor hands each
	/// group its already-created parent.
	/// </summary>
	/// <remarks>
	/// A group whose parent is itself, is in a cycle, or is missing can never become ready, so it is left out
	/// rather than looped on. Each pass either emits at least one group or stops, so the method makes at most
	/// one pass per group. In practice those groups never get here: SG0021 and SG0022 block generation.
	/// </remarks>
	public static EquatableArray<ResourceGroupModel> ParentsFirst(
		ImmutableArray<ResourceGroupModel> groups,
		CancellationToken cancellationToken
	)
	{
		List<ResourceGroupModel> ordered = [];
		HashSet<TypeIdentity> emitted = [];
		var remaining = groups.ToList();

		while (remaining.Count > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var ready = remaining.Where(g => !g.HasParent || emitted.Contains(g.ParentType)).ToArray();
			if (ready.Length == 0)
				break;

			foreach (var group in ready)
			{
				ordered.Add(group);
				emitted.Add(group.GroupType);
				remaining.Remove(group);
			}
		}

		return EquatableArray<ResourceGroupModel>.Create([.. ordered]);
	}
}
