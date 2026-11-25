using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;

namespace Padel.Application.Common.Behaviors;

/// <summary>
/// Wraps every request in an OpenTelemetry span and logs its duration and outcome.
/// </summary>
public sealed class TelemetryBehavior<TRequest, TValue>(ILogger<TelemetryBehavior<TRequest, TValue>> logger)
    : IPipelineBehavior<TRequest, TValue>
    where TRequest : notnull
    where TValue : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<Result<TValue>> Handle(TRequest request, RequestHandlerDelegate<TValue> next, CancellationToken cancellationToken)
    {
        using var activity = PadelTelemetry.ActivitySource.StartActivity(RequestName);
        activity?.SetTag("padel.request.type", typeof(TRequest).FullName);

        var started = Stopwatch.GetTimestamp();

        try
        {
            var result = await next();
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            if (result is Error error)
            {
                // An expected outcome (invalid input, business rule, not found), not a fault of the service.
                activity?.SetTag("padel.error.code", error.Code);
                activity?.SetTag("padel.error.kind", error.Value?.GetType().Name);
                logger.LogInformation(
                    "{RequestName} failed with {ErrorCode} in {ElapsedMilliseconds} ms",
                    RequestName,
                    error.Code,
                    elapsed);
            }
            else
            {
                logger.LogDebug("Handled {RequestName} in {ElapsedMilliseconds} ms", RequestName, elapsed);
            }

            return result;
        }
        catch (Exception exception)
        {
            activity?.AddException(exception);
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw;
        }
    }
}
