using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Padel.Application.Common;

namespace Padel.Infrastructure.Telemetry;

internal static class TelemetryServiceCollection
{
    /// <summary>
    /// Adds the telemetry sources owned by this service: the application's use-case spans and the
    /// PostgreSQL (Npgsql) commands. Exporting is configured by the service defaults.
    /// </summary>
    public static IServiceCollection AddPadelTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(PadelTelemetry.ActivitySourceName)
                .AddNpgsql())
            .WithMetrics(metrics => metrics.AddMeter("Npgsql"));

        return services;
    }
}
