namespace Padel.Domain.Common;

/// <summary>A resource does not exist (HTTP 404).</summary>
public sealed record NotFoundError(string Code, string Description) : IError;
