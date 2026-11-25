using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Padel.Infrastructure.Persistence.Outbox;

/// <summary>
/// Degraded when domain events are not being handled: the oldest waiting message is older than
/// <see cref="OutboxOptions.StaleAfter"/>, or messages failed <see cref="OutboxOptions.MaxAttempts"/> times and
/// will not be retried. Not part of readiness: the API itself still works, only the follow-ups
/// (such as notifications) lag behind. It shows in <c>/health</c> and in monitoring.
/// </summary>
internal sealed class OutboxHealthCheck(PadelDbContext context, IOptions<OutboxOptions> options, TimeProvider timeProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthCheckContext, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var unprocessed = context.OutboxMessages.Where(message => message.ProcessedOn == null);

        var failed = await unprocessed.CountAsync(message => message.Attempts >= settings.MaxAttempts, cancellationToken);
        var oldestWaiting = await unprocessed
            .Where(message => message.Attempts < settings.MaxAttempts)
            .OrderBy(message => message.OccurredOn)
            .Select(message => (DateTimeOffset?)message.OccurredOn)
            .FirstOrDefaultAsync(cancellationToken);

        var waitingFor = oldestWaiting is { } occurredOn ? timeProvider.GetUtcNow() - occurredOn : TimeSpan.Zero;
        var data = new Dictionary<string, object>
        {
            ["failedMessages"] = failed,
            ["oldestWaitingSeconds"] = (int)waitingFor.TotalSeconds,
        };

        var problems = new List<string>();
        if (failed > 0)
        {
            problems.Add($"{failed} message(s) failed {settings.MaxAttempts} times and are no longer retried (see LastError)");
        }

        if (waitingFor > settings.StaleAfter)
        {
            problems.Add($"the oldest message has been waiting for {waitingFor:g}");
        }

        return problems.Count == 0
            ? HealthCheckResult.Healthy(data: data)
            : HealthCheckResult.Degraded(string.Join("; ", problems) + ".", data: data);
    }
}
