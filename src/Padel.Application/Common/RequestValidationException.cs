namespace Padel.Application.Common;

/// <summary>
/// The request is invalid (HTTP 400). Errors are grouped per property name.
/// </summary>
public class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
