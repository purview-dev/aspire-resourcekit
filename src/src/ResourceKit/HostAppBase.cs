using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Provides a base implementation for host applications composed of <see cref="IAppResourceKit{THostApp}"/> resources.
/// </summary>
/// <typeparam name="THostApp">The concrete host application type.</typeparam>
public abstract class HostAppBase<THostApp> : IHostApp
	where THostApp : HostAppBase<THostApp>, IHostApp
{
	/// <summary>
	/// Gets or sets the resources managed by this host application.
	/// </summary>
	protected ImmutableArray<IAppResourceKit<THostApp>> Resources { get; set; }

	/// <inheritdoc/>
	public virtual void Build([NotNull] IDistributedApplicationBuilder builder)
	{
		foreach (var resource in Resources)
			resource.Build(builder);
	}

	/// <inheritdoc/>
	public virtual void Configure()
	{
		var hostApp = (THostApp)this;
		foreach (var resource in Resources)
			resource.Configure(hostApp);
	}
}
