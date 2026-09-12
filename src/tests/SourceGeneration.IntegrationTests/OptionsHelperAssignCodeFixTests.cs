using Purview.Aspire.ResourceKit.SourceGeneration.CodeFixes;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies that <see cref="OptionsHelperAssignCodeFixProvider"/> splits a block-bodied
/// <c>OptionsHelper.Assign</c> action assigning multiple property paths into one assignment per argument.
/// </summary>
public sealed record OptionsHelperCodeFixTestOptions : CodeFixTestOptions
{
	public OptionsHelperCodeFixTestOptions()
	{
		AdditionalAssemblyTypes = [typeof(OptionsHelper)];
	}
}

public class OptionsHelperAssignCodeFixTests
	: TUnitCodeFixTestBase<
		OptionsHelperAssignAnalyzer,
		OptionsHelperAssignCodeFixProvider,
		OptionsHelperCodeFixTestOptions
	>
{
	const string OptionsSource = """
		public class Options
		{
			public RedisOptions Redis { get; set; } = new();
			public RedisOptions API { get; set; } = new();
		}

		public class RedisOptions
		{
			public string Name { get; set; } = "";
		}
		""";

	static string BuildSource(string callerSource) => "namespace Testing;\n" + OptionsSource + "\n" + callerSource;

	static string GetFixedCode(CodeFixTestResult result) => result.FixedCode().Trees.First().GetText().ToString();

	[Test]
	public async Task ApplyCodeFix_GivenBlockLambdaWithTwoAssignments_SplitsIntoSeparateAssignments(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string callerSource = """
			class Caller
			{
				string[] Build() =>
					Purview.Aspire.ResourceKit.OptionsHelper.Assign<Options>(o =>
					{
						o.Redis.Name = "a";
						o.API.Name = "b";
					}).Build();
			}
			""";

		// Act
		var result = await ApplyCodeFixAsync(
			BuildSource(callerSource),
			new OptionsHelperCodeFixTestOptions(),
			cancellationToken
		);

		// Assert
		var fixedCode = GetFixedCode(result);
		await Assert.That(fixedCode).Contains("o => o.Redis.Name = \"a\", o => o.API.Name = \"b\"");
		await Assert.That(fixedCode).DoesNotContain("o.Redis.Name = \"a\";");
	}

	[Test]
	public async Task ApplyCodeFix_GivenBlockLambdaWithThreeAssignments_SplitsIntoSeparateAssignments(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string callerSource = """
			class Caller
			{
				string[] Build() =>
					Purview.Aspire.ResourceKit.OptionsHelper.Assign<Options>(o =>
					{
						o.Redis.Name = "a";
						o.Redis.Name = "b";
						o.API.Name = "c";
					}).Build();
			}
			""";

		// Act
		var result = await ApplyCodeFixAsync(
			BuildSource(callerSource),
			new OptionsHelperCodeFixTestOptions(),
			cancellationToken
		);

		// Assert
		var fixedCode = GetFixedCode(result);
		await Assert
			.That(fixedCode)
			.Contains("o => o.Redis.Name = \"a\", o => o.Redis.Name = \"b\", o => o.API.Name = \"c\"");
	}
}
