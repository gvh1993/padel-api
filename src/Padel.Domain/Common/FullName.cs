namespace Padel.Domain.Common;

public sealed record FullName
{
    public const int MaxLength = 64;

    public string First { get; }
    public string Last { get; }

    public FullName(string first, string last)
    {
        First = EnsureName(first, "First name");
        Last = EnsureName(last, "Last name");
    }

    public string DisplayName => $"{First} {Last}";

    private static string EnsureName(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{label} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"{label} cannot be longer than {MaxLength} characters.");
        }

        return trimmed;
    }

    public override string ToString() => DisplayName;
}
