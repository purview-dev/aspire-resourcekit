using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

public sealed class GeneratorCachingTests
{
	[Test]
	public async Task GeneratedText_QuoteLiteral_EscapesCSharpCharacters()
	{
		// Arrange
		const string value = "quote \" slash \\ newline\n";

		// Act
		var escaped = GeneratedText.QuoteLiteral(value);

		// Assert
		await Assert.That(escaped).IsEqualTo("\"quote \\\" slash \\\\ newline\\n\"");
	}

	[Test]
	public async Task GeneratorTrackingNames_AreStableConstants()
	{
		// Arrange

		// Act
		var names = new[]
		{
			GeneratorTrackingNames.HostKitTargets,
			GeneratorTrackingNames.ResourceDefinitionTargets,
			GeneratorTrackingNames.GenericResourceDefinitionTargets,
		};

		// Assert
		await Assert.That(names.Distinct().Count()).IsEqualTo(names.Length);
	}

	[Test]
	public async Task HintNameHelper_ForHost_DistinguishesNestedAndGenericIdentities()
	{
		// Arrange
		const string nestedType = "Testing.Outer+Inner`1";
		const string genericType = "Testing.Outer_Inner_1";

		// Act
		var nestedHint = HintNameHelper.ForHost(nestedType);
		var genericHint = HintNameHelper.ForHost(genericType);

		// Assert
		await Assert.That(nestedHint).IsNotEqualTo(genericHint);
		await Assert.That(nestedHint).Contains("Testing.Outer_Inner_1");
		await Assert.That(nestedHint).EndsWith(".g.cs");
	}
}
