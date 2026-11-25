using Padel.Domain.Common;

namespace Padel.Application.Common;

public static class VersionExtensions
{
    /// <summary>
    /// Optimistic concurrency across requests: when the client says which version it changes
    /// (HTTP <c>If-Match</c>), the entity must still have that version.
    /// </summary>
    public static Result MatchesVersion<T>(this T entity, uint? expectedVersion) where T : Entity =>
        expectedVersion is { } expected && entity.Version != expected
            ? ApplicationErrors.Stale<T>(entity.Id)
            : Success.Instance;
}
