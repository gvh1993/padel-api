using Padel.Domain.Common;

namespace Padel.Application.Common;

/// <summary>Errors that are not specific to one feature.</summary>
public static class ApplicationErrors
{
    public static readonly Error Forbidden = Error.Forbidden(
        "Access.Forbidden",
        "You are not allowed to access this resource.");

    public static readonly Error ConcurrentChange = Error.Conflict(
        "Concurrency.Conflict",
        "The resource was changed by another request. Reload it and try again.");

    public static Error NotFound<T>(Guid id) =>
        Error.NotFound($"{typeof(T).Name}.NotFound", $"{typeof(T).Name} '{id}' was not found.");
}
