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
	public static AssertionResult HasDiagnostic(this DriverRunResult diagnostic, DiagnosticDescriptor expected) =>
		expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				!diagnostic.Result.Diagnostics.Any(d => d.Id == expected.Id),
				$"expected to contain diagnostic with Id {expected.Id}\n\n"
					+ diagnostic
						.Result.GeneratedTrees.Select(t => $"  - {t.FilePath}")
						.Concat(diagnostic.Result.Diagnostics.Select(d => $"  - {d.Id}: {d.Descriptor.Title}"))
						.DefaultIfEmpty("  - (none)")
						.Aggregate((a, b) => $"{a}\n{b}")
			);

	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(this DriverRunResult result, DiagnosticDescriptor expected) =>
		expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.Result.Diagnostics.Any(d => d.Id == expected.Id),
				$"expected not to contain diagnostic with Id {expected.Id}\n\n"
					+ result
						.Result.GeneratedTrees.Select(t => $"  - {t.FilePath}")
						.Concat(result.Result.Diagnostics.Select(d => $"  - {d.Id}: {d.Descriptor.Title}"))
						.DefaultIfEmpty("  - (none)")
						.Aggregate((a, b) => $"{a}\n{b}")
			);

	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoErrorDiagnostics(this DriverRunResult result) =>
		AssertionResult.FailIf(
			result.Result.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error),
			"expected no error diagnostics to be reported by the generator:\n"
				+ string.Join(
					'\n',
					result
						.Result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)
						.Select(d => $"  - {d.Id}: {d.Descriptor.Title}")
				)
				+ "\n\n"
				+ result
					.Result.GeneratedTrees.Select(t => $"  - {t.FilePath}")
					.Concat(result.Result.Diagnostics.Select(d => $"  - {d.Id}: {d.Descriptor.Title}"))
					.DefaultIfEmpty("  - (none)")
					.Aggregate((a, b) => $"{a}\n{b}")
		);
}
