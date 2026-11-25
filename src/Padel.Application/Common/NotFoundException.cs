namespace Padel.Application.Common;

/// <summary>
/// The requested resource does not exist (HTTP 404).
/// </summary>
public class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException For<T>(Guid id) => new($"{typeof(T).Name} '{id}' was not found.");
}
