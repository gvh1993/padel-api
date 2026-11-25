# Code review: padel-api

> **Achterhaald.** Dit rapport beschrijft de code zoals die was vóór de ombouw: controllers, exceptions, XML en geen Aspire of Keycloak. De huidige opzet staat in de [README](README.md). Wat er sindsdien is veranderd, staat in de git-geschiedenis (`git log`).

**Datum:** 25 november 2026
**Scope:** de hele repository (5 projecten, ongeveer 2.500 regels)
**Hoe gecontroleerd:**
- Alle bronbestanden zijn gelezen.
- Lokaal is alleen SDK 10 geïnstalleerd en het project target `net11.0`. Daarom is een kopie omgezet naar `net10.0` en zijn de packages naar 10.0.0 gezet. Die kopie is gebouwd, getest en gestart, en de endpoints zijn met echte HTTP-calls aangeroepen.

## Samenvatting

De architectuur is netjes opgezet. Er is een duidelijke scheiding tussen Domain, Application, Infrastructure en Api. De domeinmodellen bewaken hun eigen regels, er is een eigen dispatcher met pipeline behaviors en de domeintests zijn goed (58 tests, alle 58 slagen).

**Toch werkt de applicatie op dit moment niet:**
- De solution bouwt niet.
- Ook na een fix daarvoor faalt elk endpoint dat de database gebruikt.

Beide problemen zitten in lagen zonder tests: Infrastructure en Api.

| Ernst | Aantal |
|---|---|
| Blokkerend | 4 |
| Hoog | 6 |
| Middel | 8 |
| Laag / opruimen | 9 |

---

## 1. Blokkerend: de app bouwt of draait niet

### 1.1 Application-project bouwt niet: verwijzing naar Logging ontbreekt
[LoggingBehavior.cs:3](src/Padel.Application/Behaviors/LoggingBehavior.cs#L3) gebruikt `ILogger<T>`. [Padel.Application.csproj](src/Padel.Application/Padel.Application.csproj) verwijst niet naar `Microsoft.Extensions.Logging.Abstractions`. De versie staat wel in [Directory.Packages.props:7](Directory.Packages.props#L7).
**Bevestigd met de build:** `error CS0234: The type or namespace name 'Logging' does not exist`.
**Fix:** voeg `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />` toe.

### 1.2 EF Core-mapping van value objects is ongeldig: elk DB-endpoint geeft 500
[PadelDbContext.cs:19-21](src/Padel.Infrastructure/PadelDbContext.cs#L19-L21) en [39-43](src/Padel.Infrastructure/PadelDbContext.cs#L39-L43) gebruiken geneste expressies zoals `Property(e => e.Name.First)` en `HasIndex(e => new { e.Slot.Start, ... })`. EF Core ondersteunt dat niet.
**Bevestigd bij het draaien:** `ArgumentException: The expression 'e => e.Name.First' is not a valid member access expression`. Dit gebeurt bij **elke** request die de DbContext gebruikt.
**Fix:**
- Map `FullName`, `Email` en `TimeSlot` met `ComplexProperty(...)` of `OwnsOne(...)`.
- Geef de value objects een private parameterloze constructor of een constructor die EF kan binden.
- Zet de index op shadow- of complex-kolommen.

### 1.3 Database wordt nooit aangemaakt
Er zijn geen migrations en er is geen `Database.Migrate()` of `EnsureCreated()`. [padel.db](src/Padel.Api/padel.db) is 0 bytes. Ook als 1.2 is opgelost, bestaan de tabellen dus niet. `Microsoft.EntityFrameworkCore.Design` is wel geïnstalleerd, maar wordt niet gebruikt.
**Fix:** maak een eerste migration (`dotnet ef migrations add Initial`) en pas die toe bij het opstarten in Development, of via een aparte stap.

### 1.4 De ExceptionMiddleware gooit zelf een exception bij een 500
[ExceptionMiddleware.cs:36-39](src/Padel.Api/Middleware/ExceptionMiddleware.cs#L36-L39) zet de header `X-Trace-Id` **nadat** de body al geschreven is.
**Bevestigd:** `InvalidOperationException: Headers are read-only, response has already started`. Bij POST-requests werd de verbinding zelfs verbroken ("underlying connection was closed"), zodat de client helemaal geen nette foutmelding krijgt.
Daarnaast:
- Onverwachte exceptions worden hier niet gelogd.
- De header bevat de exception-typenaam, geen echte trace-id. Dat lekt informatie.

**Fix:** gebruik `IExceptionHandler` met `AddProblemDetails()`, log de exception en zet `Activity.Current?.Id` / `HttpContext.TraceIdentifier` **vóór** het schrijven van de body.

---

## 2. Hoog

### 2.1 List-endpoints geven een `Task` als JSON terug
[ApiEndpoints.cs:23-24](src/Padel.Api/Endpoints/ApiEndpoints.cs#L23-L24), [54-55](src/Padel.Api/Endpoints/ApiEndpoints.cs#L54-L55), [85-86](src/Padel.Api/Endpoints/ApiEndpoints.cs#L85-L86) en [100-106](src/Padel.Api/Endpoints/ApiEndpoints.cs#L100-L106) zijn `async`-lambdas die `dispatcher.Send(...)` teruggeven **zonder `await`**. Het returntype wordt dan `Task<Task<T>>`. Minimal API serialiseert in dat geval het binnenste `Task`-object.
**Bevestigd:** GET `/api/members` faalt met een `AggregateException` uit `Task.ThrowIfExceptional` (de serializer leest `Task.Result`). Zelfs als het goed gaat, krijg je een JSON-object met `Status`, `IsCompleted` enzovoort, in plaats van de lijst. Een `NotFound` of `Validation` wordt daardoor ook een 500.
**Fix:** `async (...) => Results.Ok(await dispatcher.Send<...>(...))`, of laat `async` weg.

### 2.2 OpenAPI-document ontbreekt (Scalar is leeg)
[Program.cs:26-27](src/Padel.Api/Program.cs#L26-L27) roept `MapOpenApi()` en `MapScalarApiReference()` aan, maar `builder.Services.AddOpenApi()` ontbreekt.
**Bevestigd:** `/openapi/v1.json` geeft `404 No OpenAPI document with the name 'v1' was found`.
Let op: deze endpoints staan ook in productie open. Overweeg ze alleen te mappen als `app.Environment.IsDevelopment()`.

### 2.3 Een boeking controleert niet of lid en baan bestaan of beschikbaar zijn
[CreateBookingCommand.cs:19-36](src/Padel.Application/Bookings/CreateBookingCommand.cs#L19-L36) gebruikt alleen de booking-repository. Daardoor kan het volgende:
- boeken met een niet-bestaand `MemberId` of `CourtId`;
- boeken als **geschorst** lid;
- boeken op een baan die **Retired** is of in **Maintenance** staat;
- boeken in het verleden.

Er zijn ook geen foreign keys tussen `Bookings` en `Members`/`Courts`.

### 2.4 Dubbele boekingen en dubbele e-mails zijn mogelijk door een race condition
Zowel de overlapcontrole ([CreateBookingCommand.cs:23-28](src/Padel.Application/Bookings/CreateBookingCommand.cs#L23-L28)) als de e-mailcontrole ([RegisterMemberCommandHandler.cs:13-18](src/Padel.Application/Members/RegisterMemberCommandHandler.cs#L13-L18)) volgen het patroon "eerst lezen, dan invoegen", zonder transactie of DB-constraint. Twee gelijktijdige requests komen allebei door de controle.
**Fix:**
- Een unieke index op `Email`.
- Voor boekingen: de controle en de insert in een serializable transactie, of een optimistic-concurrency-token per baan/dag.

### 2.5 Domain event handlers worden nooit aangeroepen
[ApplicationServiceCollection.cs:26-40](src/Padel.Application/ApplicationServiceCollection.cs#L26-L40) registreert alleen `IRequestHandler<,>`, niet `IDomainEventHandler<>`. Er bestaan ook nog geen handlers. Alle events gaan daardoor ongemerkt verloren. Wie later een handler toevoegt, ziet die zonder foutmelding niet uitgevoerd worden.
Verder publiceert [PadelUnitOfWork.cs:13-29](src/Padel.Infrastructure/PadelUnitOfWork.cs#L13-L29) de events **na** de commit. Als een handler faalt, krijgt de client een fout terwijl de data al is opgeslagen. Overweeg een outbox, of maak expliciet dat dit "best effort" is.

### 2.6 Infrastructure en Api hebben geen tests
[Padel.Tests.csproj](tests/Padel.Tests/Padel.Tests.csproj) verwijst alleen naar Domain en Application. Alle vier blokkerende bugs en 2.1/2.2 zitten in lagen zonder tests. Eén integratietest met `WebApplicationFactory` en SQLite in-memory had ze allemaal gevonden.

---

## 3. Middel

| # | Bevinding | Locatie |
|---|---|---|
| 3.1 | `DateTime` zonder tijdzone in `TimeSlot`/`Booking`. Clients die lokale tijd versturen geven andere waarden dan clients die UTC versturen, en SQLite slaat ze op als tekst. Gebruik `DateTimeOffset`, of normaliseer naar UTC en valideer `Kind`. | [TimeSlot.cs:8-9](src/Padel.Domain/Common/TimeSlot.cs#L8-L9) |
| 3.2 | Endpoints geven geen `CancellationToken` door, dus de dispatcher krijgt altijd `default` en afgebroken requests lopen gewoon door. | [ApiEndpoints.cs](src/Padel.Api/Endpoints/ApiEndpoints.cs) |
| 3.3 | De volgorde van pipeline behaviors hangt af van `Assembly.GetTypes()`, die geen gegarandeerde volgorde heeft. Logging en validatie kunnen dus wisselen. Registreer ze expliciet. | [ApplicationServiceCollection.cs:47-75](src/Padel.Application/ApplicationServiceCollection.cs#L47-L75) |
| 3.4 | `ValidationBehavior` gebruikt het synchrone `Validate`. Async-regels (`MustAsync`) gooien dan een exception. Gebruik `ValidateAsync`. | [ValidationBehavior.cs:19-23](src/Padel.Application/Behaviors/ValidationBehavior.cs#L19-L23) |
| 3.5 | De naam `ValidationException` botst met `FluentValidation.ValidationException`. Dat werkt nu alleen dankzij de regels voor naamresolutie, wat verwarrend en kwetsbaar is. Hernoem naar bijvoorbeeld `RequestValidationException`. | [ValidationException.cs](src/Padel.Application/ValidationException.cs) |
| 3.6 | Foutformaat `{code, message}` in plaats van standaard `ProblemDetails` (RFC 9457). Validatiefouten komen als één string in plaats van per veld. | [ExceptionMiddleware.cs:43-50](src/Padel.Api/Middleware/ExceptionMiddleware.cs#L43-L50) |
| 3.7 | Geen authenticatie of autorisatie. Iedereen kan leden schorsen, banen afschrijven en boekingen van anderen annuleren. | [Program.cs](src/Padel.Api/Program.cs) |
| 3.8 | List-endpoints hebben geen paginering. `/api/bookings` geeft alle boekingen ooit. | [ApiEndpoints.cs:85](src/Padel.Api/Endpoints/ApiEndpoints.cs#L85) |

---

## 4. Laag / opruimen

1. **Geen `global.json`.** Pin de SDK (net11 preview). Dan krijgt iemand zonder die SDK meteen een duidelijke melding in plaats van NETSDK1045.
2. **Geen git, `.gitignore` of README.** `bin/`, `obj/`, `padel.db`, `api-err.log` en `api-out.log` staan los in de map.
3. **Dubbele projectinstellingen.** `TargetFramework`, `Nullable` en `ImplicitUsings` staan in elk csproj. Verplaats ze naar [Directory.Build.props](Directory.Build.props). `TreatWarningsAsErrors` staat nu alleen in Domain; zet het overal aan.
4. **Ongebruikte packageversie:** `Microsoft.Extensions.DependencyInjection.Abstractions` wordt nergens gerefereerd.
5. **Value objects** (`Email`, `FullName`, `TimeSlot`) zijn classes zonder value-equality. Maak er `sealed record`s van.
6. **Dubbel werk:**
   - het mappen naar `MemberDto`/`CourtDto` staat op 3 à 4 plekken (er is wel een `BookingMapper`);
   - redundante `CourtId`-filters in [CreateBookingCommand.cs:25](src/Padel.Application/Bookings/CreateBookingCommand.cs#L25) en [GetBookingQuery.cs:50](src/Padel.Application/Bookings/GetBookingQuery.cs#L50).
7. **Domeingedrag zonder endpoint:** `Complete`, `MarkAsNoShow`, `Reactivate`, `UpdateProfile`, `SetMaintenance` en `GetForMemberAsync`. De overgang `NoShow → Completed` is inhoudelijk ook twijfelachtig.
8. **Inconsistente bestandsindeling:**
   - `RegisterMemberCommandHandler` staat in een eigen bestand, de andere handlers niet;
   - `GetMemberQuery.cs` bevat de DTO en de list-handler;
   - de drie endpoint-klassen staan in één `ApiEndpoints.cs`;
   - losse `;`-regels na `.WithName(...)`.
9. **Ongebruikte test-fake:** `RecordingDomainEventPublisher` in [InMemoryFakes.cs:87](tests/Padel.Tests/Fakes/InMemoryFakes.cs#L87). Ook de dispatcher en de behaviors hebben geen tests.

---

## Wat goed is

- Duidelijke laagindeling en afhankelijkheden in de goede richting (Domain heeft geen afhankelijkheden).
- Aggregates beschermen hun eigen regels: statusovergangen, idempotente `Suspend`/`Retire`, en validatie in value objects.
- Een lichte eigen mediator zonder externe afhankelijkheid. De opbouw van de pipeline is correct.
- Centraal pakketbeheer (`Directory.Packages.props`).
- Leesbare tests met duidelijke namen en goede dekking van het domein.

## Aanbevolen volgorde

1. Fix 1.1 t/m 1.4 en 2.1/2.2, zodat de API werkt.
2. Voeg één integratietestproject toe (`WebApplicationFactory` met SQLite in-memory) dat elk endpoint één keer aanroept.
3. Pak 2.3 t/m 2.5 aan (bedrijfsregels en dataconsistentie).
4. Daarna de punten uit §3 en §4.

## Opmerking over de verificatie

- De build- en runtimeresultaten komen van een kopie die naar `net10.0` en packages 10.0.0 is omgezet, omdat de .NET 11 SDK lokaal ontbreekt. De gevonden fouten (ontbrekende referentie, EF-mapping, middleware, async-lambdas, ontbrekende `AddOpenApi`) hangen niet af van die versie.
- NuGet meldde bij de 10.0-versies kwetsbare transitieve packages, onder andere `SQLitePCLRaw.lib.e_sqlite3` en `Microsoft.OpenApi`. Of dat ook voor de 11.0-versies geldt, is niet gecontroleerd. Draai `dotnet list package --vulnerable --include-transitive` met de echte SDK.

---

## Status na de fixes (25 november 2026)

Alle bevindingen zijn verwerkt. Daarnaast zijn de aanvullende wensen gerealiseerd: Scalar, Testcontainers, Refit, OpenTelemetry, JSON/XML, en pagination/filtering/sorting.

**Verificatie (met de .NET 11.0.100 SDK):**
- Build: 0 warnings, 0 errors, met `TreatWarningsAsErrors` overal aan.
- 158 unit tests (TUnit): allemaal geslaagd.
- 51 integratietests (TUnit + Testcontainers PostgreSQL + Refit): allemaal geslaagd.

| # | Oplossing |
|---|---|
| 1.1 | `Logging.Abstractions` is op net11 al beschikbaar via package pruning. De Application-laag bouwt. |
| 1.2 | Value objects zijn records en worden gemapt als EF Core *complex properties*. |
| 1.3 | Overgestapt op PostgreSQL met een `InitialCreate`-migration. Die wordt bij het opstarten toegepast als `Database:ApplyMigrationsOnStartup` aan staat. |
| 1.4 | De middleware is vervangen door `IExceptionHandler` met RFC 9457 problem details (JSON of XML) en een `traceId`. Onverwachte fouten worden gelogd. |
| 2.1 | Overgestapt op controllers, omdat die content negotiation ondersteunen. Endpoints krijgen nu het juiste resultaat terug. |
| 2.2 | `AddOpenApi` en Scalar zijn toegevoegd, met een bearer-securityscheme en XML-doc-comments. Ze staan alleen aan via `OpenApi:Enabled` (aan in Development). |
| 2.3 | `Booking.Create` controleert of het lid actief is, of de baan beschikbaar is en of het slot in de toekomst ligt. Er zijn foreign keys naar Members en Courts. |
| 2.4 | Een unieke index op e-mail en een exclusion constraint op baan + tijdslot. Gelijktijdige requests zijn getest: precies één 201, de rest krijgt 409. |
| 2.5 | Event handlers worden automatisch geregistreerd. Events worden vóór de commit afgehandeld, in dezelfde transactie. |
| 2.6 | Integratietests met `WebApplicationFactory`, Testcontainers en Refit. |
| 3.1 | `DateTimeOffset` wordt overal genormaliseerd naar UTC. |
| 3.2 | Endpoints geven de `CancellationToken` door. |
| 3.3 | Behaviors staan in een expliciete volgorde: eerst telemetry, dan validatie. |
| 3.4 | `ValidateAsync` wordt gebruikt. |
| 3.5 | `RequestValidationException` met fouten per veld. |
| 3.6 | Problem details, met camelCase-veldnamen in `errors`. |
| 3.7 | JWT bearer. Beheer-endpoints vereisen de rol `admin`. Leden hebben alleen toegang tot hun eigen data, via de `member_id`-claim. |
| 3.8 | `page`, `pageSize` en `sort` met een whitelist van velden, plus filters per resource. |
| 4.1–4.9 | `global.json`, git, `.gitignore`, README, `Directory.Build.props`, records voor value objects, mappers, endpoints voor al het domeingedrag (en `NoShow → Completed` verwijderd), een consistente bestandsindeling en tests voor de dispatcher en de behaviors. |
