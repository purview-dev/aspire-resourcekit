using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

partial class HostKitGenerator
{
	//static void ReportDiagnostics(
	//	SourceProductionContext context,
	//	DiagnosticInfo diagnostic,
	//	GenerationContext generationContext
	//) => ReportDiagnostics(context, [diagnostic], generationContext.Logger);

	//static void ReportDiagnostics(
	//	SourceProductionContext context,
	//	IEnumerable<DiagnosticInfo> diagnostics,
	//	GenerationContext generationContext
	//) => ReportDiagnostics(context, diagnostics, generationContext.Logger);

	//static void ReportDiagnostics(
	//	SourceProductionContext context,
	//	DiagnosticInfo diagnostic,
	//	GenerationLogger? logger
	//) => ReportDiagnostics(context, [diagnostic], logger);

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
}
