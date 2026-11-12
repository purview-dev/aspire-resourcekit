using Purview.Aspire.ResourceKit.SourceGeneration.CodeFixes;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies that the SG0040 code fix removes the redundant <c>.ResourceBuilder</c> hop while preserving the
/// rest of the fluent chain.
/// </summary>
public sealed record RedundantResourceBuilderCodeFixTestOptions : CodeFixTestOptions
{
	public RedundantResourceBuilderCodeFixTestOptions()
	{
		List<Type> additionalTypes =
		[
			typeof(ResourceKitBase<,>),
			typeof(IHostKit),
			typeof(DefaultAspireResource),
			typeof(global::Aspire.Hosting.ApplicationModel.IResource),
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder),
		];

		AdditionalAssemblyTypes = [.. additionalTypes];
		AdditionalNamespaces =
		[
			typeof(DefaultAspireResource).Namespace!,
			typeof(ResourceKitBase<,>).Namespace!,
			typeof(global::Aspire.Hosting.ApplicationModel.IResource).Namespace!,
			typeof(global::Aspire.Hosting.IDistributedApplicationBuilder).Namespace!,
		];
	}
}

public sealed class RedundantResourceBuilderAccessCodeFixTests
	: TUnitCodeFixTestBase<
		RedundantResourceBuilderAccessAnalyzer,
		RedundantResourceBuilderAccessCodeFixProvider,
		RedundantResourceBuilderCodeFixTestOptions
	>
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

	[Test]
	public async Task ApplyCodeFix_GivenKitResourceBuilderAccess_RemovesTheHop(CancellationToken cancellationToken)
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
		var result = await ApplyCodeFixAsync(
			source,
			new RedundantResourceBuilderCodeFixTestOptions(),
			cancellationToken
		);

		// Assert
		var fixedCodeSource = await result.FixedCode().Trees.First().GetTextAsync(cancellationToken);
		var fixedCode = fixedCodeSource.ToString();

		await Assert.That(fixedCode).Contains("parent.WithAnnotation(new TestAnnotation())");
		await Assert.That(fixedCode).DoesNotContain("parent.ResourceBuilder.WithAnnotation");
	}
}
