using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public static class TypeLibraryExtensions
{
	static readonly TypeIdentity _iResourceBuilder = new(typeof(IResourceBuilder<>));
	static readonly TypeIdentity _defaultAspireResource = TypeIdentity.Create<DefaultAspireResource>();

	extension(TypeLibrary)
	{
		public static TypeIdentity IResourceBuilder => _iResourceBuilder;

		public static TypeIdentity DefaultAspireResource => _defaultAspireResource;
	}
}
