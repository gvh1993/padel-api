# 8. Idempotency keys for creating bookings

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Mobile clients retry requests after a timeout. If the first request did succeed, a retry of `POST /bookings` would create a second booking, or fail with a confusing "slot taken".

## Decision

- Clients may send an `Idempotency-Key` header with `POST /api/v1/bookings`.
- The key is stored on the booking, unique per member (a filtered unique index).
- A retry with the same key, for the same court and slot, returns the existing booking. The same key with a different court or slot returns a 409 (`Booking.IdempotencyKeyReused`).
- Concurrent retries with the same key are caught by the unique index.

## Consequences

- Safe retries for the operation where duplicates hurt most.
- Keys are kept as long as the booking. No separate store or expiry is needed.
- Other POST endpoints have no idempotency keys. They are admin actions: registering a member twice is prevented by the unique email, and a duplicate court is harmless and can be retired.
