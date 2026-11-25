namespace Padel.Domain.Common;

public sealed record FullName
{
    public const int MaxLength = 64;

    // Also used by EF Core to materialize stored (already valid) values.
    private FullName(string first, string last)
    {
        First = first;
        Last = last;
    }

    public string First { get; }
    public string Last { get; }

    public string DisplayName => $"{First} {Last}";

    /// <summary>Validates and trims a first and last name.</summary>
    public static Result<FullName> Create(string? first, string? last)
    {
        var firstName = CreateFirstName(first);
        if (firstName is not string validFirstName)
        {
            return firstName.Error;
        }

        var lastName = CreateLastName(last);
        if (lastName is not string validLastName)
        {
            return lastName.Error;
        }

        return new FullName(validFirstName, validLastName);
    }

    /// <summary>Validates and trims a first name on its own (used to report the field when validating requests).</summary>
    public static Result<string> CreateFirstName(string? value) => Normalize(value, "First name", "FullName.FirstName");

    /// <summary>Validates and trims a last name on its own.</summary>
    public static Result<string> CreateLastName(string? value) => Normalize(value, "Last name", "FullName.LastName");

    private static Result<string> Normalize(string? value, string label, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation($"{code}.Required", $"{label} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            return Error.Validation($"{code}.TooLong", $"{label} cannot be longer than {MaxLength} characters.");
        }

        return trimmed;
    }

    public override string ToString() => DisplayName;
}
