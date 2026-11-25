namespace Padel.Domain.Common;

/// <summary>The successful outcome of an operation that returns no value.</summary>
public sealed record Success
{
    public static readonly Success Instance = new();

    private Success()
    {
    }
}
