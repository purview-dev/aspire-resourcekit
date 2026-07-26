using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Provides a base implementation for a host-app resource with build and configure lifecycle hooks.
/// </summary>
/// <typeparam name="THostApp">The host application type.</typeparam>
/// <typeparam name="TResource">The Aspire resource type this kit builds.</typeparam>
public abstract class ResourceKitBase<THostApp, TResource> : IAppResourceKit<THostApp>
	where THostApp : class, IHostApp
	where TResource : class, IResource
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceKitBase{THostApp, TResource}"/> class.
	/// </summary>
	/// <param name="name">
	/// Optional logical resource name. When not provided, the runtime type name is used.
	/// </param>
	protected ResourceKitBase(string? name = null)
	{
		Name = string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
	}

	/// <inheritdoc/>
	public string Name
	{
		get;
		set
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(value);
			field = value;
		}
	}

	/// <inheritdoc/>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets the underlying Aspire resource builder for this resource.
	/// </summary>
	/// <remarks>
	/// Accessing this property while the resource is disabled throws an <see cref="InvalidOperationException"/>.
	/// </remarks>
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

	/// <summary>
	/// Determines whether this resource should be built for the current application run.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <returns><see langword="true"/> when the resource should be built; otherwise <see langword="false"/>.</returns>
	/// <remarks>
	/// The default implementation returns <see cref="IsEnabled"/>.
	/// </remarks>
	protected virtual bool IsResourceEnabled([NotNull] IDistributedApplicationBuilder builder) => IsEnabled;

	/// <summary>
	/// Builds and returns the resource builder for this resource.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <returns>The resource builder created for this resource.</returns>
	protected abstract IResourceBuilder<TResource> BuildResource(IDistributedApplicationBuilder builder);

	/// <summary>
	/// Configures cross-resource behavior after all resources have been built.
	/// </summary>
	/// <param name="app">The host application instance.</param>
	protected virtual void ConfigureResource(THostApp app) { }

	/// <inheritdoc/>
	public void Build([NotNull] IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentException.ThrowIfNullOrWhiteSpace(Name, nameof(Name));

		IsEnabled = IsResourceEnabled(builder);
		if (!IsEnabled)
			return;

		ResourceBuilder = BuildResource(builder);
	}

	/// <inheritdoc/>
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
