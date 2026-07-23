using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DistributedApplicationBuilderExtensions
{
	extension(IDistributedApplicationBuilder builder)
	{
		public IDistributedApplicationBuilder AddResourceKit<THostApp>()
			where THostApp : class, IHostApp, new()
		{
			ArgumentNullException.ThrowIfNull(builder);

			THostApp hostApp = new();
			hostApp.Build(builder);
			hostApp.Configure();

			builder.Services.AddSingleton(hostApp);

			return builder;
		}
	}
}
