using System.ComponentModel;
using Microsoft.CodeAnalysis;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Assertions;

[EditorBrowsable(EditorBrowsableState.Never)]
static partial class DiagnosticAssertions
{
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasDiagnostic(
		this GeneratorDriverRunResult diagnostic,
		DiagnosticDescriptor expected
	) =>
		expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				!diagnostic.Diagnostics.Any(d => d.Id == expected.Id),
				$"expected to contain diagnostic with Id {expected.Id}"
			);

	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(
		this GeneratorDriverRunResult result,
		DiagnosticDescriptor expected
	) =>
		expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.Diagnostics.Any(d => d.Id == expected.Id),
				$"expected not to contain diagnostic with Id {expected.Id}"
			);

	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoErrorDiagnostics(this GeneratorDriverRunResult result) =>
		AssertionResult.FailIf(
			result.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error),
			"expected no error diagnostics to be reported by the generator:\n"
				+ string.Join(
					'\n',
					result
						.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)
						.Select(d => $"  - {d.Id}: {d.Descriptor.Title}")
				)
		);
}
