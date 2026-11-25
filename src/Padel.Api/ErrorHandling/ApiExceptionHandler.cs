using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Padel.Application.Common;
using Padel.Domain.Common;

namespace Padel.Api.ErrorHandling;

/// <summary>
/// Translates exceptions into RFC 9457 problem details.
/// </summary>
internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            RequestValidationException validation => new HttpValidationProblemDetails(ToCamelCaseKeys(validation.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            },
            BadHttpRequestException badRequest => Problem(badRequest.StatusCode, "The request is invalid.", badRequest.Message),
            DomainException => Problem(StatusCodes.Status422UnprocessableEntity, "A business rule was violated.", exception.Message),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "The resource was not found.", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "The request conflicts with the current state.", exception.Message),
            ForbiddenException => Problem(StatusCodes.Status403Forbidden, "Access denied.", exception.Message),
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

    private static Dictionary<string, string[]> ToCamelCaseKeys(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(error => JsonNamingPolicy.CamelCase.ConvertName(error.Key), error => error.Value);
}
