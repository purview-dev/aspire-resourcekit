using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit;

public sealed class DelegateIsolatedResource<TResource>(
	string resourceKey,
	string defaultName,
	Func<IDistributedApplicationBuilder, AppIsolationContext, string, IResourceBuilder<TResource>> build,
	Action<IsolatedResourceCollection, AppIsolationContext, IResourceBuilder<TResource>>? configure = null,
	Func<AppIsolationContext, bool>? isEnabled = null
) : IsolatedResource<TResource>
	where TResource : class, IResource
{
	readonly Func<AppIsolationContext, bool>? _isEnabled = isEnabled;
	readonly Func<IDistributedApplicationBuilder, AppIsolationContext, string, IResourceBuilder<TResource>> _build =
		build;
	readonly Action<IsolatedResourceCollection, AppIsolationContext, IResourceBuilder<TResource>>? _configure =
		configure;

	public override string ResourceKey { get; } = resourceKey;

	public override string DefaultName { get; } = defaultName;

	protected override bool IsResourceEnabled([NotNull] AppIsolationContext context) =>
		base.IsResourceEnabled(context) && (_isEnabled?.Invoke(context) ?? true);

	protected override IResourceBuilder<TResource> Build(
		IDistributedApplicationBuilder builder,
		AppIsolationContext context,
		string resolvedName
	) => _build(builder, context, resolvedName);

	protected override void Configure(IsolatedResourceCollection app, AppIsolationContext context) =>
		_configure?.Invoke(app, context, ResourceBuilder);
}
