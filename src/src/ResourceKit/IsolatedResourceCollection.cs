namespace Purview.Aspire.ResourceKit;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public sealed class IsolatedResourceCollection(AppIsolationContext context)
{
	readonly List<IIsolatedResource> _resources = [];

	public AppIsolationContext Context { get; } = context;

	public TResource Add<TResource>(TResource resource)
		where TResource : class, IIsolatedResource
	{
		_resources.Add(resource);
		return resource;
	}

	public TResource Get<TResource>()
		where TResource : class, IIsolatedResource => _resources.OfType<TResource>().First();

	public void Initialize(IDistributedApplicationBuilder builder)
	{
		foreach (var resource in _resources)
			resource.BuildResource(builder, Context);

		foreach (var resource in _resources)
			resource.ConfigureResource(this, Context);
	}
}
