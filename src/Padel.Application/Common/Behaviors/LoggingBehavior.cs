using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Padel.Application.Common.Logging;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;

namespace Padel.Application.Common.Behaviors;

/// <summary>
/// Logs every command and query: what went in, who sent it, and what came out (the result, or the error).
/// The entries include personal data as is (names, email addresses); their own category,
/// <see cref="UseCaseLog.Category"/>, lets them be filtered, routed or kept as an audit trail.
/// </summary>
public sealed partial class LoggingBehavior<TRequest, TValue>(ILoggerFactory loggerFactory, ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TValue>
    where TRequest : notnull
    where TValue : notnull
{
    private static readonly string UseCase = typeof(TRequest).Name;

    private readonly ILogger _logger = loggerFactory.CreateLogger(UseCaseLog.Category);

    public async Task<Result<TValue>> Handle(TRequest request, RequestHandlerDelegate<TValue> next, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? "anonymous";
        var memberId = currentUser.MemberId;
        var isAdmin = currentUser.IsAdmin;
        var requestJson = UseCaseLogSerializer.Serialize(request);

        // Everything logged while the use case runs (EF Core, the handler) carries the use case and the user.
        using var scope = _logger.BeginScope(new Dictionary<string, object?> { ["UseCase"] = UseCase, ["UserId"] = userId });
        var started = Stopwatch.GetTimestamp();

        try
        {
            var result = await next();
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            if (result is TValue value)
            {
                UseCaseLog.Succeeded(_logger, UseCase, userId, memberId, isAdmin, elapsed, requestJson, UseCaseLogSerializer.Serialize(value));
            }
            else
            {
                var error = result.Error;
                UseCaseLog.Failed(_logger, UseCase, error.Code, userId, memberId, isAdmin, elapsed, requestJson, error.Description);
            }

            return result;
        }
        catch (Exception exception)
        {
            UseCaseLog.Threw(_logger, exception, UseCase, userId, memberId, isAdmin, Stopwatch.GetElapsedTime(started).TotalMilliseconds, requestJson);
            throw;
        }
    }
}

/// <summary>The entries of <see cref="LoggingBehavior{TRequest, TValue}"/>.</summary>
public static partial class UseCaseLog
{
    public const string Category = "Padel.UseCases";

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "{UseCase} succeeded for user {UserId} (member {MemberId}, admin {IsAdmin}) in {ElapsedMilliseconds} ms. Request: {Request}. Result: {Result}")]
    public static partial void Succeeded(
        ILogger logger, string useCase, string userId, Guid? memberId, bool isAdmin, double elapsedMilliseconds, string request, string result);

    /// <summary>An expected failure (invalid input, a business rule, not found): not a fault of the service.</summary>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "{UseCase} failed with {ErrorCode} for user {UserId} (member {MemberId}, admin {IsAdmin}) in {ElapsedMilliseconds} ms. Request: {Request}. Error: {ErrorDescription}")]
    public static partial void Failed(
        ILogger logger, string useCase, string errorCode, string userId, Guid? memberId, bool isAdmin, double elapsedMilliseconds, string request, string errorDescription);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "{UseCase} threw an exception for user {UserId} (member {MemberId}, admin {IsAdmin}) after {ElapsedMilliseconds} ms. Request: {Request}")]
    public static partial void Threw(
        ILogger logger, Exception exception, string useCase, string userId, Guid? memberId, bool isAdmin, double elapsedMilliseconds, string request);
}
