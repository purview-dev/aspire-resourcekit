using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Infrastructure;

/// <summary>
/// A minimal <see cref="AnalyzerConfigOptionsProvider"/> used by the integration tests to feed
/// MSBuild/analyzer-config options (such as the source generator opt-out switch) into the
/// generator driver without spinning up a full MSBuild evaluation.
/// </summary>
sealed class TestAnalyzerConfigOptionsProvider(bool disableSourceGenerator) : AnalyzerConfigOptionsProvider
{
	readonly TestAnalyzerConfigOptions _globalOptions = new(disableSourceGenerator);

	public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;

	public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;
}

sealed class TestAnalyzerConfigOptions(bool? disableSourceGenerator) : AnalyzerConfigOptions
{
	public static readonly TestAnalyzerConfigOptions Empty = new(disableSourceGenerator: null);

	public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
	{
		if (
			key == SourceGenHelpers.DisablePurviewAspireResourceKitSourceGeneratorPropertyName
			&& disableSourceGenerator is bool disabled
		)
		{
			value = disabled ? "true" : "false";
			return true;
		}

		value = null;
		return false;
	}

	public override IEnumerable<string> Keys =>
		disableSourceGenerator is bool
			? [SourceGenHelpers.DisablePurviewAspireResourceKitSourceGeneratorPropertyName]
			: [];
}
