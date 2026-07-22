namespace Purview.Aspire.ResourceKit;

public sealed class AppIsolationContextTests
{
	[Test]
	public async Task ResolveName_UsesOverrideWhenPresent()
	{
		var settings = new IsolationSettings();
		settings.ResourceNameOverrides["api"] = "custom-api";

		var context = new AppIsolationContext(AppRunMode.Local, settings);
		var name = context.ResolveName("api", "default-api");

		await Assert.That(name).IsEqualTo("custom-api");
	}

	[Test]
	public async Task ResolveName_UsesPrefixAndSuffixWhenNoOverride()
	{
		var settings = new IsolationSettings { NamePrefix = "local", NameSuffix = "run01" };
		var context = new AppIsolationContext(AppRunMode.Running, settings);

		var name = context.ResolveName("storage", "blob-store");

		await Assert.That(name).IsEqualTo("local-blob-store-run01");
	}
}
