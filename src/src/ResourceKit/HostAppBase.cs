using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

public abstract class HostAppBase<THostApp> : IHostApp
	where THostApp : HostAppBase<THostApp>, IHostApp
{
	protected ImmutableArray<IAppResource<THostApp>> Resources { get; set; }

	public virtual void Build([NotNull] IDistributedApplicationBuilder builder)
	{
		foreach (var resource in Resources)
			resource.Build(builder);
	}

	public virtual void Configure()
	{
		var hostApp = (THostApp)this;
		foreach (var resource in Resources)
			resource.Configure(hostApp);
	}
}
