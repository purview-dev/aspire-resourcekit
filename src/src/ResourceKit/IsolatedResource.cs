using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Purview.Aspire.ResourceKit;

public abstract class IsolatedResource<TResource> : IIsolatedResource
	where TResource : class, IResource
{
	public abstract string ResourceKey { get; }

	public abstract string DefaultName { get; }

	public string Name { get; private set; } = string.Empty;

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

	protected virtual bool IsResourceEnabled([NotNull] AppIsolationContext context) =>
		!context.Settings.IsResourceDisabled(ResourceKey);

	protected abstract IResourceBuilder<TResource> Build(
		IDistributedApplicationBuilder builder,
		AppIsolationContext context,
		string resolvedName
	);

	protected virtual void Configure(IsolatedResourceCollection app, AppIsolationContext context) { }

	public void BuildResource(IDistributedApplicationBuilder builder, AppIsolationContext context)
	{
		IsEnabled = IsResourceEnabled(context);
		if (!IsEnabled)
			return;

		Name = context.ResolveName(ResourceKey, DefaultName);
		ResourceBuilder = Build(builder, context, Name);
	}

	public void ConfigureResource(IsolatedResourceCollection app, AppIsolationContext context)
	{
		if (!IsEnabled)
			return;

		Configure(app, context);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	void GuardEnabled()
	{
		if (!IsEnabled)
			throw new InvalidOperationException($"The '{ResourceKey}' resource is disabled and cannot be accessed.");
	}
}
