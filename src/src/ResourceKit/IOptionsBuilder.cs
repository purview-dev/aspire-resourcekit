namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Collects options entries and selects the output format.
/// </summary>
public interface IOptionsBuilder
{
	/// <summary>
	/// Adds entries from assignment expressions for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="assignments">One or more property assignment actions.</param>
	/// <returns>The same builder.</returns>
	IOptionsBuilder Assign<TOptions>(params Action<TOptions>[] assignments);

	/// <summary>
	/// Adds entries from assignment expressions for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="assignments">One or more property assignment actions.</param>
	/// <returns>The same builder.</returns>
	IOptionsBuilder Assign<TOptions>(string sectionName, params Action<TOptions>[] assignments);

	/// <summary>
	/// Builds the collected entries as command-line arguments (default mode).
	/// </summary>
	/// <returns>The generated command-line arguments.</returns>
	string[] Build();

	/// <summary>
	/// Switches the builder to produce environment variables.
	/// </summary>
	/// <returns>A builder that outputs environment variables.</returns>
	IEnvironmentVariablesBuilder AsEnvironmentVariables();
}
