using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

public sealed class DefaultAspireResource : IResource
{
	public string Name => nameof(DefaultAspireResource);

	public ResourceAnnotationCollection Annotations => [];
}
