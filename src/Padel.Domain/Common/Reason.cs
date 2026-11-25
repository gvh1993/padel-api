namespace Padel.Domain.Common;

/// <summary>
/// Normalizes optional free-text reasons (suspension, cancellation).
/// </summary>
public static class Reason
{
    public const int MaxLength = 500;

    public static string? Normalize(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Reason cannot be longer than {MaxLength} characters.");
        }

        return trimmed;
    }
}
