namespace Purview.Aspire.ResourceIsolation;

public sealed class IsolationSettingsTests
{
	[Test]
	public async Task CreateScoped_UsesConfiguredSuffixGenerator()
	{
		var generator = IIsolationSuffixGenerator.Mock();
		generator.CreateSuffix().Returns("abc123");

		var settings = IsolationSettings.CreateScoped(namePrefix: "test", suffixGenerator: generator.Object);

		await Assert.That(settings.NamePrefix).IsEqualTo("test");
		await Assert.That(settings.NameSuffix).IsEqualTo("abc123");
	}

	[Test]
	public async Task IsResourceDisabled_UsesCaseInsensitiveComparison()
	{
		var settings = new IsolationSettings();
		settings.DisabledResources.Add("Storage");

		await Assert.That(settings.IsResourceDisabled("storage")).IsTrue();
		await Assert.That(settings.IsResourceDisabled("api")).IsFalse();
	}
}
