namespace Padel.Domain.Common;

public sealed record Email
{
    public const int MaxLength = 254;
    public const int LocalPartMaxLength = 64;

    public string Value { get; }

    public Email(string value)
    {
        Value = Normalize(value);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Email cannot be longer than {MaxLength} characters.");
        }

        if (trimmed.Count(c => c == '@') is not 1)
        {
            throw new DomainException("Email must contain exactly one '@' character.");
        }

        var atIndex = trimmed.IndexOf('@');
        var localPart = trimmed[..atIndex];
        var domainPart = trimmed[(atIndex + 1)..];

        if (localPart.Length is 0 or > LocalPartMaxLength)
        {
            throw new DomainException("Email has an invalid local part.");
        }

        if (domainPart.Length < 3 || !domainPart.Contains('.') || domainPart.StartsWith('.') || domainPart.EndsWith('.'))
        {
            throw new DomainException("Email has an invalid domain part.");
        }

        return trimmed.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
