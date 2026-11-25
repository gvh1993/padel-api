using FluentValidation;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;

namespace Padel.Application.Common.Behaviors;

/// <summary>
/// Runs the request's FluentValidation validators. Invalid requests do not reach the handler;
/// they fail with a <see cref="ValidationError"/> that lists the messages per field.
/// </summary>
public sealed class ValidationBehavior<TRequest, TValue>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TValue>
    where TRequest : notnull
    where TValue : notnull
{
    public async Task<Result<TValue>> Handle(TRequest request, RequestHandlerDelegate<TValue> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
                .ToDictionary(group => group.Key, group => group.Distinct().ToArray());

            return Error.Validation(errors);
        }

        return await next();
    }
}
