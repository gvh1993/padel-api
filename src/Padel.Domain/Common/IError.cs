namespace Padel.Domain.Common;

/// <summary>What every kind of <see cref="Error"/> has: a stable code for clients and a readable description.</summary>
public interface IError
{
    string Code { get; }

    string Description { get; }
}
