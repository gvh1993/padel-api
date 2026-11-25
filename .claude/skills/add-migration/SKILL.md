---
name: add-migration
description: Change the padel API's database schema with an EF Core migration, including constraints EF Core cannot model (exclusion constraints, trigram indexes). Use when an entity, its EF configuration, an index or a database constraint changes, or when CI reports pending model changes.
---

# Add a migration

Migrations live in `src/Padel.Infrastructure/Persistence/Migrations`. `Padel.MigrationService` applies them; the API never migrates on start-up (ADR 10).

## 1. Change the model

- Entity mapping goes in the feature's `IEntityTypeConfiguration` (`Infrastructure/Features/<Feature>/<Entity>Configuration.cs`). Value objects use `ComplexProperty`, and enums are stored as strings (`HasConversion<string>()`).
- Name every index and constraint that the API maps to an error, and add the name to `Persistence/DatabaseConstraints.cs` with the matching domain error in `ErrorFor`. Otherwise the violation becomes a 500 instead of a 409.

## 2. Create the migration

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Padel.Infrastructure --output-dir Persistence/Migrations --namespace Padel.Infrastructure.Persistence.Migrations
```

`<Name>` describes the change in PascalCase, for example `CourtOpeningHours`. Read the generated `Up` and `Down` before you continue: check for data loss (dropped or renamed columns) and for defaults on new non-nullable columns.

## 3. Constraints EF Core cannot model

Add these by hand in `Up`, with the reverse in `Down`. See the `BookingRules` and `SearchIndexes` migrations for examples:

```csharp
migrationBuilder.Sql("""
    ALTER TABLE "Bookings" ADD CONSTRAINT "EX_Bookings_NoOverlappingSlots"
    EXCLUDE USING gist ("CourtId" WITH =, tstzrange("SlotStart", "SlotEnd") WITH &&)
    WHERE ("Status" <> 'Cancelled');
    """);
```

Add a check to `tests/Padel.IntegrationTests/DatabaseTests.cs` for constraints and indexes written by hand, because no model check covers them.

## 4. Verify

```bash
dotnet build PadelApi.slnx
dotnet ef migrations has-pending-model-changes --project src/Padel.Infrastructure   # must report no changes; CI checks this too
dotnet test --project tests/Padel.IntegrationTests   # migrates a fresh PostgreSQL container
```

To try it locally, restart the AppHost: the migration service applies the new migration before the API starts.

Never edit a migration that is already on `main`. Add a new one instead.
