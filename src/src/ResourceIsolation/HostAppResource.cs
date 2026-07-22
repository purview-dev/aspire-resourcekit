using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceIsolation;

public abstract class HostAppResource<THostApp, TResource> : IHostAppResource<THostApp>
	where THostApp : class
	where TResource : class, IResource
{
	public abstract string Name { get; }

	public bool IsEnabled { get; private set; } = true;

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

	protected virtual bool IsResourceEnabled(
		[NotNull] IDistributedApplicationBuilder builder,
		IServiceProvider services
	) => IsResourceEnabled(builder);

	protected abstract IResourceBuilder<TResource> Build(IDistributedApplicationBuilder builder);

	protected virtual void Configure(THostApp app) { }

	protected virtual void Configure(THostApp app, IServiceProvider services) => Configure(app);

	public void BuildResource(IDistributedApplicationBuilder builder, IServiceProvider services)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(services);

		IsEnabled = IsResourceEnabled(builder, services);
		if (!IsEnabled)
			return;

		ResourceBuilder = Build(builder);
	}

	public void ConfigureResource(THostApp app, IServiceProvider services)
	{
		ArgumentNullException.ThrowIfNull(app);
		ArgumentNullException.ThrowIfNull(services);

		if (!IsEnabled)
			return;

		Configure(app, services);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	void GuardEnabled()
	{
		if (!IsEnabled)
			throw new InvalidOperationException($"The '{Name}' resource is not enabled and cannot be accessed.");
	}
}
