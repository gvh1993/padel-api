# 12. API versioning in the URL

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Contracts will change in incompatible ways, as the switch to cursor paging did. Clients need a way to stay on the old contract while they migrate.

## Decision

- The major version is part of the path: `/api/v1/members`. We use Asp.Versioning with `UrlSegmentApiVersionReader`.
- Every response reports `api-supported-versions`; deprecated versions are reported in `api-deprecated-versions`.
- An unknown version (`/api/v2/...`) is a path that does not exist: 404 with problem details.
- Each version has its own OpenAPI document (`/openapi/v1.json`). The API explorer groups endpoints per version (`'v'VVV`) and fills the version into the paths.
- `Asp.Versioning.OpenApi` is not used: it requires Microsoft.OpenApi 2.x and ASP.NET Core 11 uses 3.x (the restore fails with NU1107). Its analyzer suggestions AV0029 and AV0030 are suppressed in `Padel.Api.csproj`.
- A new version is added to `ApiVersioning.Versions`. Endpoints that differ between versions use `MapToApiVersion`.

## Consequences

- The version is visible in every URL, log line and trace, and it is easy to route and cache.
- We considered a header or query parameter (`api-version`): it keeps URLs stable, but it is easier to forget and harder to see.
- Revisit the OpenAPI integration when `Asp.Versioning.OpenApi` supports Microsoft.OpenApi 3.
