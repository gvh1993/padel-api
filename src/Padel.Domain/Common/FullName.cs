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
        var firstName = Normalize(first, "First name", "FullName.FirstName");
        if (firstName is not string validFirstName)
        {
            return firstName.Error;
        }

        var lastName = Normalize(last, "Last name", "FullName.LastName");
        if (lastName is not string validLastName)
        {
            return lastName.Error;
        }

        return new FullName(validFirstName, validLastName);
    }

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
