using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for registering a ResourceKit host application.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DistributedApplicationBuilderExtensions
{
	extension(IDistributedApplicationBuilder builder)
	{
		/// <summary>
		/// Creates, builds, and configures a ResourceKit host app, then registers it as a singleton.
		/// </summary>
		/// <typeparam name="THostKit">The host app type to register.</typeparam>
		/// <param name="onBuilt">An optional action to invoke after the host app is built (post <see cref="IHostKit.Build(IDistributedApplicationBuilder)"/>).</param>
		/// <param name="onConfigured">An optional action to invoke after the host app is configured (post <see cref="IHostKit.Configure"/>).</param>
		/// <returns>The same distributed application builder for chaining.</returns>
		public IDistributedApplicationBuilder AddAspireResourceKit<THostKit>(
			Action<THostKit>? onBuilt = null,
			Action<THostKit>? onConfigured = null
		)
			where THostKit : class, IHostKit, new()
		{
			ArgumentNullException.ThrowIfNull(builder);

			THostKit hostApp = new();

			hostApp.Build(builder);
			onBuilt?.Invoke(hostApp);

			hostApp.Configure();
			onConfigured?.Invoke(hostApp);

			builder.Services.AddSingleton(hostApp);

			return builder;
		}
	}
}
