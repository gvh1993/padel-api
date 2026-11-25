# Padel API

API for a padel club: members, courts and bookings.

**Stack:** .NET 11, ASP.NET Core minimal APIs, EF Core 11 with PostgreSQL, Keycloak, FluentValidation, OpenTelemetry, Scalar, Aspire.
**Tests:** TUnit (engine only) with AwesomeAssertions. The integration tests use Testcontainers (PostgreSQL and Keycloak) and Refit; the architecture tests use NetArchTest.

## Requirements

- .NET SDK 11.0.100 or newer (pinned in [global.json](global.json))
- Docker
- A trusted development certificate: `dotnet dev-certs https --trust`. Aspire serves Keycloak over HTTPS with it.

## Run locally

```bash
dotnet run --project src/Padel.AppHost     # or: aspire run
```

The Aspire AppHost starts PostgreSQL, Keycloak (realm `padel`) and the API. It opens the Aspire dashboard, which shows the resources, their health, logs, traces and metrics.

| What | Where |
| --- | --- |
| API documentation (Scalar) | <http://localhost:5062/scalar>, or the link in the dashboard |
| OpenAPI document | <http://localhost:5062/openapi/v1.json> |
| Keycloak admin console | <https://localhost:8080>. The admin password is in the dashboard, under the `keycloak` resource's parameters. |

In Scalar, choose **Authorize** and sign in with a realm user:

| User | Password | Access |
| --- | --- | --- |
| `admin` | `admin` | Realm role `admin`: manages members, courts and all bookings |
| `member` | `member` | Linked to the member with email `member@padel.example`, once an admin registers one |

Accounts are linked to members automatically. When an account signs in for the first time with the verified email address of a member that is not linked yet, the API links the two; from then on the account (Keycloak `sub`) identifies the member. For example, register a member with the email `member@padel.example`, then sign in as `member`.

The realm is defined in [src/Padel.AppHost/Realms/padel-realm.json](src/Padel.AppHost/Realms/padel-realm.json). Keycloak imports it on first start; after that, the data volume keeps the realm and its users. After you change the file, remove the `keycloak-data` volume to import it again. The integration tests use the same file.

The users and the password grant on `padel-scalar` are for development only.

## Architecture

| Project | Responsibility |
| --- | --- |
| `Padel.Domain` | Aggregates (`Member`, `Court`, `Booking`), value objects (`Email`, `FullName`, `TimeSlot`) and domain events. No dependencies. |
| `Padel.Application` | Use cases as vertical slices, plus the in-house dispatcher with pipeline behaviors (telemetry, validation) and paging, filtering and sorting. |
| `Padel.Infrastructure` | EF Core and PostgreSQL, repositories and the unit of work. It owns the database's telemetry (Npgsql, the application's spans) and the database readiness check. |
| `Padel.Api` | Minimal API endpoints per feature, Keycloak JWT authentication, RFC 9457 problem details, and OpenAPI with Scalar. |
| `Padel.ServiceDefaults` | Cross-cutting service defaults: OpenTelemetry export, health endpoints, service discovery and HTTP resilience. |
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
    MembersGroup.cs                         the /api/members route group
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

### Business rules enforced in the database

- `IX_Members_Email`: a unique email address per member.
- `EX_Bookings_NoOverlappingSlots`: an exclusion constraint (`btree_gist`). No two bookings that are not cancelled may overlap on the same court.
- An `xmin` row version gives optimistic concurrency on every table.

Violations of these constraints return **409 Conflict**.

## API conventions

- **JSON:** requests and responses are JSON. Enums are written as strings.
- **Errors:** RFC 9457 problem details (`application/problem+json`). Each problem includes a `traceId`. Application errors also include an `errorCode`, such as `Member.EmailInUse`, that clients can rely on.
- **Lists:** every list endpoint supports `page`, `pageSize` (1–100) and `sort`, for example `sort=lastName,-registeredOn`. A `-` before a field sorts it descending. Each list also has its own filters:
  - `GET /api/members`: `status`, `search`
  - `GET /api/courts`: `status`, `surface`, `search`
  - `GET /api/bookings`: `memberId`, `courtId`, `status`, `from`, `to`
  - `GET /api/courts/{id}/bookings`: `status`, `from`, `to`

  The response contains `items`, `page`, `pageSize`, `totalCount` and `totalPages`. Unknown sort fields return a 400 that lists the allowed fields.
- **Status codes:**

  | Code | Meaning |
  | --- | --- |
  | 400 | Validation error, with the errors listed per field |
  | 401 | Not authenticated |
  | 403 | Not allowed |
  | 404 | Not found |
  | 409 | Conflict |
  | 422 | A business rule was violated |

- **Authorization:**
  - Admins (realm role `admin`) manage members and courts.
  - Members (accounts linked to a member) can only see and change their own profile and bookings.
  - Every authenticated user can view courts and court schedules.

## Health checks

These endpoints need no token.

| Endpoint | Checks | Use |
| --- | --- | --- |
| `/health/live` | Only that the process responds | Liveness probe: restart when it fails |
| `/health/ready` | The database | Readiness probe: send no traffic when it fails. Aspire waits for it. |
| `/health` | Every check, including Keycloak, with details as JSON | Development only |

Keycloak is deliberately not part of readiness. The API validates tokens with cached signing keys, so a short Keycloak outage should not take the API out of service.

## Tests

```bash
dotnet test
```

| Project | What it tests |
| --- | --- |
| `Padel.UnitTests` | Domain and Application, using in-memory fakes. |
| `Padel.ArchitectureTests` | Layer dependencies, vertical slices and naming conventions. |
| `Padel.IntegrationTests` | The whole API, using `WebApplicationFactory` and Refit clients. It runs real PostgreSQL and Keycloak containers (Testcontainers). Each test member gets its own Keycloak account with the member's verified email address. |

Test names follow `Action_Should_Outcome` and `Given_Context_Action_Should_Outcome`.

Assertions use AwesomeAssertions (`value.Should().Be(expected)`). Results have their own helpers: `result.ShouldSucceed()` returns the value, and `result.ShouldFail().Is<NotFoundError>()` checks the kind of error. Only `TUnit.Engine` is referenced, so TUnit's own assertions are not available and cannot creep back in.

## Migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Padel.Infrastructure --output-dir Persistence/Migrations
```

The exclusion constraint and the unique index are written as SQL in `InitialCreate`, because EF Core cannot model them.
