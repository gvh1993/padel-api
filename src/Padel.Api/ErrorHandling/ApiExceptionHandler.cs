using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Padel.Api.ErrorHandling;

/// <summary>
/// Last line of defence: turns unexpected exceptions (and malformed requests) into RFC 9457 problem details.
/// Expected failures are not exceptions; endpoints return them as results (see <c>ResultExtensions</c>).
/// </summary>
internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            BadHttpRequestException badRequest => Problem(badRequest.StatusCode, "The request is invalid.", badRequest.Message),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                Problem(StatusCodes.Status499ClientClosedRequest, "The client closed the request.", null),
            _ => null,
        };

        if (problem is null)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            problem = Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string? detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
    };

}
