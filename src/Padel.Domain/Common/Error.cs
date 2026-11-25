namespace Padel.Domain.Common;

/// <summary>
/// Why an operation failed: exactly one of the error kinds. Switch on it to handle each kind;
/// the compiler checks that every kind is handled.
/// </summary>
public union Error(ValidationError, NotFoundError, ConflictError, ForbiddenError, BusinessRuleError)
{
    /// <summary>The stable code of the error, e.g. <c>Booking.SlotTaken</c>.</summary>
    public string Code => Details.Code;

    public string Description => Details.Description;

    private IError Details => Value as IError ?? throw new InvalidOperationException("The error has no value.");

    public static Error Validation(string code, string description) => new ValidationError(code, description);

    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new ValidationError("Validation", "One or more validation errors occurred.") { FieldErrors = fieldErrors };

    public static Error NotFound(string code, string description) => new NotFoundError(code, description);

    public static Error Conflict(string code, string description) => new ConflictError(code, description);

    public static Error Forbidden(string code, string description) => new ForbiddenError(code, description);

    public static Error BusinessRule(string code, string description) => new BusinessRuleError(code, description);
}
