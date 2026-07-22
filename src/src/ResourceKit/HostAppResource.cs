using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit;

public abstract class HostAppResource<THostApp, TResource> : IHostAppResource<THostApp>
	where THostApp : class
	where TResource : class, IResource
{
	public abstract string Name { get; }

	public bool IsEnabled { get; set; } = true;

	public IResourceBuilder<TResource> ResourceBuilder
	{
		get
		{
			GuardEnabled();
			return field;
		}
		private set
		{
			GuardEnabled();
			field = value;
		}
	} = default!;

	protected virtual bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) => true;

	protected abstract IResourceBuilder<TResource> BuildResource(IDistributedApplicationBuilder builder);

	protected virtual void ConfigureResource(THostApp app) { }

	public void Build(IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		IsEnabled = IsResourceEnabled(builder);
		if (!IsEnabled)
			return;

		ResourceBuilder = BuildResource(builder);
	}

	public void Configure(THostApp app)
	{
		ArgumentNullException.ThrowIfNull(app);

		if (!IsEnabled)
			return;

		ConfigureResource(app);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	void GuardEnabled()
	{
		if (!IsEnabled)
			throw new InvalidOperationException($"The '{Name}' resource is not enabled and cannot be accessed.");
	}
}
