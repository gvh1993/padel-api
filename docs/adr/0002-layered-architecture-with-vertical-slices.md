# 2. Layered architecture with vertical slices

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

The API has three features (members, courts, bookings) with rich business rules, and will grow. We want the rules to be testable without a database or HTTP, and a change to one use case should not ripple through the others.

## Decision

- Four layers with dependencies pointing inwards: `Domain` <- `Application` <- `Infrastructure` and `Api`.
  - `Domain` has the aggregates, value objects and domain events, without dependencies.
  - `Application` has the use cases and the ports (repositories, unit of work, current user).
  - `Infrastructure` implements the ports with EF Core and PostgreSQL.
  - `Api` is the HTTP adapter.
- Within the layers, code is organised by feature and use case (vertical slices): `Features/Members/RegisterMember/` holds the command, handler and validator, and the API has a matching folder with request, endpoint and response (REPR).
- API requests and responses are separate contracts. Endpoints map them to commands and DTOs, so an HTTP contract can change without touching the application.
- `Padel.ArchitectureTests` (NetArchTest) enforces the rules: layer dependencies, independent features, one handler and one endpoint per request, sealed handlers, and internal endpoints.

## Consequences

- Business rules are unit-tested in memory; the integration tests cover the adapters.
- Adding a use case means a few small files in fixed places, rather than editing shared services.
- There is some duplication (request, command, DTO and response often look alike). We accept that for independent contracts.
