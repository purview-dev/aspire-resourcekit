using System.Globalization;
using Microsoft.CodeAnalysis;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;
using Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration;

partial class HostAppGenerator
{
	static void ReportDiagnostics(
		SourceProductionContext context,
		DiagnosticInfo diagnostic,
		GenerationContext generationContext
	) => ReportDiagnostics(context, [diagnostic], generationContext.Logger);

	static void ReportDiagnostics(
		SourceProductionContext context,
		IEnumerable<DiagnosticInfo> diagnostics,
		GenerationContext generationContext
	) => ReportDiagnostics(context, diagnostics, generationContext.Logger);

	static void ReportDiagnostics(
		SourceProductionContext context,
		DiagnosticInfo diagnostic,
		GenerationLogger? logger
	) => ReportDiagnostics(context, [diagnostic], logger);

	static void ReportDiagnostics(
		SourceProductionContext context,
		IEnumerable<DiagnosticInfo> diagnostics,
		GenerationLogger? logger
	)
	{
		foreach (var diagnosticInfo in diagnostics)
		{
			var diagnostic = diagnosticInfo.ToDiagnostic();
			context.ReportDiagnostic(diagnostic);

			logger?.Diagnostic(diagnostic.GetMessage(CultureInfo.InvariantCulture));
		}
	}

	void ILogSupport.SetLogOutput(Action<string, OutputType> action) => _logger = new GenerationLogger(action);
}
