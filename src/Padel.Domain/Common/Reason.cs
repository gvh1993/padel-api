namespace Padel.Domain.Common;

/// <summary>
/// An optional free-text reason (suspension, cancellation), trimmed; blank means no reason.
/// </summary>
public sealed record Reason
{
    public const int MaxLength = 500;

    public static readonly Reason None = new(text: null);

    private Reason(string? text)
    {
        Text = text;
    }

    /// <summary>The trimmed reason, or <c>null</c> when none was given.</summary>
    public string? Text { get; }

    public static Result<Reason> Create(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return None;
        }

        var trimmed = text.Trim();
        if (trimmed.Length > MaxLength)
        {
            return Error.Validation("Reason.TooLong", $"Reason cannot be longer than {MaxLength} characters.");
        }

        return new Reason(trimmed);
    }
}
