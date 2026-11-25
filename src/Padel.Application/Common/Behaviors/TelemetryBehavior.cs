using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Padel.Application.Common;
using Padel.Application.Common.Messaging;

namespace Padel.Application.Common.Behaviors;

/// <summary>
/// Wraps every request in an OpenTelemetry span and logs its duration.
/// </summary>
public sealed class TelemetryBehavior<TRequest, TResponse>(ILogger<TelemetryBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = PadelTelemetry.ActivitySource.StartActivity(RequestName);
        activity?.SetTag("padel.request.type", typeof(TRequest).FullName);

        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await next();

            logger.LogDebug(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                RequestName,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            activity?.AddException(exception);
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw;
        }
    }
}
