# 7. Optimistic concurrency with row versions and ETags

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Two admins can edit the same court or member at the same time. Without a check, the last write silently overwrites the other one.

## Decision

- Every table uses PostgreSQL's `xmin` system column as its row version (`IsRowVersion`). No extra column is needed.
- Responses carry the version as an `ETag` header (`"42"`).
- Clients send it back in `If-Match`. When the resource has changed in the meantime, the API returns **412 Precondition Failed**. Without `If-Match`, the change goes through (last write wins), so simple clients keep working.
- A conflict that EF Core detects during the save (`DbUpdateConcurrencyException`) returns 409.

## Consequences

- Lost updates are prevented for clients that send `If-Match`, with standard HTTP semantics.
- `xmin` changes on every update of the row, including updates by background processes. Clients then get a 412 and must reload, which is the intended behaviour.
