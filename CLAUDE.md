# CLAUDE.md

Padel club API: members, courts and bookings. .NET 11 / C# 15, minimal APIs, EF Core 11 on PostgreSQL, Keycloak, Aspire.
[README.md](README.md) describes the system; [docs/adr](docs/adr/README.md) explains why it is built this way. Read the relevant ADR before changing a design decision.

## Commands

The SDK pinned in `global.json` (11.0.100) may only be installed in `%LOCALAPPDATA%\Microsoft\dotnet`. If `dotnet --list-sdks` shows no 11.x, put that folder first on the PATH for the command: `export PATH="$LOCALAPPDATA/Microsoft/dotnet:$PATH"` (bash).

```bash
dotnet build PadelApi.slnx                                   # code style is part of the build
dotnet test --solution PadelApi.slnx                         # all tests; integration tests need Docker
dotnet test --project tests/Padel.UnitTests                  # fast: domain and application
dotnet test --project tests/Padel.IntegrationTests --treenode-filter "/*/*/BookingsApiTests/*"   # one class
dotnet ef migrations has-pending-model-changes --project src/Padel.Infrastructure   # must say: no changes
aspire run                                                   # or: dotnet run --project src/Padel.AppHost
```

Before you call a change done: build, run the unit tests, and run the integration tests when the change touches Infrastructure, the API or the database.

## Layout

- `src/Padel.Domain`: aggregates, value objects, domain events. No dependencies.
- `src/Padel.Application/Features/<Feature>/<UseCase>/`: command or query, handler, validator.
- `src/Padel.Infrastructure`: EF Core configuration, repositories, unit of work, outbox, migrations.
- `src/Padel.Api/Features/<Feature>/<UseCase>/`: request, endpoint, response (REPR).
- `tests/Padel.UnitTests`, `tests/Padel.IntegrationTests`, `tests/Padel.ArchitectureTests`.

The architecture tests enforce the layering and slice rules. When they fail, fix the code, not the test.

## Rules

- **Expected failures are results, not exceptions.** Return `Error` values (`Error.Conflict(...)`, `ApplicationErrors.NotFound<T>(id)`), and check them with `if (x is Error error) return error;`. Throw only for bugs. ([ADR 4](docs/adr/0004-expected-failures-as-result-unions.md))
- Every request has exactly one handler and one endpoint with the same name: `RenameCourtCommand`, `RenameCourtCommandHandler` and `RenameCourtEndpoint`. Handlers, validators and endpoints are `sealed`; endpoints are `internal`.
- API requests and responses never expose Application or Domain DTOs. Map them with a `From(...)` method on the response.
- Routes hang off the feature group (`routes.MapCourtsGroup()`), which is under `/api/v{version}`. Do not hard-code `/api/v1` in endpoints. ([ADR 12](docs/adr/0012-api-versioning-in-the-url.md))
- Business rules that a concurrent request could bypass also need a database constraint. ([ADR 6](docs/adr/0006-database-constraints-as-last-line-of-defence.md))
- Use `TimeProvider` for the current time, never `DateTime.Now` or `DateTimeOffset.UtcNow` in src.
- Lists use `ListQuery`, `SortMap` and cursor paging. Sort keys must not be null. ([ADR 11](docs/adr/0011-cursor-paging-for-lists.md))
- Packages are versioned centrally in `Directory.Packages.props`; `PackageReference` has no `Version`.
- `TreatWarningsAsErrors` is on: unused usings (IDE0005), missing accessibility modifiers and block-scoped namespaces fail the build.
- Code, comments and docs are in English. Comments explain why, not what.
- A significant design decision gets an ADR in the same change: `/adr` skill.

## Tests

- Names: `Action_GivenState_ShouldOutcome`, `Action_WhenInput_ShouldOutcome` or `Action_ShouldOutcome`.
- Assertions: AwesomeAssertions (`x.Should()...`); results with `ShouldSucceed()` and `ShouldFail().Is<NotFoundError>()`. Do not use TUnit's `Assert.That`.
- Test data: builders (`MemberBuilder.New().WithEmail(...)`, `CreateBookingRequestBuilder.New()...`). Set only what the test depends on.
- Test doubles ([ADR 14](docs/adr/0014-test-strategy.md)):
  - handler tests use `HandlerFixture` (in-memory repositories, `FakeUnitOfWork`, `FakeTimeProvider`, `FakeCurrentUser`);
  - NSubstitute only for interactions and failure injection (`Received(1)`, a unit of work whose save fails);
  - do not mock repositories.
- Integration tests share one API host and one set of containers. Every test creates its own data (unique emails and names), so tests can run in parallel and never depend on each other.

## Skills

- `/add-use-case`: a new command or query with endpoint and tests.
- `/add-migration`: change the database schema.
- `/adr`: record an architecture decision.
