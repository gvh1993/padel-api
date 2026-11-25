using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Padel.Infrastructure.Persistence;

/// <summary>
/// Readiness: the database has every migration this version of the code expects. The migration service
/// applies them before a rollout; until it has, this instance gets no traffic instead of failing on
/// missing tables or columns. Once up to date it stays so (migrations are never rolled back while the
/// process runs), so later probes skip the query.
/// </summary>
internal sealed class DatabaseMigrationsHealthCheck(PadelDbContext context, DatabaseMigrationsHealthCheck.Status status) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthCheckContext, CancellationToken cancellationToken = default)
    {
        if (status.UpToDate)
        {
            return HealthCheckResult.Healthy();
        }

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            return HealthCheckResult.Unhealthy($"{pending.Count} migration(s) not applied yet: {string.Join(", ", pending)}.");
        }

        status.UpToDate = true;
        return HealthCheckResult.Healthy();
    }

    /// <summary>Remembers, per process, that the database was up to date.</summary>
    internal sealed class Status
    {
        public volatile bool UpToDate;
    }
}
