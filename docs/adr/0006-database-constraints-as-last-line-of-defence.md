# 6. Database constraints as the last line of defence

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Two requests can book the same court at the same time. A check in the handler ("is the slot free?") cannot prevent that race, and neither can locking in the application without serialising all bookings.

## Decision

- The handler checks first (overlap, email in use) so that the common case gets a clear error message.
- PostgreSQL enforces the rules that a race could slip past:
  - exclusion constraints (`btree_gist`) against overlapping bookings per court and per member, for bookings that are not cancelled;
  - unique indexes on the member's email, the account link and the idempotency key.
- `PadelUnitOfWork` maps unique and exclusion violations to the matching domain error (`DatabaseConstraints.ErrorFor`). The client gets the same 409 as from the handler's check.
- EF Core cannot model exclusion constraints, so the migrations create them with SQL.

## Consequences

- Correct under concurrency without application-level locks. The integration test `CreateBooking_WhenConcurrentRequestsForTheSameSlot_ShouldCreateExactlyOneBooking` proves it.
- We are tied to PostgreSQL. That is acceptable; we do not plan to support another database.
