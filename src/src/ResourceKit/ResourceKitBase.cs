using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Provides a base implementation for a host-app resource with build and configure lifecycle hooks.
/// </summary>
/// <typeparam name="THostKit">The host application type.</typeparam>
/// <typeparam name="TResource">The Aspire <see cref="IResource"/> type this kit builds.</typeparam>
public abstract class ResourceKitBase<THostKit, TResource>
	: IResourceKit<THostKit>,
		IResourceKitGating,
		IResourceBuilder<TResource>
	where THostKit : class, IHostKit
	where TResource : class, IResource
{
	bool _hostEnabled = true;
	bool _groupsEnabled = true;
	bool _ancestorsEnabled = true;
	bool? _configuredIsEnabled;
	bool? _effectiveIsEnabled;
	ResourceKitLifecycleState _state = ResourceKitLifecycleState.Unbuilt;

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

	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceKitBase{THostKit, TResource}"/> class that is
	/// owned by <paramref name="parent"/>.
	/// </summary>
	/// <param name="hostKit">The Host Kit application instance.</param>
	/// <param name="parent">The resource kit that owns this resource kit.</param>
	/// <param name="name">
	/// Optional logical resource name. When not provided, the runtime type name is used.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="hostKit"/> or <paramref name="parent"/> is <see langword="null"/>.
	/// </exception>
	protected ResourceKitBase(THostKit hostKit, IResourceKit<THostKit> parent, string? name = null)
		: this(hostKit, name)
	{
		ParentKit = parent ?? throw new ArgumentNullException(nameof(parent));
	}

	/// <inheritdoc />
	public THostKit HostKit { get; init; }

	/// <inheritdoc />
	public IResourceKit<THostKit>? ParentKit { get; protected init; }

	/// <summary>
	/// Gets the value of <see cref="IsEnabled"/> when this kit entered the build phase.
	/// </summary>
	/// <remarks>
	/// <see cref="IsEnabled"/> keeps its historical meaning: it is the configured value before
	/// <see cref="Build"/> and the effective value afterwards. This property retains the configured value,
	/// so a host can explain why a kit did not build even though its options enabled it.
	/// </remarks>
	public bool ConfiguredIsEnabled => _configuredIsEnabled ?? IsEnabled;

	/// <summary>
	/// Gets whether this kit participates in the current run after every gate has been applied.
	/// </summary>
	/// <remarks>
	/// Before <see cref="Build"/> this mirrors <see cref="IsEnabled"/>. Afterwards it reflects the host mode,
	/// the kit's groups, every ancestor, the configured <see cref="IsEnabled"/>, the kit <see cref="Mode"/>
	/// and <see cref="IsResourceEnabled"/>.
	/// </remarks>
	public bool EffectiveIsEnabled => _effectiveIsEnabled ?? IsEnabled;

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
	/// Gets the execution mode in which this resource participates.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Defaults to <see cref="ResourceKitMode.All"/>. Generated resource kits override this member to surface the
	/// configured <c>Mode</c> option; manual resource kits can override it directly.
	/// </para>
	/// <para>
	/// The mode is evaluated by <see cref="Build"/> before <see cref="IsResourceEnabled"/> is invoked, so a
	/// resource excluded by its mode never runs that hook and never builds.
	/// </para>
	/// </remarks>
	protected virtual ResourceKitMode Mode => ResourceKitMode.All;

	/// <summary>
	/// Gets the named URLs the dashboard shows for this resource.
	/// </summary>
	/// <remarks>
	/// Defaults to none. Generated resource kits override this member with the URLs declared by
	/// <c>[ResourceUrl]</c> and <c>[EndpointUrl]</c>, as bound from options; manual resource kits can override it
	/// directly. The URLs are applied after <see cref="BuildResource"/> returns, through
	/// <see cref="ResourceKitUrl.Apply"/>.
	/// </remarks>
	protected virtual IEnumerable<ResourceKitUrl> Urls => [];

	/// <summary>
	/// Gets the underlying Aspire resource builder for this resource.
	/// </summary>
	/// <remarks>
	/// Accessing this property before the kit has built, or after it was disabled or failed, throws an
	/// <see cref="InvalidOperationException"/>.
	/// </remarks>
	public IResourceBuilder<TResource> ResourceBuilder
	{
		get
		{
			GuardAccessible();
			return field;
		}
		private set
		{
			GuardAccessible();
			field = value;
		}
	} = default!;

	ResourceKitLifecycleState IResourceKitGating.State => _state;

	bool IResourceKitGating.EffectiveIsEnabled => EffectiveIsEnabled;

	void IResourceKitGating.ApplyGates(bool hostEnabled, bool groupsEnabled, bool ancestorsEnabled)
	{
		_hostEnabled = hostEnabled;
		_groupsEnabled = groupsEnabled;
		_ancestorsEnabled = ancestorsEnabled;
	}

	/// <summary>
	/// Determines whether this resource should be built for the current application run.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <returns><see langword="true"/> when the resource should be built; otherwise <see langword="false"/>.</returns>
	/// <remarks>
	/// The default implementation returns <see cref="IsEnabled"/>. This hook is only invoked when
	/// <see cref="IsEnabled"/> is already <see langword="true"/> and <see cref="Mode"/> participates in the
	/// current run; a disabled or mode-excluded kit is never re-enabled here.
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
	/// <remarks>
	/// <para>
	/// A kit builds at most once. The host kit builds in parent-before-child order; calling
	/// <see cref="Build"/> on a kit whose parent has not finished building throws
	/// <see cref="InvalidOperationException"/> rather than letting the child use a missing parent builder.
	/// </para>
	/// <para>
	/// Nothing is created when a composition gate excludes the kit: the host mode, any group the kit belongs
	/// to, or any disabled ancestor. An excluded kit never invokes <see cref="IsResourceEnabled"/>.
	/// </para>
	/// </remarks>
	public void Build(IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentException.ThrowIfNullOrWhiteSpace(Name, nameof(Name));

		if (_state != ResourceKitLifecycleState.Unbuilt)
			return;

		_configuredIsEnabled = IsEnabled;

		var gatesEnabled = _hostEnabled && _groupsEnabled && _ancestorsEnabled;
		if (gatesEnabled && ParentKit is { } parent)
			gatesEnabled = ParentAllowsBuild(parent);

		if (!gatesEnabled)
		{
			Disable();
			return;
		}

		if (IsEnabled)
			IsEnabled = Mode.IsEnabledFor(builder) && IsResourceEnabled(builder);

		if (!IsEnabled)
		{
			Disable();
			return;
		}

		_state = ResourceKitLifecycleState.Building;
		try
		{
			ResourceBuilder = BuildResource(builder);

			ResourceKitUrl[] urls = [.. Urls.Where(static u => u.IsEnabled)];
			if (urls.Length > 0)
				ResourceBuilder.WithResourceKitUrls(urls);

			_effectiveIsEnabled = true;
			_state = ResourceKitLifecycleState.Built;
		}
		catch
		{
			_effectiveIsEnabled = false;
			_state = ResourceKitLifecycleState.Failed;
			throw;
		}
	}

	/// <inheritdoc/>
	/// <remarks>
	/// A kit configures at most once, and only after it built successfully. A kit whose parent failed to build
	/// or configure is skipped, so a failed parent cannot leave a usable descendant.
	/// </remarks>
	public void Configure()
	{
		if (_state != ResourceKitLifecycleState.Built)
			return;

		if (
			ParentKit is IResourceKitGating parent
			&& parent.State
				is not (
					ResourceKitLifecycleState.Built
					or ResourceKitLifecycleState.Configuring
					or ResourceKitLifecycleState.Configured
				)
		)
			return;

		_state = ResourceKitLifecycleState.Configuring;
		try
		{
			ConfigureResource();
			_state = ResourceKitLifecycleState.Configured;
		}
		catch
		{
			_state = ResourceKitLifecycleState.Failed;
			throw;
		}
	}

	void Disable()
	{
		IsEnabled = false;
		_effectiveIsEnabled = false;
		_state = ResourceKitLifecycleState.Disabled;
	}

	static bool ParentAllowsBuild(IResourceKit<THostKit> parent)
	{
		if (parent is not IResourceKitGating gating)
			return parent.IsEnabled;

		return gating.State switch
		{
			ResourceKitLifecycleState.Unbuilt or ResourceKitLifecycleState.Building =>
				throw new InvalidOperationException(
					$"The '{parent.Name}' resource kit must finish building before its children can build."
				),
			ResourceKitLifecycleState.Failed => throw new InvalidOperationException(
				$"The '{parent.Name}' resource kit failed to build, so its children cannot build."
			),
			ResourceKitLifecycleState.Disabled => false,
			_ => gating.EffectiveIsEnabled,
		};
	}

	[DebuggerHidden]
	[StackTraceHidden]
	void GuardAccessible()
	{
		if (!IsEnabled)
			throw new InvalidOperationException($"The '{Name}' resource is not enabled and cannot be accessed.");

		if (_state == ResourceKitLifecycleState.Unbuilt)
			throw new InvalidOperationException($"The '{Name}' resource has not been built and cannot be accessed.");

		if (_state == ResourceKitLifecycleState.Failed)
			throw new InvalidOperationException(
				$"The '{Name}' resource failed to build or configure and cannot be accessed."
			);
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
