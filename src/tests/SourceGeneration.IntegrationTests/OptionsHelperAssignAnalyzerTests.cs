using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies that <c>SG0020</c> is reported by <see cref="OptionsHelperAssignAnalyzer"/> when an
/// <c>OptionsHelper.Assign</c> (or chained <c>IOptionsBuilder.Assign</c>) action assigns more than one
/// property path, and is not reported for the valid single-assignment forms.
/// </summary>
public class OptionsHelperAssignAnalyzerTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
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

	static ResourceKitSourceGeneratorTestOptions CreateOptions()
	{
		ResourceKitSourceGeneratorTestOptions baseOptions = new();
		return baseOptions with
		{
			AnalyzerTypes = [typeof(OptionsHelperAssignAnalyzer)],
			AdditionalAssemblyTypes = baseOptions.AdditionalAssemblyTypes.Add(typeof(OptionsHelper)),
		};
	}

	static string BuildSource(string callerSource) => "namespace Testing;\n" + OptionsSource + "\n" + callerSource;

	[Test]
	public async Task Generate_GivenBlockLambdaWithTwoAssignments_ReportsAssignSetsMultiplePropertyPaths(
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
		var result = await GenerateAsync(BuildSource(callerSource), CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AssignSetsMultiplePropertyPaths);
	}

	[Test]
	public async Task Generate_GivenBlockLambdaWithThreeAssignments_ReportsAssignSetsMultiplePropertyPaths(
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
		var result = await GenerateAsync(BuildSource(callerSource), CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AssignSetsMultiplePropertyPaths);
	}

	[Test]
	public async Task Generate_GivenExpressionLambda_DoesNotReportAssignSetsMultiplePropertyPaths(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string callerSource = """
			class Caller
			{
				string[] Build() =>
					Purview.Aspire.ResourceKit.OptionsHelper.Assign<Options>(o => o.Redis.Name = "a").Build();
			}
			""";

		// Act
		var result = await GenerateAsync(BuildSource(callerSource), CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.AssignSetsMultiplePropertyPaths);
	}

	[Test]
	public async Task Generate_GivenSingleStatementBlock_DoesNotReportAssignSetsMultiplePropertyPaths(
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
					}).Build();
			}
			""";

		// Act
		var result = await GenerateAsync(BuildSource(callerSource), CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.AssignSetsMultiplePropertyPaths);
	}

	[Test]
	public async Task Generate_GivenSectionNameOverloadWithBlockLambda_ReportsAssignSetsMultiplePropertyPaths(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string callerSource = """
			class Caller
			{
				string[] Build() =>
					Purview.Aspire.ResourceKit.OptionsHelper.Assign<Options>("Section", o =>
					{
						o.Redis.Name = "a";
						o.API.Name = "b";
					}).Build();
			}
			""";

		// Act
		var result = await GenerateAsync(BuildSource(callerSource), CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AssignSetsMultiplePropertyPaths);
	}

	[Test]
	public async Task Generate_GivenChainedBuilderAssignWithBlockLambda_ReportsAssignSetsMultiplePropertyPaths(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string callerSource = """
			class Caller
			{
				string[] Build() =>
					Purview.Aspire.ResourceKit.OptionsHelper.Assign<Options>(o => o.Redis.Name = "a")
						.Assign<Options>(o =>
						{
							o.Redis.Name = "b";
							o.API.Name = "c";
						})
						.Build();
			}
			""";

		// Act
		var result = await GenerateAsync(BuildSource(callerSource), CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AssignSetsMultiplePropertyPaths);
	}

	[Test]
	public async Task Generate_GivenUnrelatedAssignMethod_DoesNotReportAssignSetsMultiplePropertyPaths(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string callerSource = """
			class Other
			{
				public void Assign(System.Action<Options> action) => action(new Options());
			}

			class Caller
			{
				void Run()
				{
					new Other().Assign(o =>
					{
						o.Redis.Name = "a";
						o.API.Name = "b";
					});
				}
			}
			""";

		// Act
		var result = await GenerateAsync(BuildSource(callerSource), CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.AssignSetsMultiplePropertyPaths);
	}
}
