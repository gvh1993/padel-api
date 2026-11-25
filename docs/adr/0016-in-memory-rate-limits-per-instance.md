# 16. In-memory rate limits per instance

- **Status:** Accepted
- **Date:** 2026-11-25
- **Supersedes:** [ADR 13](0013-shared-rate-limits-in-redis-fail-open.md)

## Context

[ADR 13](0013-shared-rate-limits-in-redis-fail-open.md) kept the rate-limit counters in Redis, so the limits would hold across instances. That added a container to the development environment and the tests, a community library (`RedisRateLimiting`) and a fail-open wrapper. All of that served one problem: with N instances, a caller gets N times the limit.

This project is an example of the patterns that matter for a club API. A single club does not need several instances, and most readers do not have the problem Redis solved. The extra infrastructure made the example harder to follow than the problem justified.

## Decision

- Rate limiting uses ASP.NET Core's built-in fixed-window limiter, with the counters in memory, per instance.
- The limits stay the same: `RateLimiting:PermitsPerMinute` (120) for all of `/api`, and `RateLimiting:BookingPermitsPerMinute` (10) for new bookings. A caller is identified by the account (`sub`), or by the IP address when not signed in, as before.
- The forwarded headers from ADR 13 stay (`ForwardedHeaders:KnownProxies` and `KnownNetworks`). They also give the correct client address and scheme to logs and links.
- Redis, `RedisRateLimiting`, `FailOpenRateLimiter` and the Redis containers in the AppHost and the tests are removed.

## Consequences

- One container less locally and in the tests, and no third-party rate-limit library.
- With several instances, each one counts on its own, so the effective limit is the number of instances times the configured limit. Lower the limits accordingly, or revisit ADR 13 when the API really runs on several instances.
