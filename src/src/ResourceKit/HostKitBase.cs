using System.Collections.Immutable;
using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Provides a base implementation for host applications composed of <see cref="IResourceKit{THostApp}"/> resources.
/// </summary>
/// <typeparam name="THostKit">The concrete host kit type.</typeparam>
public abstract class HostKitBase<THostKit> : IHostKit
	where THostKit : class, IHostKit
{
	readonly List<IResourceKit<THostKit>> _resources = [];
	bool _resourcesSealed;

	/// <summary>
	/// Gets the resources managed by this host kit.
	/// </summary>
	/// <remarks>The resources will be empty until <see cref="SealResources"/> is called.</remarks>
	protected ImmutableArray<IResourceKit<THostKit>> Resources { get; private set; } = [];

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
	public void AddResource(IResourceKit<THostKit> resource)
	{
		if (_resourcesSealed)
			throw new InvalidOperationException("Resources cannot be added after the host kit has been sealed.");

		ArgumentNullException.ThrowIfNull(resource);

		_resources.Add(resource);
	}

	bool SealResources()
	{
		if (_resourcesSealed)
			return false;

		_resourcesSealed = true;
		Resources = [.. _resources];

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
			if (!modeEnabled)
				resource.IsEnabled = false;

			resource.Build(builder);
		}
	}

	/// <inheritdoc/>
	public virtual void Configure()
	{
		foreach (var resource in Resources)
			resource.Configure();
	}
}
