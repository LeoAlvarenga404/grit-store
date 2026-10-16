using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Grit.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    private const string HealthPath = "/health";

    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("MassTransit", "Microsoft.EntityFrameworkCore"))
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName, "MassTransit")
                .AddAspNetCoreInstrumentation(aspNetCore =>
                    aspNetCore.Filter = context => !context.Request.Path.StartsWithSegments(HealthPath))
                .AddHttpClientInstrumentation());

        // Aspire sets this variable; without it (tests, plain dotnet run) nothing is exported.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        builder.Services.AddHealthChecks();
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // A minimal API endpoint, not MapHealthChecks, so /health is in the OpenAPI document the web types come from.
        app.MapGet(HealthPath, CheckHealth)
            .WithTags("Health")
            .WithSummary("Reports whether the API and its dependencies are reachable.")
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);
        return app;
    }

    private static async Task<Results<Ok<HealthResponse>, JsonHttpResult<HealthResponse>>> CheckHealth(
        HealthCheckService health,
        CancellationToken cancellationToken)
    {
        var report = await health.CheckHealthAsync(cancellationToken);
        var response = new HealthResponse(report.Status.ToString());
        if (report.Status == HealthStatus.Unhealthy)
        {
            return TypedResults.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return TypedResults.Ok(response);
    }
}
