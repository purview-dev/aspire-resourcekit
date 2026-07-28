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
	/// <summary>
	/// Gets or sets the resources managed by this host kit.
	/// </summary>
	protected ImmutableArray<IResourceKit<THostKit>> Resources { get; set; }

	/// <inheritdoc/>
	public virtual void Build(IDistributedApplicationBuilder builder)
	{
		foreach (var resource in Resources)
			resource.Build(builder);
	}

	/// <inheritdoc/>
	public virtual void Configure()
	{
		foreach (var resource in Resources)
			resource.Configure();
	}
}
