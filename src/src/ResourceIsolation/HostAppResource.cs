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

	protected abstract IResourceBuilder<TResource> Build(IDistributedApplicationBuilder builder);

	protected virtual void Configure(THostApp app) { }

	public void BuildResource(IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		IsEnabled = IsResourceEnabled(builder);
		if (!IsEnabled)
			return;

		ResourceBuilder = Build(builder);
	}

	public void ConfigureResource(THostApp app)
	{
		ArgumentNullException.ThrowIfNull(app);

		if (!IsEnabled)
			return;

		Configure(app);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	void GuardEnabled()
	{
		if (!IsEnabled)
			throw new InvalidOperationException($"The '{Name}' resource is not enabled and cannot be accessed.");
	}
}
