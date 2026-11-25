using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace Padel.Api.Http;

/// <summary>
/// Lets requests through when Redis cannot be reached: a broken rate-limit store should not take the API down.
/// The requests are then not limited until Redis is back.
/// </summary>
internal sealed partial class FailOpenRateLimiter(RateLimiter inner, ILogger<FailOpenRateLimiter> logger) : RateLimiter
{
    public override TimeSpan? IdleDuration => inner.IdleDuration;

    public override RateLimiterStatistics? GetStatistics() => inner.GetStatistics();

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        try
        {
            return inner.AttemptAcquire(permitCount);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            LogRedisUnavailable(exception);
            return AllowedLease.Instance;
        }
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        try
        {
            return await inner.AcquireAsync(permitCount, cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            LogRedisUnavailable(exception);
            return AllowedLease.Instance;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override ValueTask DisposeAsyncCore() => inner.DisposeAsync();

    private static bool IsRedisUnavailable(Exception exception) => exception is RedisException or RedisTimeoutException;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis is unavailable; the request is not rate limited")]
    private partial void LogRedisUnavailable(Exception exception);

    private sealed class AllowedLease : RateLimitLease
    {
        public static readonly AllowedLease Instance = new();

        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
