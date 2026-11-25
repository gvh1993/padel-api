namespace Padel.Domain.Common;

/// <summary>The input is invalid (HTTP 400).</summary>
public sealed record ValidationError(string Code, string Description) : IError
{
    private static readonly IReadOnlyDictionary<string, string[]> NoFieldErrors = new Dictionary<string, string[]>();

    /// <summary>Validation messages per input field, when the error comes from validating a request.</summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = NoFieldErrors;
}
