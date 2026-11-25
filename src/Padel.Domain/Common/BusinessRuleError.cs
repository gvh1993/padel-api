namespace Padel.Domain.Common;

/// <summary>A business rule does not allow it in the current state (HTTP 422).</summary>
public sealed record BusinessRuleError(string Code, string Description) : IError;
