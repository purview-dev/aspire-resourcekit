using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Guards the Roslyn analyzer release tracking files. Every diagnostic descriptor defined by
/// <see cref="DiagnosticLibrary"/> must be listed in exactly one of
/// <c>AnalyzerReleases.Shipped.md</c> or <c>AnalyzerReleases.Unshipped.md</c>, so a rule can never be
/// introduced without a release tracking entry.
/// </summary>
public sealed partial class AnalyzerReleaseTrackingTests
{
	const string ReleaseTrackingFolder = "AnalyzerReleases";

	[GeneratedRegex(@"^\s*(?<id>SG\d{4})\s*\|", RegexOptions.CultureInvariant)]
	private static partial Regex RuleIdPattern();

	[Test]
	public async Task ReleaseTrackingFiles_EveryDiagnosticDescriptor_IsTrackedExactlyOnce(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var descriptorIds = GetDescriptorIds();
		var shippedIds = await ReadRuleIdsAsync("AnalyzerReleases.Shipped.md", cancellationToken);
		var unshippedIds = await ReadRuleIdsAsync("AnalyzerReleases.Unshipped.md", cancellationToken);

		// Act
		var untracked = descriptorIds.Where(id => !shippedIds.Contains(id) && !unshippedIds.Contains(id)).ToArray();
		var trackedInBoth = shippedIds.Intersect(unshippedIds, StringComparer.Ordinal).ToArray();

		// Assert
		await Assert.That(untracked).IsEmpty();
		await Assert.That(trackedInBoth).IsEmpty();
	}

	[Test]
	public async Task ReleaseTrackingFiles_SG0017ToSG0020_AreShipped(CancellationToken cancellationToken)
	{
		// Arrange
		var shippedIds = await ReadRuleIdsAsync("AnalyzerReleases.Shipped.md", cancellationToken);

		// Act
		string[] expected = ["SG0017", "SG0018", "SG0019", "SG0020"];
		var missing = expected.Where(id => !shippedIds.Contains(id)).ToArray();

		// Assert
		await Assert.That(missing).IsEmpty();
	}

	static HashSet<string> GetDescriptorIds() =>
		[
			.. typeof(DiagnosticLibrary)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(static field => field.FieldType == typeof(DiagnosticDescriptor))
				.Select(static field => ((DiagnosticDescriptor)field.GetValue(null)!).Id),
		];

	static async Task<HashSet<string>> ReadRuleIdsAsync(string fileName, CancellationToken cancellationToken)
	{
		var path = Path.Combine(AppContext.BaseDirectory, ReleaseTrackingFolder, fileName);
		var lines = await File.ReadAllLinesAsync(path, cancellationToken);

		return
		[
			.. lines
				.Select(static line => RuleIdPattern().Match(line))
				.Where(static match => match.Success)
				.Select(static match => match.Groups["id"].Value),
		];
	}
}
