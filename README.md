# Padel API

API for a padel club: members, courts and bookings.

**Stack:** .NET 11, ASP.NET Core minimal APIs, EF Core 11 with PostgreSQL, Keycloak, FluentValidation, OpenTelemetry, Scalar, Aspire.
**Tests:** TUnit. The integration tests use Testcontainers (PostgreSQL and Keycloak) and Refit; the architecture tests use NetArchTest.

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
| `member` | `member` | Not linked to a member yet |

To link a Keycloak user to a member:

1. Register the member as admin.
2. In the Keycloak admin console, set the user's `member_id` attribute to that member's id. The attribute is in the realm's user profile, and only admins can edit it.

The realm is defined in [src/Padel.AppHost/Realms/padel-realm.json](src/Padel.AppHost/Realms/padel-realm.json). Keycloak imports it on first start; after that, the data volume keeps the realm and its users. After you change the file, remove the `keycloak-data` volume to import it again. The integration tests use the same file.

The users and the password grant on `padel-scalar` are for development only.

## Architecture

| Project | Responsibility |
| --- | --- |
| `Padel.Domain` | Aggregates (`Member`, `Court`, `Booking`), value objects (`Email`, `FullName`, `TimeSlot`) and domain events. No dependencies. |
| `Padel.Application` | Use cases as vertical slices, plus the in-house dispatcher with pipeline behaviors (telemetry, validation) and paging, filtering and sorting. |
| `Padel.Infrastructure` | EF Core and PostgreSQL, repositories and the unit of work. It owns the database's telemetry (Npgsql, the application's spans) and the database readiness check. |
| `Padel.Api` | Minimal API endpoints per feature, Keycloak JWT authentication, RFC 9457 problem details, JSON/XML content negotiation, and OpenAPI with Scalar. |
| `Padel.ServiceDefaults` | Cross-cutting service defaults: OpenTelemetry export, health endpoints, service discovery and HTTP resilience. |
| `Padel.AppHost` | The Aspire app model for local development. |

Code is organised by feature (vertical slices):

```text
src/Padel.Application/Features/Members/
    MemberDto.cs, MemberMappings.cs, MemberSorting.cs, IMemberRepository.cs
    RegisterMember/        RegisterMemberCommand.cs (request + handler), RegisterMemberCommandValidator.cs
    ListMembers/           ListMembersQuery.cs, ListMembersQueryValidator.cs
    ...
src/Padel.Api/Features/Members/MemberEndpoints.cs, MemberRequests.cs
src/Padel.Infrastructure/Features/Members/MemberRepository.cs, MemberConfiguration.cs
```

`Padel.ArchitectureTests` enforces the layering and the slices:

- Dependencies point inwards.
- The members and courts features do not depend on other features.
- Every request has exactly one handler.
- Handlers and validators are sealed.
- Entities have no public setters.

### Business rules enforced in the database

- `IX_Members_Email`: a unique email address per member.
- `EX_Bookings_NoOverlappingSlots`: an exclusion constraint (`btree_gist`). No two bookings that are not cancelled may overlap on the same court.
- An `xmin` row version gives optimistic concurrency on every table.

Violations of these constraints return **409 Conflict**.

## API conventions

- **JSON or XML:** JSON is the default. Send `Accept: application/xml` (or prefer it with `q` values) to get XML. Any other format returns **406**.
- **Errors:** RFC 9457 problem details in the format of the response: `application/problem+json`, or `application/problem+xml` (RFC 9457 appendix B). Each problem includes a `traceId`.
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
  | 406 | Response format not supported |
  | 409 | Conflict |
  | 422 | A business rule was violated |

- **Authorization:**
  - Admins (realm role `admin`) manage members and courts.
  - Members (`member_id` claim) can only see and change their own profile and bookings.
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
| `Padel.IntegrationTests` | The whole API, using `WebApplicationFactory` and Refit clients. It runs real PostgreSQL and Keycloak containers (Testcontainers). Each test member gets its own Keycloak user with a `member_id` attribute. |

Test names follow `Action_Should_Outcome` and `Given_Context_Action_Should_Outcome`.

## Migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Padel.Infrastructure --output-dir Persistence/Migrations
```

The exclusion constraint and the unique index are written as SQL in `InitialCreate`, because EF Core cannot model them.
