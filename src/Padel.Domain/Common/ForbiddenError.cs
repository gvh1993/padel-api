namespace Padel.Domain.Common;

/// <summary>The caller may not do this (HTTP 403).</summary>
public sealed record ForbiddenError(string Code, string Description) : IError;
