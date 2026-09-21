using System.Linq.Expressions;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Builds environment variables from a populated options object.
/// </summary>
/// <typeparam name="TOptions">The root options type.</typeparam>
public interface IOptionsEnvironmentBuilder<TOptions>
{
	/// <summary>
	/// Overrides one or more values using assignment expressions.
	/// </summary>
	/// <param name="assignments">One or more assignment actions.</param>
	/// <returns>The same builder.</returns>
	IOptionsEnvironmentBuilder<TOptions> Override(params Action<TOptions>[] assignments);

	/// <summary>
	/// Ignores one or more values using member selectors.
	/// </summary>
	/// <param name="selectors">The member selectors to ignore.</param>
	/// <returns>The same builder.</returns>
	IOptionsEnvironmentBuilder<TOptions> Ignore(params Expression<Func<TOptions, object?>>[] selectors);

	/// <summary>
	/// Builds the collected entries as environment variables.
	/// </summary>
	/// <returns>A dictionary of environment variable names to values.</returns>
	IReadOnlyDictionary<string, string> Build();
}
