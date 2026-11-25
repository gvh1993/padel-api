namespace Padel.Domain.Common;

/// <summary>The resource changed since the client read it (HTTP 412).</summary>
public sealed record PreconditionFailedError(string Code, string Description) : IError;
