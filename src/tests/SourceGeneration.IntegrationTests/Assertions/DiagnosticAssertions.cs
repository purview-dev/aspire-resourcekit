using System.ComponentModel;
using Microsoft.CodeAnalysis;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Assertions;

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
}
