using System.Collections.Immutable;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Provides a base implementation for host applications composed of <see cref="IResourceKit{THostApp}"/> resources.
/// </summary>
/// <typeparam name="THostKit">The concrete host kit type.</typeparam>
public abstract class HostKitBase<THostKit> : IHostKit
	where THostKit : class, IHostKit
{
	// The relationship type WithParentRelationship records, and the dashboard nests by.
	const string ParentRelationshipType = "Parent";

	readonly List<IResourceKit<THostKit>> _resources = [];
	readonly List<ResourceKitGroup<THostKit>> _groups = [];
	readonly HashSet<ResourceKitGroup<THostKit>> _registeredGroups = [with(ReferenceEqualityComparer.Instance)];
	readonly Dictionary<IResourceKit<THostKit>, List<ResourceKitGroup<THostKit>>> _memberships =
	[
		with(ReferenceEqualityComparer.Instance),
	];
	bool _resourcesSealed;

	// Held from Build to Configure, which adds the group dashboard nodes once and then releases it.
	IDistributedApplicationBuilder? _builder;

	/// <summary>
	/// Gets the resources managed by this host kit.
	/// </summary>
	/// <remarks>The resources will be empty until <see cref="SealResources"/> is called.</remarks>
	protected ImmutableArray<IResourceKit<THostKit>> Resources { get; private set; } = [];

	/// <summary>
	/// Gets the resource groups registered with this host kit, parents before their children.
	/// </summary>
	/// <remarks>The groups will be empty until <see cref="SealResources"/> is called.</remarks>
	protected ImmutableArray<ResourceKitGroup<THostKit>> ResourceGroups { get; private set; } = [];

	/// <summary>
	/// Gets the execution mode in which this host kit, and therefore every resource it composes, participates.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="ResourceKitMode.All"/>. Generated host kits override this member to surface the
	/// configured host <c>Mode</c> option. When the mode does not participate in the current run, every resource
	/// is disabled before it is built, so <see cref="Build"/> and <see cref="Configure"/> both skip them.
	/// </remarks>
	protected virtual ResourceKitMode Mode => ResourceKitMode.All;

	/// <summary>
	/// Adds a resource to the host kit.
	/// </summary>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="InvalidOperationException">Thrown when resources have been sealed.</exception>
	public void AddResource(IResourceKit<THostKit> resource) => AddResource(resource, []);

	/// <summary>
	/// Adds a resource to the host kit as a member of <paramref name="groups"/>.
	/// </summary>
	/// <param name="resource">The resource to add.</param>
	/// <param name="groups">
	/// The groups the resource belongs to. The first is its primary group, which decides where the resource is
	/// shown in the dashboard. Groups, and their ancestors, are registered with the host kit if they are not
	/// already.
	/// </param>
	/// <exception cref="InvalidOperationException">Thrown when resources have been sealed.</exception>
	public void AddResource(IResourceKit<THostKit> resource, params ReadOnlySpan<ResourceKitGroup<THostKit>> groups)
	{
		ThrowIfSealed();
		ArgumentNullException.ThrowIfNull(resource);

		_resources.Add(resource);

		foreach (var group in groups)
		{
			ArgumentNullException.ThrowIfNull(group, nameof(groups));

			AddGroup(group);
			group.AddMember(resource);

			if (!_memberships.TryGetValue(resource, out var memberships))
				_memberships[resource] = memberships = [];

			if (!memberships.Contains(group))
				memberships.Add(group);
		}
	}

	/// <summary>
	/// Registers a group, and its ancestors, with the host kit.
	/// </summary>
	/// <param name="group">The group to register.</param>
	/// <remarks>
	/// Only needed for a group no resource is added to; <see cref="AddResource(IResourceKit{THostKit}, ReadOnlySpan{ResourceKitGroup{THostKit}})"/>
	/// registers the groups it is given.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown when resources have been sealed.</exception>
	public void AddGroup(ResourceKitGroup<THostKit> group)
	{
		ThrowIfSealed();
		ArgumentNullException.ThrowIfNull(group);

		// Stops at the first ancestor already registered, so each group is visited once however groups arrive.
		for (var current = group; current is not null; current = current.Parent)
		{
			if (!_registeredGroups.Add(current))
				break;

			_groups.Add(current);
		}
	}

	/// <summary>
	/// Gets the groups a resource belongs to, primary group first.
	/// </summary>
	/// <param name="resource">The resource.</param>
	/// <returns>The resource's groups, or an empty list when it belongs to none.</returns>
	public IReadOnlyList<ResourceKitGroup<THostKit>> GetGroups(IResourceKit<THostKit> resource)
	{
		ArgumentNullException.ThrowIfNull(resource);

		return _memberships.TryGetValue(resource, out var groups) ? groups.AsReadOnly() : [];
	}

	void ThrowIfSealed()
	{
		if (_resourcesSealed)
			throw new InvalidOperationException("Resources cannot be added after the host kit has been sealed.");
	}

	bool SealResources()
	{
		if (_resourcesSealed)
			return false;

		_resourcesSealed = true;
		Resources = [.. _resources];
		ResourceGroups = [.. _groups.OrderBy(static g => g.Depth)];

		return true;
	}

	/// <inheritdoc/>
	public virtual void Build(IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		if (!SealResources())
			return;

		var modeEnabled = Mode.IsEnabledFor(builder);

		foreach (var resource in Resources)
		{
			if (!modeEnabled || !GetGroups(resource).All(g => g.IsEnabledFor(builder)))
				resource.IsEnabled = false;

			resource.Build(builder);
		}

		_builder = builder;
	}

	/// <inheritdoc/>
	/// <remarks>
	/// After every resource is configured, the groups' dashboard nodes are added and resources are parented to
	/// them. Doing this last means a resource added in <c>ConfigureResource</c>, as well as in
	/// <c>BuildResource</c>, can never be shadowed by a group node of the same name.
	/// </remarks>
	public virtual void Configure()
	{
		foreach (var resource in Resources)
			resource.Configure();

		if (_builder is not null)
		{
			BuildGroupDashboardResources(_builder);
			_builder = null;

			// Placement has happened, so a later Include could only be silently ignored.
			foreach (var group in ResourceGroups)
				group.CloseIncludes();
		}
	}

	// A group's node is named after the group when that name is free. Aspire resource names are unique and
	// case-insensitive, and a group is often named after something it contains (a "minder" group holding a
	// "minder" database), so a taken name falls back to "<name>-group", then "<name>-group-2", and so on.
	static string DashboardResourceName(IDistributedApplicationBuilder builder, string groupName)
	{
		HashSet<string> taken = new(builder.Resources.Select(static r => r.Name), StringComparer.OrdinalIgnoreCase);
		if (!taken.Contains(groupName))
			return groupName;

		var candidate = $"{groupName}-group";
		for (var suffix = 2; taken.Contains(candidate); suffix++)
			candidate = $"{groupName}-group-{suffix}";

		return candidate;
	}

	void BuildGroupDashboardResources(IDistributedApplicationBuilder builder)
	{
		if (!builder.ExecutionContext.IsRunMode || ResourceGroups.IsEmpty)
			return;

		AddGroupNodes(builder);
		PlaceResourcesInGroupNodes();
	}

	void AddGroupNodes(IDistributedApplicationBuilder builder)
	{
		// ResourceGroups is ordered parents first, so a parent's node always exists before its children's.
		foreach (var group in ResourceGroups)
		{
			if (!group.ShowInDashboard || !group.IsEnabledFor(builder) || !group.HasDashboardContent())
				continue;

			var node = builder
				.AddResource(new ResourceKitGroupResource(DashboardResourceName(builder, group.Name)))
				.WithInitialState(
					new()
					{
						ResourceType = ResourceKitGroupResource.ResourceTypeName,
						State = new(KnownResourceStates.Active, KnownResourceStateStyles.Success),
						IconName = group.Icon ?? ResourceKitGroupResource.IconName,
						Properties = [],
					}
				)
				.ExcludeFromManifest();

			// URLs go on as annotations, not in the initial snapshot: the orchestrator replaces a snapshot's URLs
			// with the ones it computes from the resource's annotations. A group has no endpoints, so only an
			// absolute URL can be shown.
			foreach (var url in group.Urls.Where(static u => u.IsEnabled && u.IsAbsolute))
			{
				node.WithAnnotation(
					new ResourceUrlAnnotation
					{
						Url = url.Url!,
						DisplayText = url.DisplayText,
						DisplayLocation = url.DisplayLocation,
					}
				);
			}

			var parentNode = group.Parent?.NearestDashboardResource();
			if (parentNode is not null)
				node.WithParentRelationship(parentNode);

			group.DashboardResource = node;
		}
	}

	void PlaceResourcesInGroupNodes()
	{
		List<(IResource Resource, IResourceBuilder<ResourceKitGroupResource> Node)> placements = [];
		HashSet<IResource> kitResources = new(ReferenceEqualityComparer.Instance);
		foreach (var resource in Resources)
		{
			if (!resource.IsEnabled || resource is not IResourceBuilder<IResource> resourceBuilder)
				continue;

			kitResources.Add(resourceBuilder.Resource);

			var groups = GetGroups(resource);
			var parentNode = groups.Count == 0 ? null : groups[0].NearestDashboardResource();
			if (parentNode is not null)
				placements.Add((resourceBuilder.Resource, parentNode));
		}

		// Then the resources groups included explicitly, after every kit, so a kit is always placed by its own
		// membership; parents first, so a resource included in two groups stays in the first one listed.
		foreach (var group in ResourceGroups)
		{
			var parentNode = group.NearestDashboardResource();
			if (parentNode is null)
				continue;

			foreach (var included in group.IncludedResources)
				placements.Add((included, parentNode));
		}

		// First the resources with no parent of their own, so a kit that another kit's resource hangs off is
		// placed before anything walks up to it.
		foreach (var (resource, node) in placements)
		{
			if (ParentOf(resource) is null)
				resource.Annotations.Add(new ResourceRelationshipAnnotation(node.Resource, ParentRelationshipType));
		}

		// Then the kits that already have a parent. The dashboard shows one parent per resource, so the kit
		// keeps its own, and the top of its chain is nested instead: an integration that builds its own tree
		// (AspireC4's diagram group, a database under its server) lands in the group with its shape intact.
		foreach (var (resource, node) in placements)
		{
			if (ParentOf(resource) is null)
				continue;

			var root = TopOfParentChain(resource);

			// Already in a group, or under another kit, which decides where its own tree goes.
			if (root is null || root is ResourceKitGroupResource || kitResources.Contains(root))
				continue;

			root.Annotations.Add(new ResourceRelationshipAnnotation(node.Resource, ParentRelationshipType));
		}
	}

	static IResource? ParentOf(IResource resource) =>
		resource
			.Annotations.OfType<ResourceRelationshipAnnotation>()
			.FirstOrDefault(static a => a.Type == ParentRelationshipType)
			?.Resource;

	// The ancestor with no parent. The chain belongs to whoever built it, so it is walked defensively: a
	// resource seen twice is a cycle, and a cycle has no top to nest, so null is returned.
	static IResource? TopOfParentChain(IResource resource)
	{
		HashSet<IResource> seen = new(ReferenceEqualityComparer.Instance) { resource };
		var current = resource;
		while (ParentOf(current) is { } parent)
		{
			if (!seen.Add(parent))
				return null;

			current = parent;
		}

		return current;
	}
}
