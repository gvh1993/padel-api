using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Cross-cutting service defaults shared by every service in the Aspire app:
/// OpenTelemetry (logs, traces, metrics), health checks, service discovery and HTTP resilience.
/// Service-specific telemetry (for example database instrumentation) is added by the layer that owns it.
/// </summary>
public static class Extensions
{
    public const string HealthEndpointPath = "/health";
    public const string LivenessEndpointPath = "/health/live";
    public const string ReadinessEndpointPath = "/health/ready";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options =>
                    // Health probes and the documentation UI are noise in traces.
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                        && !context.Request.Path.StartsWithSegments("/scalar"))
                .AddHttpClientInstrumentation());

        // Aspire sets OTEL_EXPORTER_OTLP_ENDPOINT to its dashboard; in production point it at your collector.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [HealthCheckTags.Live]);

        return builder;
    }

    /// <summary>
    /// Maps the health endpoints (anonymous, so orchestrators can probe them):
    /// <list type="bullet">
    /// <item><c>/health/live</c>: liveness, only checks tagged <see cref="HealthCheckTags.Live"/>.</item>
    /// <item><c>/health/ready</c>: readiness, only checks tagged <see cref="HealthCheckTags.Ready"/>.</item>
    /// <item><c>/health</c>: every check, with details per check.</item>
    /// </list>
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(LivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Live),
        }).AllowAnonymous();

        app.MapHealthChecks(ReadinessEndpointPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
        }).AllowAnonymous();

        // Details reveal infrastructure, so only in Development.
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions { ResponseWriter = WriteDetailsAsync })
                .AllowAnonymous();
        }

        return app;
    }

    private static Task WriteDetailsAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var body = new Dictionary<string, object>
        {
            ["status"] = report.Status.ToString(),
            ["totalDuration"] = report.TotalDuration.ToString(),
            ["checks"] = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new Dictionary<string, object?>
                {
                    ["status"] = entry.Value.Status.ToString(),
                    ["description"] = entry.Value.Description ?? entry.Value.Exception?.Message,
                    ["duration"] = entry.Value.Duration.ToString(),
                    ["tags"] = entry.Value.Tags,
                }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
