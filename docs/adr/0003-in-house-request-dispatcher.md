# 3. In-house request dispatcher instead of MediatR

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Endpoints send commands and queries to handlers, with cross-cutting pipeline behaviors (validation, telemetry). MediatR is the usual library for this, but it has had a commercial licence since version 13 (2025). We only need a small part of it: one handler per request, behaviors and domain event handlers.

## Decision

The application has its own small dispatcher (`Common/Messaging`):

- `IRequest<TValue>`, `IRequestHandler<TRequest, TValue>` and `IRequestDispatcher.Send`.
- `IPipelineBehavior<TRequest, TValue>`, run in registration order, outermost first: `TelemetryBehavior`, then `ValidationBehavior` (FluentValidation).
- `IDomainEventHandler<TEvent>` and `IDomainEventPublisher` for domain events (see [ADR 5](0005-transactional-outbox-for-domain-events.md)).
- Handlers are registered by scanning the Application assembly at start-up.

Handlers return `Result<T>` (see [ADR 4](0004-expected-failures-as-result-unions.md)), so behaviors can short-circuit with an error without throwing.

## Consequences

- No licence or upgrade dependency, and about 100 lines of code that we fully understand and test (`RequestDispatcherTests`).
- No notifications or streaming, which we do not need.
- Handlers are found with reflection, which does not work with Native AOT. That is fine; Native AOT is not a goal.
