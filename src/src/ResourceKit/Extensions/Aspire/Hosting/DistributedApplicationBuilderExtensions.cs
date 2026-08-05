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
		/// <returns>The same distributed application builder for chaining.</returns>
		public IDistributedApplicationBuilder AddAspireResourceKit<THostKit>()
			where THostKit : class, IHostKit, new()
		{
			ArgumentNullException.ThrowIfNull(builder);

			THostKit hostApp = new();

			hostApp.Build(builder);
			hostApp.Configure();

			builder.Services.AddSingleton(hostApp);

			return builder;
		}
	}
}
