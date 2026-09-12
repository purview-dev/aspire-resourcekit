using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Locks in the contract that execution-only rules (SG0017/SG0018/SG0019) never block generation.
/// These report problems that prevent runtime execution, not generation, so they must stay non-Error
/// severity (which keeps <c>ShouldProcess</c>/<c>IsFatal</c> from halting generation) and remain
/// analyzer-owned so the analyzer reports them once.
/// </summary>
public sealed class ExecutionOnlyRuleSeverityTests
{
	[Test]
	public async Task ExecutionOnlyRules_AreNotErrorSeverity_SoGenerationIsNeverBlocked()
	{
		// Arrange
		var descriptors = GetExecutionOnlyDescriptors();

		// Act
		var errorSeverityRules = descriptors.Where(static d => d.DefaultSeverity == DiagnosticSeverity.Error).ToArray();

		// Assert
		await Assert.That(errorSeverityRules).IsEmpty();
	}

	[Test]
	public async Task ExecutionOnlyRules_AreRecognizedByIsExecutionOnly()
	{
		// Arrange
		var descriptors = GetExecutionOnlyDescriptors();

		// Act
		var notRecognized = descriptors.Where(static d => !ResourceKitRules.IsExecutionOnly(d)).ToArray();

		// Assert
		await Assert.That(notRecognized).IsEmpty();
	}

	[Test]
	public async Task ExecutionOnlyRules_AreAnalyzerOwned_SoAreReportedExactlyOnce()
	{
		// Arrange
		var descriptors = GetExecutionOnlyDescriptors();

		// Act
		var notAnalyzerOwned = descriptors.Where(static d => !ResourceKitRules.IsAnalyzerOwned(d)).ToArray();

		// Assert
		await Assert.That(notAnalyzerOwned).IsEmpty();
	}

	static IEnumerable<DiagnosticDescriptor> GetExecutionOnlyDescriptors()
	{
		var descriptorsById = typeof(DiagnosticLibrary)
			.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
			.Where(static field => field.FieldType == typeof(DiagnosticDescriptor))
			.Select(static field => (DiagnosticDescriptor)field.GetValue(null)!)
			.ToDictionary(static d => d.Id, StringComparer.Ordinal);

		return ResourceKitRules.ExecutionOnlyRuleIds.Select(id =>
			descriptorsById.TryGetValue(id, out var descriptor)
				? descriptor
				: throw new InvalidOperationException(
					$"Execution-only rule '{id}' has no matching descriptor in {nameof(DiagnosticLibrary)}."
				)
		);
	}
}
