using System.ComponentModel;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class WebApplicationExtensions
{
	extension(WebApplication app)
	{
		public WebApplication MapDefaultEndpoints()
		{
			// Adding health checks endpoints to applications in non-development environments has security implications.
			// See https://aka.ms/aspire/healthchecks for details before enabling these endpoints in non-development environments.
			if (app.Environment.IsDevelopment())
			{
				// All health checks must pass for app to be considered ready to accept traffic after starting
				app.MapHealthChecks(Platform.EndpointsDefinitions.Health);

				// Only health checks tagged with the "live" tag must pass for app to be considered alive
				app.MapHealthChecks(
					Platform.EndpointsDefinitions.Aliveness,
					new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }
				);
			}

			return app;
		}
	}
}
