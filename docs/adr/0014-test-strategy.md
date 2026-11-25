# 14. Test strategy and tools

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

We want fast feedback on business rules, and confidence that the real stack (PostgreSQL constraints, Keycloak tokens, Redis, HTTP contracts) works. Two popular .NET test libraries changed their terms recently: FluentAssertions 8 is commercial, and Moq briefly shipped SponsorLink.

## Decision

- **Framework:** TUnit (engine only) on Microsoft.Testing.Platform. It is source-generated and runs tests in parallel by default.
- **Assertions:** AwesomeAssertions, the open-source fork of FluentAssertions. Results have their own helpers (`ShouldSucceed()`, `ShouldFail().Is<T>()`). TUnit's assertions are not referenced, so the two styles do not mix.
- **Test data:** builders on AutoFaker (`MemberBuilder.New().WithEmail(...)`). Tests set only the values they depend on.
- **Test doubles in unit tests:**
  - *State-based fakes* where behaviour matters: in-memory repositories run the same LINQ as EF Core (filters, sorting, cursor paging), and `FakeUnitOfWork` collects domain events. Mocks would only repeat the test's assumptions.
  - *Plain fakes* for data holders (`FakeCurrentUser`) and `FakeTimeProvider` for time.
  - *NSubstitute* for interactions and failure injection: checking that a notification was sent (`Received`), a pipeline `next` delegate, or a unit of work whose save fails. The NSubstitute analyzers catch misuse at build time.
- **Integration tests:** the real API (`WebApplicationFactory`) against PostgreSQL, Redis and Keycloak in Testcontainers, called through Refit clients. No in-memory database: it would not enforce the constraints of [ADR 6](0006-database-constraints-as-last-line-of-defence.md).
- **Architecture tests:** NetArchTest for the rules of [ADR 2](0002-layered-architecture-with-vertical-slices.md).
- Names follow `Action_GivenState_ShouldOutcome` or `Action_WhenInput_ShouldOutcome`.

## Consequences

- Unit tests run in about a second. The integration tests need Docker and take about half a minute.
- Before adding a mock, check whether an in-memory fake already covers the behaviour.
