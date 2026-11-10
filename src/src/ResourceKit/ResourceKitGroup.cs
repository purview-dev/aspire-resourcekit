using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// A named group of resource kits, used to enable, disable or scope a set of resources with one switch and
/// to nest them under one node in the Aspire dashboard.
/// </summary>
/// <remarks>
/// <para>
/// Groups nest through <see cref="Parent"/>. A resource kit can belong to several groups; it is built only
/// when every group it belongs to, and every ancestor of those groups, is enabled for the current run (see
/// <see cref="IsEnabledFor"/>). The first group a resource kit is added with is its <em>primary</em> group,
/// and decides where the kit is shown in the dashboard.
/// </para>
/// <para>
/// Generated groups (<c>[ResourceGroup]</c>) derive from this class and bind <see cref="IsEnabled"/>,
/// <see cref="Mode"/>, <see cref="ShowInDashboard"/> and <see cref="Urls"/> from options. Manual host kits can
/// create instances directly and pass them to <see cref="HostKitBase{THostKit}.AddResource(IResourceKit{THostKit}, ReadOnlySpan{ResourceKitGroup{THostKit}})"/>.
/// </para>
/// </remarks>
/// <typeparam name="THostKit">The host kit type.</typeparam>
public class ResourceKitGroup<THostKit>
	where THostKit : class, IHostKit
{
	readonly List<ResourceKitGroup<THostKit>> _children = [];
	readonly List<IResourceKit<THostKit>> _resources = [];
	readonly List<IResource> _included = [];
	bool _includesClosed;

	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceKitGroup{THostKit}"/> class.
	/// </summary>
	/// <param name="name">
	/// The group name. It also names the group's dashboard resource, so it must be a valid Aspire resource name.
	/// When another resource already has the name, the node is named <c>&lt;name&gt;-group</c> instead; see
	/// <see cref="DashboardResource"/>.
	/// </param>
	/// <param name="parent">The optional parent group.</param>
	public ResourceKitGroup(string name, ResourceKitGroup<THostKit>? parent = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		Name = name;
		Parent = parent;
		Depth = parent is null ? 0 : parent.Depth + 1;
		Children = _children.AsReadOnly();
		Resources = _resources.AsReadOnly();
		IncludedResources = _included.AsReadOnly();

		// The only place a parent link is made. This instance does not exist until the constructor runs, so it
		// cannot already be an ancestor of its parent: the parent chain is acyclic by construction.
		parent?._children.Add(this);
	}

	/// <summary>
	/// Gets the group name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets or sets the Fluent UI icon shown for the group in the dashboard. When <see langword="null"/>,
	/// </summary>
	public string? Icon { get; set; }

	/// <summary>
	/// Gets the parent group, or <see langword="null"/> for a root group.
	/// </summary>
	public ResourceKitGroup<THostKit>? Parent { get; }

	/// <summary>
	/// Gets or sets a value indicating whether the group, and so every resource kit in it, is enabled.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the execution mode in which the group, and so every resource kit in it, participates.
	/// </summary>
	public ResourceKitMode Mode { get; set; } = ResourceKitMode.All;

	/// <summary>
	/// Gets or sets a value indicating whether the group is shown as a node in the Aspire dashboard.
	/// </summary>
	/// <remarks>
	/// When <see langword="false"/>, the group still gates its resources, and they are parented to the nearest
	/// ancestor group that is shown instead.
	/// </remarks>
	public bool ShowInDashboard { get; set; } = true;

	/// <summary>
	/// Gets or sets the absolute URLs shown on the group's dashboard node, for example a runbook link.
	/// </summary>
	public IReadOnlyList<ResourceKitUrl> Urls { get; set; } = [];

	/// <summary>
	/// Gets the groups whose <see cref="Parent"/> is this group.
	/// </summary>
	/// <remarks>Read-only: a child is added only by constructing it with this group as its parent.</remarks>
	public IReadOnlyList<ResourceKitGroup<THostKit>> Children { get; }

	/// <summary>
	/// Gets the resource kits added directly to this group.
	/// </summary>
	public IReadOnlyList<IResourceKit<THostKit>> Resources { get; }

	/// <summary>
	/// Gets the resources that are not resource kits but were placed in this group with
	/// <c>WithResourceGroup(group)</c>.
	/// </summary>
	public IReadOnlyList<IResource> IncludedResources { get; }

	// The public entry point is IResourceBuilder<T>.WithResourceGroup, which carries the documentation.
	internal void Include(IResource resource)
	{
		ArgumentNullException.ThrowIfNull(resource);

		// A group node is only ever parented to another group node; including one could parent it to itself.
		if (resource is ResourceKitGroupResource)
			throw new ArgumentException("A group's dashboard node cannot be included in a group.", nameof(resource));

		if (_includesClosed)
		{
			throw new InvalidOperationException(
				$"Resources cannot be included in the '{Name}' group after the host kit has configured its resources."
			);
		}

		if (!_included.Contains(resource, ReferenceEqualityComparer.Instance))
			_included.Add(resource);
	}

	/// <summary>
	/// Gets the resource kits in this group and in every descendant group, each once.
	/// </summary>
	public IEnumerable<IResourceKit<THostKit>> AllResources
	{
		get
		{
			// Iterative, and each group is visited once, so the walk cannot recurse or loop even if the tree
			// were ever malformed. Order is depth-first: a group's own resources, then each child in turn.
			HashSet<IResourceKit<THostKit>> seenResources = [with(ReferenceEqualityComparer.Instance)];
			HashSet<ResourceKitGroup<THostKit>> seenGroups = [with(ReferenceEqualityComparer.Instance)];
			Stack<ResourceKitGroup<THostKit>> pending = new([this]);

			while (pending.Count > 0)
			{
				var group = pending.Pop();
				if (!seenGroups.Add(group))
					continue;

				foreach (var resource in group._resources)
				{
					if (seenResources.Add(resource))
						yield return resource;
				}

				for (var i = group._children.Count - 1; i >= 0; i--)
					pending.Push(group._children[i]);
			}
		}
	}

	/// <summary>
	/// Gets the group's dashboard resource, once the host kit has finished configuring its resources.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see langword="null"/> until <see cref="HostKitBase{THostKit}.Configure"/> has run, so it is not
	/// available inside <c>ConfigureResource</c>. It also stays <see langword="null"/> outside run mode, when
	/// <see cref="ShowInDashboard"/> is <see langword="false"/>, when the group is disabled, or when none of its
	/// resources were built.
	/// </para>
	/// <para>
	/// The resource is named <see cref="Name"/> when no other resource has that name, and otherwise
	/// <c>&lt;Name&gt;-group</c> (then <c>-group-2</c>, and so on), because Aspire resource names are unique.
	/// </para>
	/// </remarks>
	public IResourceBuilder<ResourceKitGroupResource>? DashboardResource { get; internal set; }

	/// <summary>
	/// Determines whether the group, and every ancestor of it, is enabled for the current run.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <returns><see langword="true"/> when the group's resources may be built.</returns>
	public bool IsEnabledFor(IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		for (var group = this; group is not null; group = group.Parent)
		{
			if (!group.IsEnabled || !group.Mode.IsEnabledFor(builder))
				return false;
		}

		return true;
	}

	// Fixed at construction: a parent's depth is already known, and the parent can never change.
	internal int Depth { get; }

	internal void CloseIncludes() => _includesClosed = true;

	// Whether this group or a descendant has anything to show: a built kit, or an included resource.
	internal bool HasDashboardContent()
	{
		if (AllResources.Any(static r => r.IsEnabled))
			return true;

		HashSet<ResourceKitGroup<THostKit>> seen = [with(ReferenceEqualityComparer.Instance)];
		Stack<ResourceKitGroup<THostKit>> pending = new([this]);
		while (pending.Count > 0)
		{
			var group = pending.Pop();
			if (!seen.Add(group))
				continue;

			if (group._included.Count > 0)
				return true;

			foreach (var child in group._children)
				pending.Push(child);
		}

		return false;
	}

	internal void AddMember(IResourceKit<THostKit> resource)
	{
		if (!_resources.Contains(resource))
			_resources.Add(resource);
	}

	internal IResourceBuilder<ResourceKitGroupResource>? NearestDashboardResource()
	{
		for (var group = this; group is not null; group = group.Parent)
		{
			if (group.DashboardResource is not null)
				return group.DashboardResource;
		}

		return null;
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		// Iterative, like every walk up the parent chain, so a deep hierarchy cannot exhaust the stack.
		List<string> names = [];
		for (var group = this; group is not null; group = group.Parent)
			names.Add(group.Name);

		names.Reverse();
		return string.Join('/', names);
	}
}
