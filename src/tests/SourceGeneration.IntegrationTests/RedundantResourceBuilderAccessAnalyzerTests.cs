using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies SG0040: a resource kit is already an <c>IResourceBuilder&lt;TResource&gt;</c>, so routing through
/// its <c>ResourceBuilder</c> property is redundant.
/// </summary>
public sealed class RedundantResourceBuilderAccessAnalyzerTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string Kits = """
		using Aspire.Hosting;
		using Aspire.Hosting.ApplicationModel;
		using Purview.Aspire.ResourceKit;

		namespace Testing;

		public sealed class FakeHost : IHostKit
		{
			public void Build(IDistributedApplicationBuilder builder) { }
			public void Configure() { }
		}

		public sealed class ParentKit : ResourceKitBase<FakeHost, DefaultAspireResource>
		{
			public ParentKit(FakeHost host) : base(host, "parent") { }

			protected override IResourceBuilder<DefaultAspireResource> BuildResource(
				IDistributedApplicationBuilder builder) => throw new global::System.NotImplementedException();
		}

		public sealed class TestAnnotation : IResourceAnnotation;
		""";

	static ResourceKitSourceGeneratorTestOptions CreateOptions()
	{
		ResourceKitSourceGeneratorTestOptions baseOptions = new();
		return baseOptions with
		{
			AnalyzerTypes = [typeof(RedundantResourceBuilderAccessAnalyzer)],
			AdditionalAssemblyTypes = baseOptions.AdditionalAssemblyTypes.Add(typeof(DefaultAspireResource)),
		};
	}

	[Test]
	public async Task Generate_GivenKitResourceBuilderCall_ReportsRedundantAccess(CancellationToken cancellationToken)
	{
		// Arrange
		var source =
			Kits
			+ """

				public sealed class Caller
				{
					public IResourceBuilder<DefaultAspireResource> Build(ParentKit parent) =>
						parent.ResourceBuilder.WithAnnotation(new TestAnnotation());
				}
				""";

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.RedundantResourceBuilderAccess);
	}

	[Test]
	public async Task Generate_GivenResourceBuilderPassedAsArgument_DoesNotReportRedundantAccess(
		CancellationToken cancellationToken
	)
	{
		// Arrange — replacing this with parent could change the target type and break compilation.
		var source =
			Kits
			+ """

				public sealed class Caller
				{
					public void Consume(ParentKit parent) => Accept(parent.ResourceBuilder);

					static void Accept(IResourceBuilder<DefaultAspireResource> builder) { }
				}
				""";

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RedundantResourceBuilderAccess);
	}

	[Test]
	public async Task Generate_GivenKitUsedDirectly_DoesNotReportRedundantAccess(CancellationToken cancellationToken)
	{
		// Arrange
		var source =
			Kits
			+ """

				public sealed class Caller
				{
					public IResourceBuilder<DefaultAspireResource> Build(ParentKit parent) =>
						parent.WithAnnotation(new TestAnnotation());
				}
				""";

		// Act
		using var result = await GenerateAsync(source, CreateOptions(), cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.RedundantResourceBuilderAccess);
	}
}
