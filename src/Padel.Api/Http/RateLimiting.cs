using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Padel.Api.Authentication;

namespace Padel.Api.Http;

/// <summary>Request limits per caller (section <c>RateLimiting</c>).</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests per minute per caller, for all of <c>/api</c>.</summary>
    public int PermitsPerMinute { get; set; } = 120;

    /// <summary>New bookings per minute per caller.</summary>
    public int BookingPermitsPerMinute { get; set; } = 10;
}

internal static class RateLimiting
{
    /// <summary>The stricter policy for creating bookings.</summary>
    public const string CreateBookingPolicy = "create-booking";

    /// <summary>
    /// Fixed one-minute windows per caller: the account (<c>sub</c>) when signed in, otherwise the IP address.
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
                    ? PerMinute($"api:{CallerOf(context)}", Options(context).PermitsPerMinute)
                    : RateLimitPartition.GetNoLimiter("unlimited"));

            limiter.AddPolicy(CreateBookingPolicy, context =>
                PerMinute($"bookings:{CallerOf(context)}", Options(context).BookingPermitsPerMinute));

            limiter.OnRejected = WriteProblemAsync;
        });

        return services;
    }

    private static RateLimitPartition<string> PerMinute(string key, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });

    private static RateLimitingOptions Options(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    private static string CallerOf(HttpContext context) =>
        context.User.FindFirst(PadelAuthorization.SubjectClaim)?.Value
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    private static async ValueTask WriteProblemAsync(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        var httpContext = rejected.HttpContext;
        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
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
}
