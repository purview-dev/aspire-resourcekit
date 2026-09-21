using System.ComponentModel;

namespace Aspire.Hosting.ApplicationModel;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class IResourceBuilderExtensions
{
	extension<T>(IResourceBuilder<T> builder)
		where T : IResourceWithEnvironment
	{
		/// <summary>
		/// Adds an environment variable to the resource.
		/// </summary>
		/// <param name="optionsBuilder">The options builder.</param>
		/// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
		public IResourceBuilder<T> WithEnvironment(IOptionsBuilder optionsBuilder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(optionsBuilder);

			var items = optionsBuilder.AsEnvironmentVariables().Build().ToDictionary();
			return ApplyEnvironment(builder, items);
		}

		/// <summary>
		/// Adds environment variables for a populated options object.
		/// </summary>
		/// <typeparam name="TOptions">The options type.</typeparam>
		/// <param name="options">The options instance.</param>
		/// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
		public IResourceBuilder<T> WithEnvironment<TOptions>(TOptions options)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(options);

			var items = OptionsHelper.Environment(options).Build().ToDictionary();
			return ApplyEnvironment(builder, items);
		}

		/// <summary>
		/// Adds environment variables for a populated options object with overrides and ignores.
		/// </summary>
		/// <typeparam name="TOptions">The options type.</typeparam>
		/// <param name="options">The options instance.</param>
		/// <param name="configure">Optional builder customization.</param>
		/// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
		public IResourceBuilder<T> WithEnvironment<TOptions>(
			TOptions options,
			Action<IOptionsEnvironmentBuilder<TOptions>> configure
		)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(options);
			ArgumentNullException.ThrowIfNull(configure);

			var environmentBuilder = OptionsHelper.Environment(options);
			configure(environmentBuilder);
			var items = environmentBuilder.Build().ToDictionary();
			return ApplyEnvironment(builder, items);
		}

		/// <summary>
		/// Adds an environment variable to the resource.
		/// </summary>
		/// <param name="values">The environment variables to add.</param>
		public IResourceBuilder<T> WithEnvironment(IDictionary<string, string> values)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(values);

			return ApplyEnvironment(builder, values);
		}

		/// <summary>
		/// Adds environment variables to the resource, for example the dictionary returned by
		/// <see cref="IOptionsEnvironmentBuilder{TOptions}.Build"/>.
		/// </summary>
		/// <param name="values">The environment variables to add.</param>
		/// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
		public IResourceBuilder<T> WithEnvironment(IReadOnlyDictionary<string, string> values)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(values);

			return ApplyEnvironment(builder, values);
		}

		static IResourceBuilder<T> ApplyEnvironment(
			IResourceBuilder<T> resourceBuilder,
			IEnumerable<KeyValuePair<string, string>> values
		)
		{
			foreach (var (key, value) in values)
				resourceBuilder = resourceBuilder.WithEnvironment(key, value);

			return resourceBuilder;
		}
	}
}
