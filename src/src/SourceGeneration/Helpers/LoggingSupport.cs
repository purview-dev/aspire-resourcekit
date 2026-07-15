using System.Collections.Concurrent;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;

interface ILogSupport
{
	void SetLogOutput(Action<string, OutputType> action);
}

sealed class GenerationLogger(Action<string, OutputType> logger)
{
	static readonly ConcurrentDictionary<int, string> SpacingCache = new();

	public void Info(string message) => logger(message, OutputType.Info);

	public void Info(string message, int spacing) => Info(GetSpacing(spacing, message));

	public void Debug(string message) => logger(message, OutputType.Debug);

	public void Debug(string message, int spacing) => Debug(GetSpacing(spacing, message));

	public void Diagnostic(string message) => logger(message, OutputType.Diagnostic);

	public void Diagnostic(string message, int spacing) => Diagnostic(GetSpacing(spacing, message));

	public void Warning(string message) => logger(message, OutputType.Warning);

	public void Warning(string message, int spacing) => Warning(GetSpacing(spacing, message));

	public void Error(string message) => logger(message, OutputType.Error);

	public void Error(string message, int spacing) => Error(GetSpacing(spacing, message));

	public void Error(Exception ex, string? message = null, int tabs = 0)
	{
		message ??= "The following exception occurred:";

		message += $"\n\nMessage: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

		logger(GetSpacing(tabs, message), OutputType.Error);
	}

	static string GetSpacing(int tabs, string message) =>
		(tabs <= 0 ? string.Empty : SpacingCache.GetOrAdd(tabs, t => new string(' ', t))) + message;
}

enum OutputType
{
	Diagnostic,
	Debug,
	Info,
	Warning,
	Error,
}
