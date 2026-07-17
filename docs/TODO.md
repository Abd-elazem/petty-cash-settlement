# TODO

## Milestone 0 — Requirements & Risk Analysis
Status: Done. Functional/non-functional requirements, risk lists, initial stack recommendation.

## Milestone 0.1 — Core Architecture
Status: Done. Domain model, entity relationships, logical DB schema, state machine, RBAC model, folder structure, API spec (surface), UI navigation map — see /ARCHITECTURE.md.
Assumptions logged: A-001–A-015 (docs/ASSUMPTIONS.md). Decisions logged: D-001–D-011 (docs/DECISIONS.md).
Stack/persistence conflict with original CONTEXT.md scaffold resolved and confirmed by client 2026-07-13: .NET backend, Postgres dev-only / SharePoint production System of Record.
Open before Phase 2 build: A-006, A-007 need explicit business sign-off (financial control gaps). A-014 (auth approach) worth revisiting given confirmed .NET stack.

## Milestone 0.1.1 — Full architecture approval + TECH_STACK.md
Status: Done. Client approved Clean Architecture, DI, REST API, mobile-first, future-client parity, and "every external system incl. PostgreSQL sits behind an adapter" as Locked (D-012). Created TECH_STACK.md as the authoritative Locked/Flexible technology record. CONTEXT.md demoted to historical notes only, no longer authoritative.

## Milestone 0.2 — Domain Layer skeleton
Status: **Verified and frozen (except bug fixes), 2026-07-13.** `PettyCash.Domain` builds successfully; all 37 unit tests passed; no compilation errors, no failing tests.
`Settlement` aggregate, `SettlementLine`, `Money`/`VatBreakdown`/`OdometerReading` value objects, state machine, domain events, unit tests. Pure Domain, no infrastructure, no API.
Self-review correction applied and logged as D-014: `Rejected` modeled as its own persisted status (matching Guide §5.5's schema) with an explicit `ReopenForEdit` transition back to Draft, instead of an instant auto-revert. ARCHITECTURE.md §6/§9 and the API spec updated to match.

## Milestone 0.2.1 — Solution file
Status: Done. Created `backend/PettyCash.sln` with `src`/`tests` solution folders; added `PettyCash.Domain`, `PettyCash.Domain.Tests`, and (pre-emptively, same file) `PettyCash.Application`/`PettyCash.Application.Tests` ahead of their creation in 0.3. Convention going forward: every new `.csproj` is added to the solution in the same turn it's created — see ARCHITECTURE.md §8.

## Milestone 0.3 — Application layer
Status: **Approved by client, frozen except bug fixes, 2026-07-13.** Solution builds successfully, all projects compile, 93 automated tests pass, no architectural issues found during review.
Full use-case set implemented (9 commands, 2 queries — see ARCHITECTURE.md §12), repository/external-service interfaces (`ISettlementRepository`, `ICategoryMappingRepository`, `IAppUserProfileRepository`, `IPhotoStore`, `IAuditLogger`, `IVatConfiguration`, `ICurrentUserContext`), DTOs + mapper, centralized `ISettlementAuthorizationPolicy`, FluentValidation validators (one per command), Application exception hierarchy, DI registration (`AddApplication()`), and a comprehensive in-memory-fake-based test suite (`PettyCash.Application.Tests`) covering every handler, the full authorization matrix, validators, and the D-008/D-018 idempotency behavior.
Self-review corrections applied and logged: D-015 (no IUnitOfWork, documented audit-ordering limitation instead of a false atomicity guarantee), D-016 (identity always server-resolved via ICurrentUserContext, never accepted as command input), D-017 (hand-rolled CQRS interfaces, MediatR deliberately not added), D-018 (RecordJournal idempotency concretely implemented), D-019 (CategoryMapping/AppUserProfile are read models, not Domain entities), D-020 (photo upload deliberately deferred rather than working around frozen Domain code — see ASSUMPTIONS.md A-016, new).
Architecture constraints verified: `PettyCash.Application.csproj` references only `PettyCash.Domain`, FluentValidation (+ its DI extensions), and `Microsoft.Extensions.DependencyInjection.Abstractions` — no ASP.NET Core, EF Core, Npgsql, Microsoft.Graph, or any Microsoft.SharePoint/D365FO/Power Automate package.

**Post-verification fix (2026-07-13):** client build confirmed `PettyCash.Domain` and `PettyCash.Application` compile cleanly; only `PettyCash.Application.Tests` failed, with CS0144 ("cannot create an instance of the abstract type or interface 'ISettlementAuthorizationPolicy'") in every handler test that used target-typed `new()` for that constructor parameter. Test-code bug only — no Application design flaw. Fixed by adding one shared `Fakes/TestAuthorizationPolicy.cs` (wraps the real `SettlementAuthorizationPolicy`, since most tests assert actual authorization outcomes rather than needing an always-allow stub) and replacing every bare `new()` across the six affected test files with `TestAuthorizationPolicy.Instance`. No production code changed.

## Sprint 4 — Infrastructure (Development)
Status: Implementation complete, 2026-07-13. Verification pending client build (Docker + one manual `dotnet ef migrations add InitialCreate` step required — see below).
`PettyCash.Infrastructure` created: `PettyCashDbContext`, Fluent API configurations (Settlement with owned Lines collection + owned Money/VatBreakdown/OdometerReading value objects, CategoryMappingReadModel, AppUserProfileReadModel, AuditLogEntry with a shadow-property key), four Postgres repository/service implementations (`PostgresSettlementRepository`, `PostgresCategoryMappingRepository`, `PostgresAppUserProfileRepository`, `PostgresAuditLogger`), `ConfigurationVatConfiguration`, design-time DbContext factory, `AddInfrastructure()` DI registration, and dev seed data (3 category mappings, 1 spender profile).
Frozen-Domain change (client pre-approved): D-021 — `SettlementLine` gets a private parameterless constructor + `LineId` gets a private setter, enabling EF materialization. Zero behavior change; all five frozen-layer conditions checked explicitly in DECISIONS.md.
Self-review decisions logged: D-022 (Lines as EF owned collection, not a DbSet), D-023 (Postgres `xmin` for concurrency, not a hand-rolled RowVersion — corrects ARCHITECTURE.md §5's original sketch), D-024 (reference-data records mapped directly, no parallel Infrastructure DTOs), D-025 (ICurrentUserContext/IPhotoStore intentionally not registered — auth and SharePoint/Graph are out of scope this sprint), D-026 (no cross-repository transaction spanning the audit log write — would make dev stricter than SharePoint can ever be), D-027 (shared Postgres+SharePoint contract-test suite deferred until the SharePoint adapter exists, refining D-002 rather than contradicting it).
Testing: `PettyCash.Infrastructure.Tests` — Testcontainers.PostgreSql (real Postgres, no mocks, per instruction), covering aggregate round-trip (incl. VAT + odometer owned values), not-found, spender-scoped queries, status/version persistence, line removal, optimistic-concurrency conflict, seed-data verification, audit log round-trip, and post-migration table existence.
Architecture constraints verified: Infrastructure references only Domain + Application + EF Core/Npgsql/Configuration packages; Application and Domain have zero reference back to Infrastructure (structurally impossible, not just unused).
**Two things this session could not do, both require the client's machine:**
1. Run `dotnet ef migrations add InitialCreate --project src/PettyCash.Infrastructure --startup-project src/PettyCash.Infrastructure` from `backend/` — no migration files exist on disk yet (ASSUMPTIONS.md A-018). `PostgresContainerFixture` fails with this exact command in its error message if skipped.
2. Run `dotnet build`/`dotnet test` — same standing limitation as every prior milestone, plus Infrastructure.Tests specifically needs Docker running (ASSUMPTIONS.md A-017).

**Post-verification fix (2026-07-13):** client's `dotnet restore` failed before compilation with NU1605 package-downgrade errors (Microsoft.EntityFrameworkCore, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Configuration.Abstractions all explicitly pinned at 9.0.0 while Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4 transitively required 9.0.4+). Fixed via Central Package Management: added `backend/Directory.Packages.props` (`ManagePackageVersionsCentrally` + `CentralPackageTransitivePinningEnabled`, both true), aligned the entire EF Core/Npgsql/Microsoft.Extensions family to exactly 9.0.4 across all five affected `.csproj` files, removed per-project `Version` attributes solution-wide. No suppression used — NU1605 remains a hard error. Full root-cause and versioning-strategy writeup: DECISIONS.md D-028.

**Second post-verification fix (2026-07-13, same day):** with NU1605 resolved, one compilation error remained — `UseXminAsConcurrencyToken()` doesn't exist in Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4 (obsoleted in 7.0, later removed; confirmed via Npgsql's own docs/issue tracker before fixing). Replaced with the currently-documented mechanism: a shadow `uint` property named `xmin` configured via standard EF Core Fluent API. Same outcome, current supported API, not a workaround. Updated DECISIONS.md D-023 and ARCHITECTURE.md §13 to match.

**Third post-verification fix (2026-07-13, same day):** two remaining issues.
(1) `PostgresContainerFixture` called a nonexistent `GetMigrationsAsync()` — confirmed against EF Core's own source/docs that `GetMigrations()` is synchronous-only (reads the migrations assembly, no DB round-trip, so no async variant was ever added). Not a missing `using`. Fixed by removing the `await`/`Async` suffix.
(2) NU1605-equivalent conflict on `Microsoft.EntityFrameworkCore.Relational` resolving as both 9.0.1 and 9.0.4. Traced: Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4's actual dependency floor on Relational is only `>= 9.0.1` (looser than assumed when D-028 was written), while Microsoft.EntityFrameworkCore.Design 9.0.4 requires `>= 9.0.4`. Relational was never given its own explicit `PackageVersion` entry in `Directory.Packages.props` — as a transitive-only package with two different floors from two different direct dependencies, it resolved inconsistently instead of unifying. Fixed by adding the missing explicit central pin (9.0.4, already the higher of the two floors — not a downgrade). Full trace in DECISIONS.md D-028 (updated).
Awaiting client's `dotnet restore` / `build` / `test` re-run.

**Fourth round (2026-07-13, same day) — InitialCreate migration hand-authored** (no dotnet/NuGet execution path exists anywhere in this environment, confirmed): `20260713120000_InitialCreate.cs`, `.Designer.cs`, `PettyCashDbContextModelSnapshot.cs` created under `src/PettyCash.Infrastructure/Migrations/`. Schema matches SettlementConfiguration/CategoryMappingConfiguration/AppUserProfileConfiguration/AuditLogEntryConfiguration exactly (5 tables, FK+cascade delete SettlementLines→Settlements, 2 indexes, 4 seed rows). `xmin` AddColumn deliberately omitted per confirmed Npgsql provider behavior (npgsql/efcore.pg#145) — see DECISIONS.md D-023. A-018 marked resolved. Lowest-confidence artifacts in this delivery are Designer.cs/ModelSnapshot.cs specifically (hand-mirrored EF codegen conventions, not tool-verified) — if a future real `dotnet ef migrations add` reports spurious pending changes, let the tool regenerate ModelSnapshot.cs fresh; that's a safe, non-destructive operation.

## Sprint 5.1 — API Foundation
Status: Implementation complete, 2026-07-14. `PettyCash.Api` project created and added to `backend/PettyCash.sln`; DI wires `AddApplication()` + `AddInfrastructure()`; `appsettings.json`/`appsettings.Development.json` + `Properties/launchSettings.json`; structured JSON console logging; `GlobalExceptionHandler` (ProblemDetails/RFC 9457) mapping the full Application+Domain exception hierarchy; API versioning foundation (Asp.Versioning.Http 8.1.0, URL-segment reader, `/api/v1` convention); built-in OpenAPI (`Microsoft.AspNetCore.OpenApi`, Development-only); Postgres-backed `/health` check.
Self-review correction applied and logged as D-031: first draft of `GlobalExceptionHandler` imported `PettyCash.Domain.Exceptions` directly, which would have compiled (transitively visible via Application) but violated "Api references only Application and Infrastructure." Fixed to match by namespace string before considering the sprint done.
Other Sprint 5 decisions logged: D-029 (built-in OpenAPI, not Swashbuckle — verified against current Microsoft docs), D-030 (Asp.Versioning 8.1.0, not 10.0.0, which targets .NET 10 — verified before pinning, given Sprint 4's package-version history), D-032 (TreatWarningsAsErrors scoped to the new Api project only, not retroactively applied to the just-frozen Domain/Application/Infrastructure build), D-033 (no Serilog — built-in JSON console logging), D-034 (health check verifies real Postgres connectivity, not just process liveness).
Explicitly out of scope, not started: business controllers/endpoints, authentication, SharePoint/Entra/Graph/Power Automate/D365FO.

**Client verification round 1 (2026-07-14):** restore/build/test (112 passing) succeeded; `dotnet run` failed — `ICurrentUserContext` (D-025, deliberately unregistered) had no implementation satisfying it, tripping ASP.NET Core's Development-only startup DI validation. Fixed with `DevelopmentCurrentUserContext` (`PettyCash.Api/Development/`), registered only under `IsDevelopment()` (D-035). No Domain/Application changes.

**Client verification round 2 (2026-07-15):** restore/build/test/run/OpenAPI all succeeded. Two runtime findings raised:
1. `/swagger` → 404. Investigated and confirmed intentional, not a defect — no Swagger UI middleware is registered anywhere; only `AddOpenApi()`/`MapOpenApi()` (the raw JSON document) exists this sprint. Logged as D-036.
2. `/health` → Unhealthy. Root-caused (via a temporary, since-removed Development-only `/health/detail` diagnostic endpoint, so as not to guess) to no PostgreSQL server listening on `127.0.0.1:5432` — `database/` had no docker-compose file or any other provisioning mechanism for a persistent local instance. (`dotnet test` passing does not cover this: `PettyCash.Infrastructure.Tests` uses Testcontainers, an ephemeral container on a random port, unrelated to this persistent one.) Confirmed by the client as the correct root cause — the application and health check were both behaving correctly.

**Fix (2026-07-15):** added `database/docker-compose.yml` (Postgres 16, matching the `PettyCashDev` connection string exactly: db `pettycash_dev`, user/password `postgres`/`postgres`, port 5432, persistent named volume `pettycash_pgdata`, `pg_isready` healthcheck) and `database/README.md` (start/stop/remove instructions, plus the `dotnet ef database update` step needed on a fresh volume). Root `README.md` created (previously empty) with full local run instructions. No application code changed as part of this fix; the temporary diagnostic health endpoint added to investigate was removed once root cause was confirmed.

**Client verification round 3 (2026-07-15):** `docker compose up -d`, `dotnet restore`, `dotnet build`, `dotnet test` (112 passing), `dotnet run`, `/health` → Healthy, and OpenAPI all confirmed. One remaining issue: `dotnet ef database update --project src\PettyCash.Infrastructure --startup-project src\PettyCash.Api` failed — "Your startup project 'PettyCash.Api' doesn't reference Microsoft.EntityFrameworkCore.Design."

**Fix (2026-07-15):** added an explicit `Microsoft.EntityFrameworkCore.Design` `PackageReference` to `PettyCash.Api.csproj` (`PrivateAssets="all"`, same pattern as Infrastructure's existing reference). Root cause: Infrastructure's reference to the Design package doesn't flow to Api via `ProjectReference` because of that same `PrivateAssets="all"`, and `dotnet ef` needs the *startup* project specifically to carry it. No new package version (resolves against the existing central `9.0.4` pin). Pure tooling fix — no application code changed. Full detail: DECISIONS.md D-037.

**Client verification round 4 (2026-07-15):** `dotnet ef database update` re-run confirmed — `InitialCreate` migration applied successfully. This was the last open item from round 3/D-037.

**Sprint 5.1 — CLOSED (2026-07-15).** All verification steps confirmed by the client: `docker compose up -d`, `dotnet restore`, `dotnet build`, `dotnet test` (112 passing), `dotnet run`, `/health` Healthy, OpenAPI, and `dotnet ef database update` (InitialCreate applied). No open items remain for this sprint.

## Vertical Slice 1 — Create Draft Settlement
Status: **CLOSED (2026-07-16). Fully verified by the client.**
`POST /api/v1/settlements` (Minimal API, D-039), reusing the existing `CreateDraftSettlementCommand`/Handler unchanged. Request DTO + explicit validation call (D-040) + OpenAPI metadata + `PettyCash.Api.Tests` (Testcontainers-backed, no mocks).
Bug found and fixed during self-review: `DevelopmentCurrentUserContext`'s hard-coded dev UserId didn't match the seeded `AppUserProfile` row — see DECISIONS.md D-038. Api-layer fixture fix only, no Domain/Application change.
Other decisions logged: D-039 (Minimal API, not MVC controller), D-040 (validation invoked explicitly by the endpoint, no generic filter yet), D-041 (DI eager-capture bug in `AddInfrastructure()`).
`backend/PettyCash.sln` and `Directory.Packages.props` updated in the same change `PettyCash.Api.Tests` was created.
Not in scope this slice: any other command-backed endpoint, `GET /settlements/{id}` (though its route is already documented in ARCHITECTURE.md §9 and the 201 `Location` header already points at it), authentication.

**Post-implementation fixes (2026-07-16):**
- CS0246/CS1061 compile errors in `ApiWebApplicationFactory.cs` — two missing `using` directives (`Xunit`, `Microsoft.EntityFrameworkCore`). No design change.
- Testcontainers connection string not applied — `AddInfrastructure()` eager-capture bug (D-041). Fixed in `InfrastructureServiceCollectionExtensions.cs` (lazy resolution via service provider overload). No production behavior change.

**Client verification (2026-07-16):** `docker compose up -d` ✅, `dotnet restore` ✅, `dotnet build` ✅, `dotnet test` ✅ (all tests passing, incl. 4 new `PettyCash.Api.Tests` integration tests). Slice closed.

## SharePoint + Entra adapters (production)
Scope: real Infrastructure implementations against a sandbox tenant, run through the shared contract-test suite deferred at D-027 (written once both adapters exist).

## Vertical Slice 2 — Add Settlement Line
Status: **CLOSED (2026-07-16). Fully verified by the client.**
`POST /api/v1/settlements/{settlementId}/lines`, reusing the existing `AddLineCommand`/Handler/Validator unchanged. `SettlementId` bound from the route. Returns 200 with the updated `SettlementDto` (D-042 — not 201, since a line isn't independently addressable, D-003).
Continues using `DevelopmentCurrentUserContext` per explicit client instruction — no JWT/Entra work this slice; A-014 remains open, unaddressed.
No Domain/Application/Infrastructure change — pure Api-layer wiring reusing Milestone 0.3/Sprint 4 building blocks (including the seeded `FUEL`/`OFFICE_SUPPLIES`/`GOVERNMENT_FEES` category mappings).
Added `PettyCash.Api.Tests/Settlements/AddSettlementLineEndpointTests.cs` (**6** tests — corrected from an earlier draft's "8"; see CHANGELOG.md). No new `.csproj` — `backend/PettyCash.sln`/`Directory.Packages.props` unchanged.
Post-verification defect found and fixed: `AddSettlementLineRequest.cs` was missing from disk despite an earlier report that it had been created; recreated and confirmed present via read-back before resubmitting (D-043).
Client verification confirmed: `docker compose up -d` ✅, `dotnet restore` ✅, `dotnet build` ✅, `dotnet test` ✅ (116 passing), `dotnet run --project src/PettyCash.Api` ✅.
Not in scope this slice: Update/Remove line, Submit, Approve, Reject, Reopen, RecordJournal endpoints; `GET /settlements/{id}`/`GET /settlements/mine`; authentication.

## Vertical Slice 3 — Submit Settlement
Status: **CLOSED (2026-07-16). Fully verified by the client.**
`POST /api/v1/settlements/{settlementId}/submit`, reusing the existing `SubmitSettlementCommand`/Handler/Validator unchanged. Returns 200 with the updated `SettlementDto`.
Investigation at session start confirmed all VS3 components — endpoint delegate (`SubmitSettlementAsync` in `SettlementsEndpoints.cs`), handler DI registration (`ApplicationServiceCollectionExtensions.cs`), and `SubmitSettlementEndpointTests.cs` (4 tests) — were already present in the repository and already counted in the 116-test total verified at VS2 close. No new production code or test file was written; documentation was the only gap.
Continues using `DevelopmentCurrentUserContext` — no JWT/Entra work this slice; A-014 remains open.
No Domain/Application/Infrastructure change.
Client verification confirmed: `docker compose up -d` ✅, `dotnet restore` ✅, `dotnet build` ✅, `dotnet test` ✅ (116 passing).

## Vertical Slice 4 — Get Settlement Detail
Status: **CLOSED (2026-07-17). Fully verified by the client.**
`GET /api/v1/settlements/{settlementId}` — wraps `GetSettlementByIdQueryHandler` (Milestone 0.3, unchanged). Returns `200 OK` with full `SettlementDto`; throws `NotFoundException` → 404 or `ForbiddenException` → 403 via `GlobalExceptionHandler`. No request body, no validator invocation (query with a route-bound Guid — nothing to validate that the `:guid` constraint doesn't already enforce).
Test file: `PettyCash.Api.Tests/Settlements/GetSettlementEndpointTests.cs` (3 tests: existing-owned-settlement → 200 with full DTO, unknown ID → 404, settlement-with-lines → 200 with lines and correct TotalAmount). Uses the shared `[Collection("Api")]` fixture.
`/mine` is registered above `/{settlementId:guid}` in the route group so the literal segment wins over the Guid constraint.
No Domain/Application/Infrastructure change.

## Vertical Slice 5 — List My Settlements
Status: **CLOSED (2026-07-17). Fully verified by the client.**
`GET /api/v1/settlements/mine` — wraps `GetMySettlementsQueryHandler` (Milestone 0.3, unchanged). Returns `200 OK` with `IReadOnlyList<SettlementSummaryDto>`. No request body; no per-query validator (identity is server-resolved from `ICurrentUserContext`, no client-supplied parameters — D-016). Route registered before `/{settlementId:guid}` so the literal segment `/mine` is resolved first.
Test file: `PettyCash.Api.Tests/Settlements/GetMySettlementsEndpointTests.cs` (3 tests: 200 with non-null list, newly created settlement appears in list with correct summary fields, list deserialises cleanly to `SettlementSummaryDto` — no Lines property). Uses the shared `[Collection("Api")]` fixture.
No Domain/Application/Infrastructure change.

## Vertical Slice 6 — Update Settlement Line
Status: **CLOSED (2026-07-17). Fully verified by the client.**
`PUT /api/v1/settlements/{settlementId}/lines/{lineId}` — wraps `UpdateLineCommandHandler` (Milestone 0.3, unchanged). Both IDs bound from route. Body: `UpdateSettlementLineRequest` (CategoryCode, GrossAmount, IsVat, Notes, CarPlate, OdometerKm). Returns `200 OK` with updated `SettlementDto` (D-042/D-003 — line not independently addressable). Explicit validation via `IValidator<UpdateLineCommand>` (D-040).
Test file: `PettyCash.Api.Tests/Settlements/UpdateSettlementLineEndpointTests.cs` (6 tests: valid non-fuel update → 200 with updated fields, category change to FUEL + odometer → 200, zero amount → 400, car-plate-without-odometer → 400, unknown settlement → 404, unknown category → 404). Uses the shared `[Collection("Api")]` fixture.
No Domain/Application/Infrastructure change.

## Vertical Slice 7 — Remove Settlement Line
Status: **CLOSED (2026-07-17). Fully verified by the client.**
`DELETE /api/v1/settlements/{settlementId}/lines/{lineId}` — wraps `RemoveLineCommandHandler` (Milestone 0.3, unchanged). Both IDs bound from route; no request body. Returns `200 OK` with updated `SettlementDto` (line removed, TotalAmount recomputed). Explicit validation via `IValidator<RemoveLineCommand>` (D-040 — validator only checks non-empty Guids, but kept for consistency).
Test file: `PettyCash.Api.Tests/Settlements/RemoveSettlementLineEndpointTests.cs` (4 tests: existing line removed → 200 with empty Lines + zero TotalAmount, one of two lines removed → 200 with correct remaining line, unknown settlement → 404, unknown line on known settlement → 400 — `Settlement.RemoveLine()` throws `DomainException`, mapped to 400 via namespace match D-031). Uses the shared `[Collection("Api")]` fixture.
No Domain/Application/Infrastructure change.

## Manager + Journal Workflow Batch — VS8 Approve / VS9 Reject / VS10 Reopen / VS11 Record Journal
Status: **CLOSED (2026-07-17). Implemented, tested, and client-verified.**
- **VS8** — `POST /api/v1/settlements/{id}/approve`: wraps `ApproveSettlementCommand`/Handler. Caller role: Approver or System (Power Automate callback). Returns 200 with updated `SettlementDto`.
- **VS9** — `POST /api/v1/settlements/{id}/reject`: wraps `RejectSettlementCommand`/Handler. Body: rejection comment. Returns 200 with updated `SettlementDto`.
- **VS10** — `POST /api/v1/settlements/{id}/reopen`: wraps `ReopenSettlementCommand`/Handler (Rejected → Draft, Version++). Returns 200 with updated `SettlementDto`.
- **VS11** — `POST /api/v1/settlements/{id}/journal`: wraps `RecordJournalCommand`/Handler (Approved → Journalled, System role only). Returns 200 with updated `SettlementDto`.
All four reuse frozen Application-layer handlers and existing endpoint/validation/error-mapping conventions (D-039/D-040/D-031/D-042). No Domain/Application/Infrastructure implementation changes were required during documentation synchronization.

## Remaining implementation work
Scope: production adapters and remaining app surface not yet implemented in the repository (SharePoint/Entra integration, photo upload flow, admin endpoints, auth integration, minimal React shell).

## Documentation synchronization
Status: **In progress / immediate focus.** Repository state is treated as canonical; documentation updates are prioritized to keep `AI_HANDOFF.md`, `CHANGELOG.md`, `docs/TODO.md`, and `ARCHITECTURE.md` aligned with VS1–VS11 verified state.

## Backlog (post-MVP / Guide "Future" phase)
- Duplicate/anomaly line checks
- Petty cash balance Power BI report
- Budget/over-settlement validation (A-006)
- Approver delegation (A-007)
