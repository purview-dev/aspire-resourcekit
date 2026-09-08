using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

[GenerateTypeLibrary(ClassName = "TestingTypeLibrary")]
public static partial class TestingTypeLibraryGeneration
{
	[TypeRef(typeof(IResourceBuilder<>))]
	static readonly TypeIdentity IResourceBuilder = default;

	[TypeRef(typeof(DefaultAspireResource))]
	static readonly TypeIdentity DefaultAspireResource = default;

	[TypeRef("TestingHostKit", "Testing.HostKitNamespace")]
	static readonly TypeIdentity DefaultHostKitType = default;

	[TypeRef("TestingResourceKit", "Testing.ResourceKitNamespace")]
	static readonly TypeIdentity DefaultResourceKitType = default;
}
