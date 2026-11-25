# 13. Shared rate limits in Redis, failing open

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Requests are limited per caller per minute (120 in general, 10 new bookings). ASP.NET Core's rate limiter keeps its counters in memory, so with N instances a caller effectively gets N times the limit.

## Decision

- When `ConnectionStrings:redis` is set, the fixed-window counters are kept in Redis (library `RedisRateLimiting`), shared by all instances. Without it, the in-memory limiter is used, which is fine for a single instance.
- The partition key is `{KeyPrefix}:{limit}:{caller}`. The caller is the account (`sub`), or the IP address when not signed in. `RateLimiting:KeyPrefix` separates apps or environments that share a Redis.
- **Fail open:** when Redis cannot be reached, `FailOpenRateLimiter` lets the request through and logs a warning. Redis is therefore not part of the readiness check.
- Behind a proxy, the client IP comes from `X-Forwarded-For`, trusted only from configured proxies (`ForwardedHeaders:KnownProxies` and `KnownNetworks`).

## Consequences

- Limits hold across instances, and a Redis outage does not take the API down. During such an outage, requests are not limited.
- `RedisRateLimiting` is a community library. It reports `Retry-After` under its own metadata name, which `RateLimiting` reads as well.
- Fail-closed (rejecting requests when Redis is down) was rejected: rate limiting protects the API, so it should not be what takes it down.
