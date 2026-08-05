using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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
	IOptionsBuilder ForSet<TOptions>(params Action<TOptions>[] assignments);

	/// <summary>
	/// Adds entries from assignment expressions for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="assignments">One or more property assignment actions.</param>
	/// <returns>The same builder.</returns>
	IOptionsBuilder ForSet<TOptions>(string sectionName, params Action<TOptions>[] assignments);

	/// <summary>
	/// Adds a single entry from an assignment expression for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="assignment">A property assignment expression.</param>
	/// <param name="assignmentExpression">Captured source text for <paramref name="assignment"/>.</param>
	/// <returns>The same builder.</returns>
	IOptionsBuilder ForSetOne<TOptions>(
		Action<TOptions> assignment,
		[CallerArgumentExpression(nameof(assignment))] string assignmentExpression = ""
	);

	/// <summary>
	/// Adds a single entry from an assignment expression for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="assignment">A property assignment expression.</param>
	/// <param name="assignmentExpression">Captured source text for <paramref name="assignment"/>.</param>
	/// <returns>The same builder.</returns>
	IOptionsBuilder ForSetOne<TOptions>(
		string sectionName,
		Action<TOptions> assignment,
		[CallerArgumentExpression(nameof(assignment))] string assignmentExpression = ""
	);

	/// <summary>
	/// Adds a single entry from a member selector expression for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="selector">A member selector expression.</param>
	/// <returns>The same builder.</returns>
	IOptionsBuilder ForOne<TOptions>(Expression<Func<TOptions, object?>> selector);

	/// <summary>
	/// Adds a single entry from a member selector expression for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="selector">A member selector expression.</param>
	/// <returns>The same builder.</returns>
	IOptionsBuilder ForOne<TOptions>(string sectionName, Expression<Func<TOptions, object?>> selector);

	/// <summary>
	/// Adds entries from multiple member selector expressions for the specified options type.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="selector">The first member selector expression.</param>
	/// <param name="selectors">Additional member selector expressions.</param>
	/// <returns>The same builder.</returns>
	[SuppressMessage(
		"Naming",
		"CA1716:Identifiers should not match keywords",
		Justification = "For is the established OptionsHelper entry point name; it is not a C# keyword and is used only in this context."
	)]
	IOptionsBuilder For<TOptions>(
		Expression<Func<TOptions, object?>> selector,
		params Expression<Func<TOptions, object?>>[] selectors
	);

	/// <summary>
	/// Adds entries from multiple member selector expressions for the specified options type with an explicit root section name.
	/// </summary>
	/// <typeparam name="TOptions">The root options type.</typeparam>
	/// <param name="sectionName">The root section name override.</param>
	/// <param name="selector">The first member selector expression.</param>
	/// <param name="selectors">Additional member selector expressions.</param>
	/// <returns>The same builder.</returns>
	[SuppressMessage(
		"Naming",
		"CA1716:Identifiers should not match keywords",
		Justification = "For is the established OptionsHelper entry point name; it is not a C# keyword and is used only in this context."
	)]
	IOptionsBuilder For<TOptions>(
		string sectionName,
		Expression<Func<TOptions, object?>> selector,
		params Expression<Func<TOptions, object?>>[] selectors
	);

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
