using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

public abstract class HostResourceBase<THostApp, TResource> : IAppResource<THostApp>
	where THostApp : class, IHostApp
	where TResource : class, IResource
{
	protected HostResourceBase(string? name = null)
	{
		Name = string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
	}

	public string Name
	{
		get;
		set
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(value);
			field = value;
		}
	}

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

	// Unless overridden, this methods returns the value of IsEnabled, which is true by default.
	protected virtual bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) => IsEnabled;

	protected abstract IResourceBuilder<TResource> BuildResource(IDistributedApplicationBuilder builder);

	protected virtual void ConfigureResource(THostApp app) { }

	public void Build([NotNull] IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentException.ThrowIfNullOrWhiteSpace(Name, nameof(Name));

		IsEnabled = IsResourceEnabled(builder);
		if (!IsEnabled)
			return;

		ResourceBuilder = BuildResource(builder);
	}

	public void Configure([NotNull] THostApp app)
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
