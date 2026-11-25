# 10. Aspire for local development and a separate migration service

- **Status:** Accepted
- **Date:** 2026-11-25
- **Amended:** 2026-11-25. Redis removed ([ADR 16](0016-in-memory-rate-limits-per-instance.md)); the promised deployment description now exists in the README.

## Context

The API depends on PostgreSQL and Keycloak. Developers need these running with the right configuration, and need logs, traces and metrics to see what happens.

## Decision

- An Aspire AppHost (`Padel.AppHost`) starts PostgreSQL and Keycloak as containers, wires the connection strings, and shows logs, traces, metrics and health in the Aspire dashboard.
- `Padel.ServiceDefaults` configures OpenTelemetry, health endpoints, service discovery and HTTP resilience for every service.
- Migrations are applied by `Padel.MigrationService`, which runs and exits. The API never migrates on start-up, because several instances would race each other. Aspire starts the API once the migrations have completed. In production, the migration image runs as a separate step (for example a Kubernetes Job).

## Consequences

- One command (`aspire run`) starts the whole environment.
- The AppHost is for development only. Production runs the two container images that CI builds, `padel-migrations` and `padel-api`, without Aspire: run the migrations first, then roll out the API. The README's [Deployment](../../README.md#deployment) section lists the steps, settings and probes.
