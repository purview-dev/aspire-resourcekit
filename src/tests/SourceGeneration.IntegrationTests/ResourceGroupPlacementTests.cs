using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Verifies where a <c>[ResourceGroup]</c> class may be declared (SG0045) and how its accessibility must
/// pair with the host kit's (SG0046). The generated group copies the declared modifier as a second partial
/// declaration and is always emitted beside the host kit, so neither problem can be corrected in generated
/// code: each is reported against the declaration instead of surfacing as CS0426/CS9338/CS0053.
/// </summary>
public sealed class ResourceGroupPlacementTests : ResourceKitSourceGeneratorTestBase<HostKitGenerator>
{
	const string Kit = """
		protected override IResourceBuilder<DefaultAspireResource> BuildResource(IDistributedApplicationBuilder builder) =>
			throw new global::System.NotImplementedException();
		""";

	[Test]
	public async Task Generate_GivenGroupNestedInAContainer_ReportsResourceGroupMustBeTopLevel(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			static class ResourceNames
			{
				public static class Groups
				{
					[ResourceGroup("Services")]
					public sealed partial class Services;
				}
			}

			[HostKit]
			sealed partial class TestingHostKit;

			[ResourceDefinition<DefaultAspireResource>("api")]
			[InGroup<ResourceNames.Groups.Services>]
			sealed partial class ApiKit;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert — one actionable diagnostic on the attribute, and nothing generated to fail in its place.
		await Assert
			.That((result.AnalyzerResult?.Diagnostics ?? []).Select(static d => d.Id))
			.IsEquivalentTo([DiagnosticLibrary.ResourceGroupMustBeTopLevel.Id]);
		await Assert.That(result).HasDiagnostics(DiagnosticLibrary.ResourceGroupMustBeTopLevel, 1);
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ResourceGroupAccessibilityMismatch);
		await Assert
			.That(Reporting(result, DiagnosticLibrary.ResourceGroupMustBeTopLevel))
			.IsEquivalentTo(["ResourceGroup(\"Services\")"]);
		await Assert.That(result.DriverResult.GeneratedTrees.Length).IsEqualTo(ExpectedGeneratedFileCount);
	}

	[Test]
	public async Task Generate_GivenGroupNestedInAnInternalContainer_ReportsOnlyThePlacementRule(
		CancellationToken cancellationToken
	)
	{
		// Arrange — the group's effective accessibility is internal, so the host kit pairing is not a
		// second problem; the placement is.
		const string source = """
			namespace Testing;

			static class ResourceNames
			{
				[ResourceGroup]
				internal sealed partial class Services;
			}

			[HostKit]
			sealed partial class TestingHostKit;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostics(DiagnosticLibrary.ResourceGroupMustBeTopLevel, 1);
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ResourceGroupAccessibilityMismatch);
	}

	[Test]
	public async Task Generate_GivenPublicGroupWithAnInternalHostKit_ReportsAccessibilityMismatch(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[HostKit]
			sealed partial class TestingHostKit;

			[ResourceGroup]
			public sealed partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostics(DiagnosticLibrary.ResourceGroupAccessibilityMismatch, 1);
		await Assert
			.That(Reporting(result, DiagnosticLibrary.ResourceGroupAccessibilityMismatch))
			.IsEquivalentTo(["ResourceGroup"]);
		await Assert.That(result.DriverResult.GeneratedTrees.Length).IsEqualTo(ExpectedGeneratedFileCount);
	}

	[Test]
	public async Task Generate_GivenInternalGroupWithAPublicHostKit_ReportsAccessibilityMismatch(
		CancellationToken cancellationToken
	)
	{
		// Arrange — the inverse pairing: the public host kit would expose an internal group through its
		// public Groups accessor, which is also a compile error (CS0053).
		const string source = """
			namespace Testing;

			[HostKit]
			public sealed partial class TestingHostKit;

			[ResourceGroup]
			sealed partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(
			source,
			ResourceKitSourceGeneratorTestOptions.NoValidation,
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostics(DiagnosticLibrary.ResourceGroupAccessibilityMismatch, 1);
		await Assert.That(result.DriverResult.GeneratedTrees.Length).IsEqualTo(ExpectedGeneratedFileCount);
	}

	[Test]
	public async Task Generate_GivenChildKitWithNoGroup_RegistersItWithoutAGroup(CancellationToken cancellationToken)
	{
		// Arrange — a child with no [InGroup] is registered with no group and inherits the parent's primary
		// group when the host seals its resources, so the generator must not warn (SG0039) about it.
		var source = $$"""
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup]
			sealed partial class DataGroup;

			[ResourceDefinition<DefaultAspireResource>("parent")]
			[InGroup<DataGroup>]
			sealed partial class ParentKit
			{
				{{Kit}}
			}

			[ResourceDefinition<DefaultAspireResource>("child", Parent = typeof(ParentKit))]
			sealed partial class ChildKit
			{
				{{Kit}}
			}
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ResourceKitChildGroupDiffersFromParent);

		var generated = result.GetSource();
		await Assert.That(generated).Contains("AddResource(Parent, Groups.Data);");
		await Assert.That(generated).Contains("AddResource(Parent.Child);");
	}

	[Test]
	public async Task Generate_GivenInternalGroupWithAnInternalHostKit_EmitsTheGroupUnchanged(
		CancellationToken cancellationToken
	)
	{
		// Arrange — the shape every existing composition uses. The emitted group, and its options, are
		// pinned so a change to the emission shows up as a diff rather than passing unnoticed.
		const string source = """
			namespace Testing;

			[HostKit]
			partial class TestingHostKit;

			[ResourceGroup("data", ShowInDashboard = false)]
			sealed partial class DataGroup;
			""";

		// Act
		using var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert
			.That(GeneratedGroup(result.GetSource(), "DataGroup"))
			.IsEqualTo(await ReadGoldenAsync("ResourceGroup.InternalTopLevelGroup.g.cs.txt", cancellationToken));
	}

	// The source text each diagnostic points at, which for a group rule is the attribute or the declaration.
	static string[] Reporting(DriverRunResult result, DiagnosticDescriptor descriptor) =>
		[
			.. (result.AnalyzerResult?.Diagnostics ?? [])
				.Where(d => d.Id == descriptor.Id && d.Location.SourceTree is not null)
				.Select(static d => d.Location.SourceTree!.GetText().ToString(d.Location.SourceSpan))
				.Order(StringComparer.Ordinal),
		];

	// The emitted group declaration, from its first line to its matching closing brace, with the generator
	// version stamp normalised so the golden text does not change with every commit.
	static string GeneratedGroup(string generated, string groupName)
	{
		var normalized = NormalizeVersionStamp(generated);
		var lines = normalized.Split('\n');
		var start = Array.FindIndex(lines, line =>
			line.Contains($" partial class {groupName} :", StringComparison.Ordinal)
		);
		if (start < 0)
			throw new InvalidOperationException($"The generated source does not declare '{groupName}'.");

		List<string> declaration = [];
		var depth = 0;
		var opened = false;
		foreach (var line in lines[start..])
		{
			declaration.Add(line);

			foreach (var character in line)
			{
				if (character == '{')
				{
					depth++;
					opened = true;
				}
				else if (character == '}')
				{
					depth--;
				}
			}

			if (opened && depth == 0)
				return string.Join('\n', declaration);
		}

		throw new InvalidOperationException($"The generated '{groupName}' declaration is not closed.");
	}

	// The checked-in golden, read from the test output directory so a working-tree checkout with CRLF
	// compares equal to the generated LF text.
	static async Task<string> ReadGoldenAsync(string fileName, CancellationToken cancellationToken) =>
		(
			await System.IO.File.ReadAllTextAsync(
				System.IO.Path.Combine(AppContext.BaseDirectory, "Golden", fileName),
				cancellationToken
			)
		).Replace("\r\n", "\n", StringComparison.Ordinal);

	// The emitted stamp is GeneratedCode("HostKitGenerator", "<version>+<commit>").
	static string NormalizeVersionStamp(string generated)
	{
		const string stampStart = "\"HostKitGenerator\", \"";
		const string stampEnd = "\")]";
		var index = generated.IndexOf(stampStart, StringComparison.Ordinal);
		while (index >= 0)
		{
			var valueStart = index + stampStart.Length;
			var valueEnd = generated.IndexOf(stampEnd, valueStart, StringComparison.Ordinal);
			if (valueEnd < 0)
				return generated;

			generated = generated[..valueStart] + "<version>" + generated[valueEnd..];
			index = generated.IndexOf(stampStart, valueStart + "<version>".Length, StringComparison.Ordinal);
		}

		return generated;
	}
}
