# Padel API

API for a padel club: members, courts and bookings.

**Stack:** .NET 11, ASP.NET Core minimal APIs, EF Core 11 with PostgreSQL, Redis (rate limits), Keycloak, FluentValidation, OpenTelemetry, Scalar, Aspire.
**Tests:** TUnit (engine only) with AwesomeAssertions, AutoBogus builders and NSubstitute. The integration tests use Testcontainers (PostgreSQL, Redis and Keycloak) and Refit; the architecture tests use NetArchTest.

## Requirements

- .NET SDK 11.0.100 or newer (pinned in [global.json](global.json))
- Docker
- A trusted development certificate: `dotnet dev-certs https --trust`. Aspire serves Keycloak over HTTPS with it.

## Run locally

```bash
dotnet run --project src/Padel.AppHost     # or: aspire run
```

The Aspire AppHost starts PostgreSQL, Redis and Keycloak (realm `padel`), runs the migration service and then starts the API. It opens the Aspire dashboard, which shows the resources, their health, logs, traces and metrics.

| What | Where |
| --- | --- |
| API documentation (Scalar) | <http://localhost:5062/scalar>, or the link in the dashboard |
| OpenAPI document | <http://localhost:5062/openapi/v1.json> |
| Keycloak admin console | <https://localhost:18080>. The admin password is in the dashboard, under the `keycloak` resource's parameters. |

In Scalar, choose **Authorize** and sign in with a realm user:

| User | Password | Access |
| --- | --- | --- |
| `admin` | `admin` | Realm role `admin`: manages members, courts and all bookings |
| `member` | `member` | Linked to the member with email `member@padel.example`, once an admin registers one |

Accounts are linked to members automatically. When an account signs in for the first time with the verified email address of a member that is not linked yet, the API links the two; from then on the account (Keycloak `sub`) identifies the member. For example, register a member with the email `member@padel.example`, then sign in as `member`.

The realm is defined in [src/Padel.AppHost/Realms/padel-realm.json](src/Padel.AppHost/Realms/padel-realm.json). Keycloak imports it on first start; after that, the data volume keeps the realm and its users. After you change the file, remove the `keycloak-data` volume to import it again. The integration tests use the same file.

The users and the password grant on `padel-scalar` are for development only.

## Architecture

The reasons behind the design are recorded as Architecture Decision Records in [docs/adr](docs/adr/README.md). [CLAUDE.md](CLAUDE.md) summarises the commands and coding rules for coding agents, and `.claude/skills` has step-by-step guides for adding a use case, a migration or an ADR.

| Project | Responsibility |
| --- | --- |
| `Padel.Domain` | Aggregates (`Member`, `Court`, `Booking`), value objects (`Email`, `FullName`, `TimeSlot`) and domain events. No dependencies. |
| `Padel.Application` | Use cases as vertical slices, plus the in-house dispatcher with pipeline behaviors (telemetry, validation) and cursor paging, filtering and sorting. |
| `Padel.Infrastructure` | EF Core and PostgreSQL, repositories and the unit of work. It owns the database's telemetry (Npgsql, the application's spans) and the database readiness check. |
| `Padel.Api` | Minimal API endpoints per feature, Keycloak JWT authentication, RFC 9457 problem details, API versioning (Asp.Versioning), and OpenAPI with Scalar. |
| `Padel.ServiceDefaults` | Cross-cutting service defaults: OpenTelemetry export, health endpoints, service discovery and HTTP resilience. |
| `Padel.MigrationService` | Applies the EF Core migrations, then exits. The API never migrates on start-up. |
| `Padel.AppHost` | The Aspire app model for local development. |

Code is organised by feature (vertical slices). Every use case has its own folder in each layer, and the API follows the REPR pattern: every endpoint has its own folder with its request, the endpoint and its response.

```text
src/Padel.Application/Features/Members/
    MemberDto.cs, MemberMappings.cs, MemberSorting.cs, IMemberRepository.cs
    RegisterMember/
        RegisterMemberCommand.cs            request + handler
        RegisterMemberCommandValidator.cs
    UpdateMemberProfile/ ...
src/Padel.Api/Features/Members/
    MembersGroup.cs                         the /api/v1/members route group
    RegisterMember/
        RegisterMemberRequest.cs            the body (or query string, for lists)
        RegisterMemberEndpoint.cs           maps the route; request → command → response
        RegisterMemberResponse.cs
    GetMember/
        GetMemberEndpoint.cs                no request type: the only input is the {id} in the route
        GetMemberResponse.cs
    ...
src/Padel.Infrastructure/Features/Members/MemberRepository.cs, MemberConfiguration.cs
```

Endpoints implement `IEndpoint` and are mapped automatically at start-up. Requests and responses are the API's own contracts. Endpoints translate them to and from the application's commands, queries and DTOs, so the HTTP contract of one endpoint can change without touching the others or the application.

`Padel.ArchitectureTests` enforces the layering and the slices:

- Dependencies point inwards.
- The members and courts features do not depend on other features.
- Every request has exactly one handler, and exactly one endpoint with the same name.
- API requests and responses never expose application types.
- Handlers, validators and endpoints are sealed; endpoints are internal.
- Entities have no public setters.

### Domain events: transactional outbox

Aggregates raise domain events, such as `BookingCreated` and `BookingCancelled`. The unit of work stores them in the `OutboxMessages` table in the same transaction as the change, so either both are saved or neither is.

The `OutboxProcessor` (a background service) then hands them to their handlers after the commit. Several instances can run at once because batches are locked with `FOR UPDATE SKIP LOCKED`. A failing handler is retried, up to `Outbox:MaxAttempts` times; the error is kept in `LastError`.

The `OutboxCleaner` deletes handled messages once they are older than `Outbox:Retention` (default 7 days), every `Outbox:CleanupInterval` (default 1 hour). Messages that failed `Outbox:MaxAttempts` times are kept, so someone can look at them.

The booking handlers send a confirmation and a cancellation notice to the member, with the time in the club's time zone. `LoggingNotificationSender` only writes them to the log. Replace it with a real email sender once the club has one.

### Errors: results as C# unions

Expected failures are not exceptions. Results are C# 15 union types:

```csharp
public union Result<T>(T, Error) where T : notnull;   // the value, or an error
public union Result(Success, Error);                  // success without a value, or an error
public union Error(ValidationError, NotFoundError, ConflictError, ForbiddenError, BusinessRuleError);
```

Every error kind is a record with a stable `Code`, such as `Booking.SlotTaken`, and a `Description`. `ValidationError` also lists the messages per field. Code checks results with pattern matching:

```csharp
var slotResult = TimeSlot.Create(request.Start, request.End);
if (slotResult is not TimeSlot slot)
{
    return slotResult.Error;
}

if (booking.Cancel(request.Reason) is Error error)
{
    return error;
}
```

The endpoints map each error kind to its HTTP status with a `switch` over `Error` (`ResultExtensions.ToProblem`). The switch has no default branch, so a new error kind does not compile until it is mapped. The validation behavior returns a `ValidationError`. The unit of work turns database conflicts into a `ConflictError`.

Only truly unexpected problems are exceptions; the exception handler turns those into a 500. The architecture tests check that no layer defines its own exception types.

### Booking rules

The club's rules are in `Club:Bookings` (`BookingPolicy`):

| Rule | Default |
| --- | --- |
| Time zone of the club; opening hours follow it, summer time included | `Europe/Amsterdam` |
| Opening hours (a booking starts and ends on the same day) | 07:00 to 23:00 |
| Bookings start and end on a multiple of | 30 minutes |
| Upcoming bookings per member | 3 |
| Members can cancel up to … before the start (admins until the start) | 2 hours |

A booking also lasts 30 minutes to 3 hours, is in the future, and needs an active member and an available court. A member cannot be on two courts at the same time. Send an `Idempotency-Key` header with `POST /api/v1/bookings` to retry safely: a retry with the same key returns the same booking.

Changes accept `If-Match` with the `ETag` from a previous response. When the resource changed in the meantime, the API returns 412 and does not apply the change.

### Business rules enforced in the database

- `IX_Members_Email`: a unique email address per member.
- `EX_Bookings_NoOverlappingSlots` and `EX_Bookings_NoOverlappingMemberSlots`: exclusion constraints (`btree_gist`). Bookings that are not cancelled may not overlap on the same court, or for the same member.
- `IX_Bookings_MemberId_IdempotencyKey` and `IX_Members_IdentityId`: one booking per idempotency key, and one member per account.
- An `xmin` row version gives optimistic concurrency on every table.

Violations of these constraints return **409 Conflict**.

## API conventions

- **JSON:** requests and responses are JSON. Enums are written as strings.
- **Errors:** RFC 9457 problem details (`application/problem+json`). Each problem includes a `traceId`. Application errors also include an `errorCode`, such as `Member.EmailInUse`, that clients can rely on.
- **Versions:** the version is part of the path: `/api/v1/members`. Every response has an `api-supported-versions` header. An unknown version, such as `/api/v2/members`, returns 404. Each version has its own OpenAPI document (`/openapi/v1.json`). To add a version, add it to `ApiVersioning.Versions` in `Padel.Api/Http/ApiVersioning.cs`; endpoints that differ between versions use `MapToApiVersion`.
- **Lists:** every list endpoint uses cursor paging with `cursor`, `pageSize` (1–100) and `sort`, for example `sort=lastName,-registeredOn`. A `-` before a field sorts it descending. Each list also has its own filters:
  - `GET /api/v1/members`: `status`, `search`
  - `GET /api/v1/courts`: `status`, `surface`, `search`
  - `GET /api/v1/bookings`: `memberId`, `courtId`, `status`, `from`, `to`
  - `GET /api/v1/courts/{id}/bookings`: `status`, `from`, `to`

  The response contains `items` and `nextCursor`. To get the next page, send `nextCursor` back as `cursor` with the same `sort` and filters. On the last page, `nextCursor` is `null`.

  How it works:
  - The cursor is opaque: base64url JSON with the sort keys and id of the last item. The next page starts after that item (keyset paging), so the database does not count or skip rows, and items that are added or removed between requests do not cause skipped or repeated items.
  - The id breaks ties between equal sort keys. Enums sort by their order in the enum.
  - A cursor only works with the sort it was made for. A cursor for another sort, or one that is not valid, returns a 400.
  - Paging only goes forward, and there is no total count.

  Unknown sort fields return a 400 that lists the allowed fields.
- **Status codes:**

  | Code | Meaning |
  | --- | --- |
  | 400 | Validation error, with the errors listed per field |
  | 401 | Not authenticated |
  | 403 | Not allowed |
  | 404 | Not found |
  | 409 | Conflict |
  | 412 | The resource changed since it was read (`If-Match`) |
  | 422 | A business rule was violated |
  | 429 | Too many requests |

- **Authorization:**
  - Admins (realm role `admin`) manage members and courts.
  - Members (accounts linked to a member) can only see and change their own profile and bookings.
  - Every authenticated user can view courts and court schedules.

## Rate limiting

Each caller has request limits per minute: the account (`sub`) when signed in, otherwise the IP address.

| Limit | Default | Setting |
| --- | --- | --- |
| All of `/api` | 120 requests | `RateLimiting:PermitsPerMinute` |
| `POST /api/v1/bookings` | 10 new bookings | `RateLimiting:BookingPermitsPerMinute` |

A caller over the limit gets a **429** problem with a `Retry-After` header. Health checks and the documentation are not limited.

The counters are kept in Redis when the connection string `ConnectionStrings:redis` is set (Aspire sets it), so the limits hold across all API instances. Without it, each instance counts on its own, which is fine for a single instance. When Redis cannot be reached, requests are let through without a limit and a warning is logged: an unavailable rate-limit store should not take the API down. Set `RateLimiting:KeyPrefix` (default `padel`) when several apps or environments share one Redis.

## Behind a reverse proxy

Behind a reverse proxy or ingress, the API takes the client's address and scheme from `X-Forwarded-For` and `X-Forwarded-Proto`. It uses them for logs, rate limiting and the links it returns (`Location`). It only trusts these headers from localhost and from the proxies you configure:

| Setting | Example |
| --- | --- |
| `ForwardedHeaders:KnownProxies` | `["10.0.0.5"]` (IP addresses) |
| `ForwardedHeaders:KnownNetworks` | `["10.0.0.0/8"]` (CIDR, for example the cluster network) |

Headers from any other address are ignored, so clients cannot fake their address.

## Logging

Every command and query is logged by `LoggingBehavior`, in the category `Padel.UseCases`:

- the use case;
- the caller: the account (`sub`), the linked member, and whether the caller is an admin;
- the request and the result as JSON, or the error code and description;
- the duration.

Lists log the number of items, their ids and whether there is a next page. Successes and expected failures are logged at Information, exceptions at Error. Logs written during the use case, for example by EF Core, carry the use case and the user as scope, and every entry has the trace id, so a request can be followed from log to trace in the Aspire dashboard.

The entries contain personal data as is ([ADR 15](docs/adr/0015-log-every-use-case.md)). Treat the log store like the database: limit access and set a retention period.

## Health checks

These endpoints need no token.

| Endpoint | Checks | Use |
| --- | --- | --- |
| `/health/live` | Only that the process responds | Liveness probe: restart when it fails |
| `/health/ready` | The database is reachable and has every migration this version expects | Readiness probe: send no traffic when it fails. Aspire waits for it. |
| `/health` | Every check, with details as JSON | Development only |

| Check | In readiness | Reports |
| --- | --- | --- |
| `database` | Yes | Unhealthy when the database cannot be reached. |
| `database-migrations` | Yes | Unhealthy while migrations are pending, so a new version gets no traffic until the migration service has run. Once up to date, later probes skip the query. |
| `outbox` | No | Degraded when the oldest waiting domain event is older than `Outbox:StaleAfter` (5 minutes), or when messages failed every attempt. The API still works, but notifications lag behind. |
| `keycloak` | No | Unhealthy when Keycloak's OpenID configuration cannot be fetched. |
| `StackExchange.Redis` | No | Unhealthy when Redis cannot be reached (added by Aspire's Redis client). Rate limiting then fails open. |

Keycloak and Redis are deliberately not part of readiness. The API validates tokens with cached signing keys, and rate limiting lets requests through without Redis, so a short outage of either should not take the API out of service. Liveness never checks dependencies: a database outage should not make the orchestrator restart every instance.

## Tests

```bash
dotnet test
```

| Project | What it tests |
| --- | --- |
| `Padel.UnitTests` | Domain and Application, using in-memory fakes. |
| `Padel.ArchitectureTests` | Layer dependencies, vertical slices and naming conventions. |
| `Padel.IntegrationTests` | The whole API, using `WebApplicationFactory` and Refit clients. It runs real PostgreSQL, Redis and Keycloak containers (Testcontainers). Each test member gets its own Keycloak account with the member's verified email address. |

Test names follow `Action_GivenState_ShouldOutcome` (existing data or the caller) or `Action_WhenInput_ShouldOutcome` (what the action receives), for example `CreateBooking_GivenSuspendedMember_ShouldReturn422` and `CreateBooking_WhenSlotInThePast_ShouldReturn422`. Tests without a scenario are named `Action_ShouldOutcome`.

Assertions use AwesomeAssertions (`value.Should().Be(expected)`). Results have their own helpers: `result.ShouldSucceed()` returns the value, and `result.ShouldFail().Is<NotFoundError>()` checks the kind of error. Only `TUnit.Engine` is referenced, so TUnit's own assertions are not available and cannot creep back in.

Test data comes from builders based on AutoFaker ([Soenneker.Utils.AutoBogus](https://github.com/soenneker/soenneker.utils.autobogus)). A builder's `New()` returns a `Faker<T>` with valid random values, and its extension methods override one rule each:

```csharp
var member = _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com").Suspended());
var request = CreateBookingRequestBuilder.New().For(member.Id).On(court.Id).StartingAt(start).Generate();
```

Set only the values the test depends on; the rest is random.

Handler tests use `HandlerFixture`: in-memory repositories that run the same LINQ as EF Core, a `FakeUnitOfWork` that collects domain events, a `FakeTimeProvider` and a `FakeCurrentUser`. NSubstitute is only used for interactions and failure injection, for example to check that a notification was sent or to make a save fail. Repositories are not mocked ([ADR 14](docs/adr/0014-test-strategy.md)). The unit tests have builders for the entities (`MemberBuilder`, `CourtBuilder`, `BookingBuilder`). These create the entity through its domain factory, then set every property with a rule, because AutoFaker would otherwise fill private setters with random values. The builders produce a given state directly. Tests of the domain rules themselves call the factories (`Booking.Create`, `Member.Register`). The integration tests have builders for the API requests.

## CI and container images

[.github/workflows/ci.yml](.github/workflows/ci.yml) runs on every push to `main` and on every pull request:

1. It builds the solution; code style is part of the build.
2. It checks that the model has no changes without a migration.
3. It runs all tests, including the integration tests with Testcontainers. The TRX and coverage files are uploaded as artifacts.
4. It builds both container images.

The actions are pinned to a commit SHA, with the version in a comment, and the NuGet packages are cached. [Dependabot](.github/dependabot.yml) opens weekly pull requests for NuGet packages (grouped, for example all of Aspire together) and for the actions.

To build the images locally (Docker required):

```bash
dotnet publish src/Padel.Api -c Release -t:PublishContainer                # image padel-api
dotnet publish src/Padel.MigrationService -c Release -t:PublishContainer   # image padel-migrations
```

## Migrations

`Padel.MigrationService` applies the migrations, then exits. The API does not migrate on start-up, because several instances would race each other. Locally, Aspire runs the migration service first and starts the API once it has completed. In production, run it as a separate step before rolling out the API, for example as a Kubernetes Job (container image `padel-migrations`).

To add a migration:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Padel.Infrastructure --output-dir Persistence/Migrations --namespace Padel.Infrastructure.Persistence.Migrations
```

EF Core cannot model some objects, so the migrations create them with SQL:

- the exclusion constraints (no overlapping bookings per court or per member);
- the unique email index;
- the trigram search indexes.
