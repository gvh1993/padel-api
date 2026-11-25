# Architecture Decision Records

Why the code looks the way it does. [ADR 1](0001-record-architecture-decisions.md) explains when to write one; start from [template.md](template.md).

| # | Decision | Status |
| --- | --- | --- |
| 1 | [Record architecture decisions](0001-record-architecture-decisions.md) | Accepted |
| 2 | [Layered architecture with vertical slices](0002-layered-architecture-with-vertical-slices.md) | Accepted |
| 3 | [In-house request dispatcher instead of MediatR](0003-in-house-request-dispatcher.md) | Accepted |
| 4 | [Expected failures as result unions, not exceptions](0004-expected-failures-as-result-unions.md) | Accepted |
| 5 | [Transactional outbox for domain events](0005-transactional-outbox-for-domain-events.md) | Accepted |
| 6 | [Database constraints as the last line of defence](0006-database-constraints-as-last-line-of-defence.md) | Accepted |
| 7 | [Optimistic concurrency with row versions and ETags](0007-optimistic-concurrency-with-etags.md) | Accepted |
| 8 | [Idempotency keys for creating bookings](0008-idempotency-keys-for-creating-bookings.md) | Accepted |
| 9 | [Keycloak for authentication, members linked by verified email](0009-keycloak-authentication-and-member-linking.md) | Accepted |
| 10 | [Aspire for local development and a separate migration service](0010-aspire-for-local-development.md) | Accepted |
| 11 | [Cursor (keyset) paging for lists](0011-cursor-paging-for-lists.md) | Accepted |
| 12 | [API versioning in the URL](0012-api-versioning-in-the-url.md) | Accepted |
| 13 | [Shared rate limits in Redis, failing open](0013-shared-rate-limits-in-redis-fail-open.md) | Superseded by 16 |
| 14 | [Test strategy and tools](0014-test-strategy.md) | Accepted |
| 15 | [Log every use case: who, what went in, what came out](0015-log-every-use-case.md) | Accepted |
| 16 | [In-memory rate limits per instance](0016-in-memory-rate-limits-per-instance.md) | Accepted |
