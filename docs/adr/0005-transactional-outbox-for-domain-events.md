# 5. Transactional outbox for domain events

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Aggregates raise domain events (`BookingCreated`, `BookingCancelled`), and handlers react to them, for example by mailing the member. If handlers ran inside the request, a failing mail server would fail the booking. If they ran after the commit without being stored, a crash in between would lose the notification.

## Decision

- The unit of work stores the events of all changed aggregates in the `OutboxMessages` table, in the same transaction as the change.
- `OutboxProcessor`, a background service, reads batches with `FOR UPDATE SKIP LOCKED` and hands each event to its handlers. Several instances can run side by side.
- A failing handler is retried up to `Outbox:MaxAttempts` times. The last error stays in `LastError`.
- `OutboxCleaner` deletes handled messages after `Outbox:Retention` (7 days). Messages that failed every attempt are kept so someone can look at them.

## Consequences

- A booking never fails because of a notification, and no event is lost after a commit.
- Handlers run at least once, so they must be safe to repeat. Sending a mail twice in rare cases is acceptable.
- Handlers run a few seconds after the change (`Outbox:PollingInterval`), not immediately.
- We considered a message broker, but it adds infrastructure that a single club API does not need yet. The outbox can later feed a broker.
