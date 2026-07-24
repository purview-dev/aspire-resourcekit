using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Fixtures;

public sealed class ExampleAppHostFixture<TAppHost> : AspireFixture<TAppHost>
	where TAppHost : class;
