using System.Diagnostics;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;

namespace Padel.Application.Common.Behaviors;

/// <summary>
/// Wraps every request in an OpenTelemetry span, tagged with the user and the outcome.
/// <see cref="LoggingBehavior{TRequest, TValue}"/> logs the request itself, inside this span.
/// </summary>
public sealed class TelemetryBehavior<TRequest, TValue>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TValue>
    where TRequest : notnull
    where TValue : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<Result<TValue>> Handle(TRequest request, RequestHandlerDelegate<TValue> next, CancellationToken cancellationToken)
    {
        using var activity = PadelTelemetry.ActivitySource.StartActivity(RequestName);
        activity?.SetTag("padel.request.type", typeof(TRequest).FullName);
        activity?.SetTag("user.id", currentUser.UserId);

        try
        {
            var result = await next();

            if (result is Error error)
            {
                // An expected outcome (invalid input, business rule, not found), not a fault of the service.
                activity?.SetTag("padel.error.code", error.Code);
                activity?.SetTag("padel.error.kind", error.Value?.GetType().Name);
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
