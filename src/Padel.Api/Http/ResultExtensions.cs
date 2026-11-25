using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Padel.Domain.Common;

namespace Padel.Api.Http;

/// <summary>Turns application results into HTTP results: the success response, or RFC 9457 problem details.</summary>
internal static class ResultExtensions
{
    public static Results<TSuccess, ProblemHttpResult> ToHttpResult<TValue, TSuccess>(
        this Result<TValue> result,
        Func<TValue, TSuccess> onSuccess)
        where TValue : notnull
        where TSuccess : IResult =>
        result switch
        {
            TValue value => onSuccess(value),
            Error error => error.ToProblem(),
        };

    /// <summary>Each kind of error has its own status code; the compiler checks that every kind is mapped.</summary>
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var problem = error switch
        {
            ValidationError validation when validation.FieldErrors.Count > 0 =>
                Problem(new HttpValidationProblemDetails(ToCamelCaseKeys(validation.FieldErrors)), StatusCodes.Status400BadRequest, "One or more validation errors occurred.", detail: null),
            ValidationError validation => Problem(new ProblemDetails(), StatusCodes.Status400BadRequest, "One or more validation errors occurred.", validation.Description),
            NotFoundError notFound => Problem(new ProblemDetails(), StatusCodes.Status404NotFound, "The resource was not found.", notFound.Description),
            ConflictError conflict => Problem(new ProblemDetails(), StatusCodes.Status409Conflict, "The request conflicts with the current state.", conflict.Description),
            ForbiddenError forbidden => Problem(new ProblemDetails(), StatusCodes.Status403Forbidden, "Access denied.", forbidden.Description),
            BusinessRuleError businessRule => Problem(new ProblemDetails(), StatusCodes.Status422UnprocessableEntity, "A business rule was violated.", businessRule.Description),
        };

        problem.Extensions["errorCode"] = error.Code;
        return TypedResults.Problem(problem);
    }

    private static ProblemDetails Problem(ProblemDetails problem, int status, string title, string? detail)
    {
        problem.Status = status;
        problem.Title = title;
        problem.Detail = detail;
        return problem;
    }

    private static Dictionary<string, string[]> ToCamelCaseKeys(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(error => JsonNamingPolicy.CamelCase.ConvertName(error.Key), error => error.Value);
}
