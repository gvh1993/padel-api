namespace Padel.Application.Common;

/// <summary>
/// The request conflicts with the current state of a resource (HTTP 409).
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
