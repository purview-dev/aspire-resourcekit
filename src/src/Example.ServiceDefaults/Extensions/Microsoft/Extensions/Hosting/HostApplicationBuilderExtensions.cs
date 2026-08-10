using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class HostApplicationBuilderExtensions
{
	extension<TBuilder>(TBuilder builder)
		where TBuilder : IHostApplicationBuilder
	{
		public TBuilder AddServiceDefaults()
		{
			builder.ConfigureOpenTelemetry();

			builder.AddDefaultHealthChecks();

			builder.Services.AddServiceDiscovery();

			builder.Services.ConfigureHttpClientDefaults(http =>
			{
				// Turn on resilience by default
				http.AddStandardResilienceHandler();

				// Turn on service discovery by default
				http.AddServiceDiscovery();
			});

			// Uncomment the following to restrict the allowed schemes for service discovery.
			// builder.Services.Configure<ServiceDiscoveryOptions>(options =>
			// {
			//     options.AllowedSchemes = ["https"];
			// });

			return builder;
		}

		public TBuilder ConfigureOpenTelemetry()
		{
			builder.Logging.AddOpenTelemetry(logging =>
			{
				logging.IncludeFormattedMessage = true;
				logging.IncludeScopes = true;
			});

			builder
				.Services.AddOpenTelemetry()
				.WithMetrics(metrics =>
					metrics
						.AddAspNetCoreInstrumentation()
						.AddHttpClientInstrumentation()
						.AddRuntimeInstrumentation()
				)
				.WithTracing(tracing =>
					tracing
						.AddSource(builder.Environment.ApplicationName)
						.AddAspNetCoreInstrumentation(tracing =>
							// Exclude health check requests from tracing
							tracing.Filter = context =>
								!context.Request.Path.StartsWithSegments(
									Platform.EndpointsDefinitions.Health,
									StringComparison.OrdinalIgnoreCase
								)
								&& !context.Request.Path.StartsWithSegments(
									Platform.EndpointsDefinitions.Aliveness,
									StringComparison.OrdinalIgnoreCase
								)
						)
						// Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
						//.AddGrpcClientInstrumentation()
						.AddHttpClientInstrumentation()
				);

			builder.AddOpenTelemetryExporters();

			return builder;
		}

		TBuilder AddOpenTelemetryExporters()
		{
			var useOtlpExporter = !string.IsNullOrWhiteSpace(
				builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
			);

			if (useOtlpExporter)
			{
				builder.Services.AddOpenTelemetry().UseOtlpExporter();
			}

			// Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
			//if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
			//{
			//    builder.Services.AddOpenTelemetry()
			//       .UseAzureMonitor();
			//}

			return builder;
		}

		public TBuilder AddDefaultHealthChecks()
		{
			builder
				.Services.AddHealthChecks()
				// Add a default liveness check to ensure app is responsive
				.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

			return builder;
		}
	}
}
