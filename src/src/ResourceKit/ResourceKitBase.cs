using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Provides a base implementation for a host-app resource with build and configure lifecycle hooks.
/// </summary>
/// <typeparam name="THostKit">The host application type.</typeparam>
/// <typeparam name="TResource">The Aspire <see cref="IResource"/> type this kit builds.</typeparam>
public abstract class ResourceKitBase<THostKit, TResource> : IResourceKit<THostKit>, IResourceBuilder<TResource>
	where THostKit : class, IHostKit
	where TResource : class, IResource
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceKitBase{THostKit, TResource}"/> class.
	/// </summary>
	/// <param name="hostKit">The Host Kit application instance.</param>
	/// <param name="name">
	/// Optional logical resource name. When not provided, the runtime type name is used.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="hostKit"/> is <see langword="null"/>.</exception>
	protected ResourceKitBase(THostKit hostKit, string? name = null)
	{
		HostKit = hostKit ?? throw new ArgumentNullException(nameof(hostKit));
		Name = string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
	}

	/// <inheritdoc />
	public THostKit HostKit { get; init; }

	/// <inheritdoc/>
	public string Name
	{
		get;
		init
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
	protected virtual bool IsResourceEnabled(IDistributedApplicationBuilder builder) => IsEnabled;

	/// <summary>
	/// Builds and returns the resource builder for this resource.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <returns>The resource builder created for this resource.</returns>
	protected abstract IResourceBuilder<TResource> BuildResource(IDistributedApplicationBuilder builder);

	/// <summary>
	/// Configures cross-resource behavior after all resources have been built.
	/// </summary>
	protected virtual void ConfigureResource() { }

	/// <inheritdoc/>
	public void Build(IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentException.ThrowIfNullOrWhiteSpace(Name, nameof(Name));

		IsEnabled = IsResourceEnabled(builder);
		if (!IsEnabled)
			return;

		ResourceBuilder = BuildResource(builder);
	}

	/// <inheritdoc/>
	public void Configure()
	{
		if (!IsEnabled)
			return;

		ConfigureResource();
	}

	[DebuggerHidden]
	[StackTraceHidden]
	void GuardEnabled()
	{
		if (!IsEnabled)
			throw new InvalidOperationException($"The '{Name}' resource is not enabled and cannot be accessed.");
	}

	/// <inheritdoc />
	public IDistributedApplicationBuilder ApplicationBuilder => ResourceBuilder.ApplicationBuilder;

	/// <inheritdoc />
	public TResource Resource => ResourceBuilder.Resource;

	/// <inheritdoc />
	public IResourceBuilder<TResource> WithAnnotation<TAnnotation>(
		TAnnotation annotation,
		ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append
	)
		where TAnnotation : IResourceAnnotation
	{
		return ResourceBuilder.WithAnnotation(annotation, behavior);
	}
}
