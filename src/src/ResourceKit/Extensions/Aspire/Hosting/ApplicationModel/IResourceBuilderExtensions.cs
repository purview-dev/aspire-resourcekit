using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

[EditorBrowsable(EditorBrowsableState.Never)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public static class IResourceBuilderExtensions
{
	static readonly Action<ILogger, string, string, string, Exception?> UrlNotApplied = LoggerMessage.Define<
		string,
		string,
		string
	>(
		LogLevel.Warning,
		new(1, nameof(UrlNotApplied)),
		"The URL '{Url}' for resource '{Resource}' was not applied: no endpoint URL matches '{Endpoint}'."
	);

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

	extension<T>(IResourceBuilder<T> builder)
		where T : IResource
	{
		/// <summary>
		/// Places a resource that is not a resource kit, such as an admin tool an integration adds beside a kit's
		/// resource, in a resource group's dashboard node.
		/// </summary>
		/// <typeparam name="THostKit">The host kit type.</typeparam>
		/// <param name="group">The group to show the resource in.</param>
		/// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
		/// <remarks>
		/// <para>
		/// Call it from <c>BuildResource</c> or <c>ConfigureResource</c>, for example
		/// <c>sql.WithDbGate(dbGate => dbGate.WithResourceGroup(HostKit.Groups.Persistence))</c>. The resource is
		/// nested when the host kit finishes <c>Configure</c>, following the same rules as a kit: a resource that
		/// already has a parent keeps it and the top of its chain is nested instead, and a resource placed in more
		/// than one group stays in the first. When the group is not shown, the resource goes to the nearest shown
		/// ancestor group; in publish mode, where there are no group nodes, nothing is placed.
		/// </para>
		/// <para>
		/// This only decides where the resource is shown. The group's <c>IsEnabled</c> and <c>Mode</c> gate resource
		/// kits, which decide whether to add the resource at all.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentException">Thrown when the resource is a group's own dashboard node.</exception>
		/// <exception cref="InvalidOperationException">Thrown after the host kit has placed its groups.</exception>
		public IResourceBuilder<T> WithResourceGroup<THostKit>(ResourceKitGroup<THostKit> group)
			where THostKit : class, IHostKit
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(group);

			group.Include(builder.Resource);
			return builder;
		}

		/// <summary>
		/// Adds named URLs to the resource, resolving relative URLs against its endpoints when the dashboard
		/// computes the resource's URLs.
		/// </summary>
		/// <param name="urls">The URLs to apply. Disabled URLs are skipped.</param>
		/// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
		/// <remarks>
		/// Unlike <c>WithUrl("/path")</c>, which replaces the path of every endpoint URL, a relative
		/// <see cref="ResourceKitUrl"/> adds one URL for one endpoint. See <see cref="ResourceKitUrl.Apply"/>.
		/// </remarks>
		public IResourceBuilder<T> WithResourceKitUrls(IEnumerable<ResourceKitUrl> urls)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(urls);

			ResourceKitUrl[] items = [.. urls.Where(static u => u.IsEnabled)];
			if (items.Length == 0)
				return builder;

			// The resource builder doesn't know the endpoint URLs yet, so we defer the application of the URLs until
			// the dashboard computes the resource's URLs.
			return builder.WithUrls(context =>
			{
				var unresolved = ResourceKitUrl.Apply(items, context.Urls);
				foreach (var url in unresolved)
				{
					UrlNotApplied(
						context.Logger,
						url.Url ?? "<endpoint>",
						context.Resource.Name,
						url.Endpoint ?? "<any>",
						null
					);
				}
			});
		}
	}
}
