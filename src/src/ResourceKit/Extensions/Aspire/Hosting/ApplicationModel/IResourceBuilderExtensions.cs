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
			return WithEnvironment(builder, items);
		}

		/// <summary>
		/// Adds an environment variable to the resource.
		/// </summary>
		/// <param name="values">The environment variables to add.</param>
		public IResourceBuilder<T> WithEnvironment(IDictionary<string, string> values)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(values);

			foreach (var (key, value) in values)
				builder = builder.WithEnvironment(key, value);

			return builder;
		}
	}
}
