using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Padel.Api.Authentication;
using RedisRateLimiting;
using StackExchange.Redis;

namespace Padel.Api.Http;

/// <summary>Request limits per caller (section <c>RateLimiting</c>).</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests per minute per caller, for all of <c>/api</c>.</summary>
    public int PermitsPerMinute { get; set; } = 120;

    /// <summary>New bookings per minute per caller.</summary>
    public int BookingPermitsPerMinute { get; set; } = 10;

    /// <summary>Prefix of the counters in Redis, so several apps or environments can share one Redis.</summary>
    public string KeyPrefix { get; set; } = "padel";
}

internal static class RateLimiting
{
    /// <summary>The stricter policy for creating bookings.</summary>
    public const string CreateBookingPolicy = "create-booking";

    /// <summary>Connection string name of the Redis that holds the counters (<c>ConnectionStrings__redis</c>).</summary>
    public const string RedisConnectionName = "redis";

    /// <summary>
    /// Fixed one-minute windows per caller: the account (<c>sub</c>) when signed in, otherwise the IP address.
    /// With Redis the counters are shared by all instances; without it each instance counts on its own.
    /// Health checks and the documentation are not limited. Rejected requests get 429 problem details
    /// with a <c>Retry-After</c> header.
    /// </summary>
    public static IServiceCollection AddPadelRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .Validate(options => options.PermitsPerMinute > 0 && options.BookingPermitsPerMinute > 0, "Rate limits must be positive.")
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Path.StartsWithSegments("/api")
                    ? PerMinute(context, "api", Options(context).PermitsPerMinute)
                    : RateLimitPartition.GetNoLimiter("unlimited"));

            limiter.AddPolicy(CreateBookingPolicy, context =>
                PerMinute(context, "bookings", Options(context).BookingPermitsPerMinute));

            limiter.OnRejected = WriteProblemAsync;
        });

        return services;
    }

    private static RateLimitPartition<string> PerMinute(HttpContext context, string limit, int permits)
    {
        var key = $"{Options(context).KeyPrefix}:{limit}:{CallerOf(context)}";

        if (context.RequestServices.GetService<IConnectionMultiplexer>() is not { } redis)
        {
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
        }

        var logger = context.RequestServices.GetRequiredService<ILogger<FailOpenRateLimiter>>();
        return RateLimitPartition.Get(key, partitionKey => new FailOpenRateLimiter(
            new RedisFixedWindowRateLimiter<string>(partitionKey, new RedisFixedWindowRateLimiterOptions
            {
                ConnectionMultiplexerFactory = () => redis,
                PermitLimit = permits,
                Window = TimeSpan.FromMinutes(1),
            }),
            logger));
    }

    private static RateLimitingOptions Options(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    private static string CallerOf(HttpContext context) =>
        context.User.FindFirst(PadelAuthorization.SubjectClaim)?.Value
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    private static async ValueTask WriteProblemAsync(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        var httpContext = rejected.HttpContext;
        if (RetryAfterSeconds(rejected.Lease) is { } seconds)
        {
            httpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests.",
                Detail = "Slow down and try again after the time in the Retry-After header.",
            },
        });
    }

    /// <summary>The in-memory limiter reports a <see cref="TimeSpan"/>; the Redis limiter whole seconds under its own name.</summary>
    private static int? RetryAfterSeconds(RateLimitLease lease) =>
        lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? (int)Math.Ceiling(retryAfter.TotalSeconds)
        : lease.TryGetMetadata(RateLimitMetadataName.RetryAfter, out var seconds) ? seconds
        : null;
}
