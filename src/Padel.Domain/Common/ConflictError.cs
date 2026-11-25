namespace Padel.Domain.Common;

/// <summary>The request conflicts with the current state, e.g. a duplicate or a concurrent change (HTTP 409).</summary>
public sealed record ConflictError(string Code, string Description) : IError;
