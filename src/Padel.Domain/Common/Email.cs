namespace Padel.Domain.Common;

public sealed record Email
{
    public const int MaxLength = 254;
    public const int LocalPartMaxLength = 64;

    // Also used by EF Core to materialize stored (already valid) values.
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>Validates and normalizes (trims, lower-cases) an email address.</summary>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation("Email.Empty", "Email cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            return Error.Validation("Email.TooLong", $"Email cannot be longer than {MaxLength} characters.");
        }

        if (trimmed.Count(c => c == '@') is not 1)
        {
            return Error.Validation("Email.InvalidFormat", "Email must contain exactly one '@' character.");
        }

        var atIndex = trimmed.IndexOf('@');
        var localPart = trimmed[..atIndex];
        var domainPart = trimmed[(atIndex + 1)..];

        if (localPart.Length is 0 or > LocalPartMaxLength)
        {
            return Error.Validation("Email.InvalidLocalPart", "Email has an invalid local part.");
        }

        if (domainPart.Length < 3 || !domainPart.Contains('.') || domainPart.StartsWith('.') || domainPart.EndsWith('.'))
        {
            return Error.Validation("Email.InvalidDomain", "Email has an invalid domain part.");
        }

        return new Email(trimmed.ToLowerInvariant());
    }

    public override string ToString() => Value;
}
