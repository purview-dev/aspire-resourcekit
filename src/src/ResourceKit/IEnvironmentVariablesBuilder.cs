namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Builds environment variables from collected options entries.
/// </summary>
public interface IEnvironmentVariablesBuilder
{
	/// <summary>
	/// Builds the collected entries as environment variables.
	/// </summary>
	/// <returns>A dictionary of environment variable names to values.</returns>
	IReadOnlyDictionary<string, string> Build();
}
