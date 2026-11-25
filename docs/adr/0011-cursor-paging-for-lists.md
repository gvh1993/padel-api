# 11. Cursor (keyset) paging for lists

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

The lists first used offset paging (`page`, `pageSize`, `totalCount`). That needs a `COUNT(*)` and an `OFFSET` per request, and both get slower as tables grow. Items that are added or removed between two requests also shift the pages, so clients skip or repeat items.

## Decision

- Lists use keyset paging: `cursor`, `pageSize` and `sort`. The response has `items` and `nextCursor`, which is `null` on the last page.
- The cursor is opaque base64url JSON. It holds the sort order and the sort keys and id of the last item. The next page is the items after that position: `(k1, k2, id) > (v1, v2, vid)`, built as an expression in `QueryableExtensions`, with the values as SQL parameters.
- The id is always the final tie-breaker, so every item has a unique position.
- A cursor only fits the sort it was made for. Any other cursor, or an invalid one, returns a 400.
- Enum sort fields sort by enum value through a `CASE` expression, because enums are stored as text, and text order would differ between the database and the in-memory test repositories.

## Consequences

- Constant cost per page and stable paging while data changes.
- No total count, no jumping to page N and no previous page. A UI that needs these would need an extra endpoint.
- This was a breaking change to the list contract, released together with API version 1 ([ADR 12](0012-api-versioning-in-the-url.md)).
