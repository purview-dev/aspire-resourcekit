using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Example17.Fixtures;

public sealed class Example17AppHostFixture<TAppHost> : AspireFixture<TAppHost>
	where TAppHost : class;
