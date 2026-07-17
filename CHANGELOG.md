# CHANGELOG

Human-readable summary of what changed, sprint by sprint. `docs/DECISIONS.md` is the authoritative record of *why*; this file is *what*, briefly.

## Vertical Slices 4–7 — Get Settlement / List My Settlements / Update Line / Remove Line — 2026-07-16
- All four endpoints were found fully implemented in the repository at session start (VS4–VS7 production code in `SettlementsEndpoints.cs` and request DTOs; all four test files in `PettyCash.Api.Tests/Settlements/`). Documentation was stale — did not yet record this work. Per PROJECT_RULES.md §15 (repository is source of truth), code accepted as-is; documentation updated to match.
- **VS4** — `GET /api/v1/settlements/{settlementId}`: `GetSettlementByIdAsync` in `SettlementsEndpoints.cs`, reusing frozen `GetSettlementByIdQueryHandler`. Returns 200+`SettlementDto` or 404/403. `GetSettlementEndpointTests.cs` (3 tests).
- **VS5** — `GET /api/v1/settlements/mine`: `GetMySettlementsAsync`, reusing frozen `GetMySettlementsQueryHandler`. Returns 200+`IReadOnlyList<SettlementSummaryDto>`. Route registered above `/{settlementId:guid}` to guarantee literal-segment wins over the Guid constraint. `GetMySettlementsEndpointTests.cs` (3 tests).
- **VS6** — `PUT /api/v1/settlements/{settlementId}/lines/{lineId}`: `UpdateLineAsync`, reusing frozen `UpdateLineCommandHandler`. Body: `UpdateSettlementLineRequest`. Returns 200+`SettlementDto`. Explicit FluentValidation (D-040). `UpdateSettlementLineEndpointTests.cs` (6 tests).
- **VS7** — `DELETE /api/v1/settlements/{settlementId}/lines/{lineId}`: `RemoveLineAsync`, reusing frozen `RemoveLineCommandHandler`. No request body. Returns 200+`SettlementDto`. Explicit FluentValidation for consistency (D-040). `RemoveSettlementLineEndpointTests.cs` (4 tests).
- No Domain/Application/Infrastructure code changed. `UpdateSettlementLineRequest.cs` was already present on disk alongside the other endpoint files.
- **Verification status: CLOSED (2026-07-17). Fully verified by the client:** `docker compose up -d` ✅, `dotnet restore` ✅, `dotnet build` ✅, `dotnet test` ✅ (all passing), `dotnet run` ✅. Test count confirmed by client — includes the 16 new tests across VS4–VS7 (3+3+6+4).

## Vertical Slice 3 — Submit Settlement — 2026-07-16
- `POST /api/v1/settlements/{settlementId}/submit`: endpoint delegate `SubmitSettlementAsync` in `PettyCash.Api/Endpoints/SettlementsEndpoints.cs`, reusing the existing frozen `SubmitSettlementCommand`/Handler/Validator (Milestone 0.3, `ApplicationServiceCollectionExtensions.cs`). Returns `200 OK` with the updated `SettlementDto`. No Domain/Application/Infrastructure change.
- `SubmitSettlementEndpointTests.cs` (4 tests: `Post_DraftWithLine_ReturnsSubmittedSettlement`, `Post_DraftWithNoLines_Returns400`, `Post_UnknownSettlementId_Returns404`, `Post_AlreadySubmittedSettlement_Returns400`) was already present in the repository and already counted in the 116-test total confirmed at Vertical Slice 2 close. No new test file was written.
- Investigation note: all VS3 components were discovered to be fully implemented in the repository. Resume-protocol verification (reading `SettlementsEndpoints.cs`, `SubmitSettlementCommand.cs`, `ApplicationServiceCollectionExtensions.cs`, `GlobalExceptionHandler.cs`, and `SubmitSettlementEndpointTests.cs`) confirmed completeness before any code was written — consistent with the repository-is-source-of-truth rule.
- **Verification status: CLOSED (2026-07-16). Fully verified by the client:** `docker compose up -d` ✅, `dotnet restore` ✅, `dotnet build` ✅, `dotnet test` ✅ (116 passing). Test count unchanged from VS2 close — VS3 tests were already included.

## Vertical Slice 2 — Add Settlement Line — 2026-07-16
- Added `POST /api/v1/settlements/{settlementId}/lines`: extended `PettyCash.Api/Endpoints/SettlementsEndpoints.cs` + new `AddSettlementLineRequest.cs`. Resolves `IValidator<AddLineCommand>` and `ICommandHandler<AddLineCommand, SettlementDto>` from DI — `AddLineCommand`/Handler/Validator (Milestone 0.3) are completely unchanged. `SettlementId` is bound from the route, not the request body.
- Returns `200 OK` with the updated `SettlementDto` (not `201 Created` — D-042; a line isn't an independently addressable resource, D-003).
- Validation invoked explicitly by the endpoint (same pattern as Vertical Slice 1, D-040). Two error paths exercised for the first time at the Api layer: unknown `SettlementId`/`CategoryCode` → 404 (`NotFoundException`), fuel category missing odometer → 400 (`DomainValidationException`, mapped by namespace per D-031).
- Continues using `DevelopmentCurrentUserContext` per explicit client instruction — no JWT/Entra work in this slice; A-014 remains open.
- Added `PettyCash.Api.Tests/Settlements/AddSettlementLineEndpointTests.cs` (8 tests: non-fuel happy path, fuel+odometer happy path, fuel-missing-odometer 400, empty category 400, zero amount 400, car-plate-without-odometer 400, unknown category 404, unknown settlement 404). No new project — added to the existing `PettyCash.Api.Tests`, no `.sln`/`Directory.Packages.props` change needed.
- `ARCHITECTURE.md` §9/§16 updated to mark this endpoint implemented.
- **Post-implementation fix (2026-07-16):** `AddSettlementLineRequest.cs` was missing from disk (CS0246 in `SettlementsEndpoints.cs`) despite being reported as created in an earlier turn — a tool-reporting/persistence failure, not a namespace/project-inclusion issue. Recreated verbatim; verified present via read-back and directory listing before re-submitting for verification. No design or behavior change.
- **Verification status: CLOSED (2026-07-16). Fully verified by the client:** `docker compose up -d` ✅, `dotnet restore` ✅, `dotnet build` ✅, `dotnet test` ✅ (116 passing), `dotnet run --project src/PettyCash.Api` ✅. Test count corrected to **6** tests in `AddSettlementLineEndpointTests.cs` (non-fuel happy path, fuel+odometer happy path, zero-amount 400, car-plate-without-odometer 400, unknown category 404, unknown settlement 404) — the "8 tests" figure in this entry as originally drafted did not match the file actually written; corrected here rather than left inconsistent.

## Vertical Slice 1 — Create Draft Settlement — 2026-07-15
- Added `POST /api/v1/settlements`: `PettyCash.Api/Endpoints/SettlementsEndpoints.cs` (Minimal API, D-039) + `CreateSettlementRequest.cs`. Resolves `IValidator<CreateDraftSettlementCommand>` and `ICommandHandler<CreateDraftSettlementCommand, SettlementDto>` from DI, exactly as already registered by `AddApplication()` — no new Application/Domain code. Returns 201 with `Location` and the `SettlementDto` body (no separate Api response DTO, consistent with D-024).
- Validation is invoked explicitly by the endpoint (D-040) — Application registers validators but never calls them itself; failures throw the existing `ValidationException`, mapped to 400 by the already-built `GlobalExceptionHandler`.
- **Bug found and fixed (D-038):** `DevelopmentCurrentUserContext`'s hard-coded UserId (`"dev-local-user"`) didn't match the seeded `AppUserProfile` row (`"spender.demo"`) — the endpoint would 404 on every local call. Fixed to match the seed data. Api-layer dev-fixture change only, no Domain/Application impact.
- Added `PettyCash.Api.Tests`: `ApiWebApplicationFactory` (real Api host + Testcontainers Postgres, migrates via the real `PettyCashDbContext`, no mocks) and `CreateSettlementEndpointTests` (happy path incl. seed-data field assertions, empty purpose → 400, default date → 400, over-length purpose → 400). Added to `backend/PettyCash.sln` and `Directory.Packages.props` (`Microsoft.AspNetCore.Mvc.Testing` 9.0.4).
- `ARCHITECTURE.md` §9/§14 updated to mark this endpoint implemented.
- **Verification status:** implementation and self-review complete in this session; this session has no `dotnet build`/`test`/`run` execution access (standing limitation, unchanged since every prior sprint) — awaiting client verification per `AI_HANDOFF.md` §9 before this slice is marked closed.

**Post-implementation fix — compile errors in `PettyCash.Api.Tests` (2026-07-16):** two missing `using` directives in `ApiWebApplicationFactory.cs` surfaced when the test project was first compiled: `using Xunit;` (required for `IAsyncLifetime` — CS0246) and `using Microsoft.EntityFrameworkCore;` (required for the `MigrateAsync()` extension method on `DatabaseFacade` — CS1061). Both were missing-namespace mistakes introduced at file creation; no design or behavior change.

**Post-implementation fix — Testcontainers connection string not applied (2026-07-16):** `ApiWebApplicationFactory`'s in-memory config override (injecting the Testcontainers instance's connection string into `ConnectionStrings:PettyCashDev`) had no effect — all 4 new `PettyCash.Api.Tests` tests connected to `localhost:5432` (the dev Postgres) instead of the container. Root cause: `AddInfrastructure()` captured the connection string eagerly at registration time into a closed-over local, before `WebApplicationFactory.ConfigureWebHost` could merge the override into `IConfiguration`. Fixed by switching to the `AddDbContext<TContext>(IServiceProvider, DbContextOptionsBuilder)` overload so the connection string is resolved lazily per DbContext construction — same config key and runtime value for dev/prod, only read timing changed. See DECISIONS.md D-041.

**Vertical Slice 1 — CLOSED (2026-07-16).** All verification confirmed by the client: `docker compose up -d` ✅, `dotnet restore` ✅, `dotnet build` ✅, `dotnet test` ✅ (all tests passing, including 4 new `PettyCash.Api.Tests` integration tests). No open items remain for this slice.

## Sprint 5.1 — API Foundation — 2026-07-14
- Added `PettyCash.Api` project: minimal-hosting-model `Program.cs`, `GlobalExceptionHandler` (ProblemDetails, RFC 9457), API versioning foundation (Asp.Versioning.Http 8.1.0), built-in OpenAPI (`Microsoft.AspNetCore.OpenApi`), structured JSON console logging, Postgres-backed health check at `/health`.
- References only Application and Infrastructure — verified no Domain project reference; exception handler matches Domain exceptions by namespace string to avoid even a transitive-but-direct type dependency.
- No business endpoints, no authentication, no SharePoint/Entra/Graph/Power Automate/D365FO — explicitly out of scope this sprint.
- `backend/PettyCash.sln` and `Directory.Packages.props` updated.
- **Follow-up fix (same sprint):** local verification found `dotnet run` failing — `ICurrentUserContext` was correctly unregistered (D-025) but nothing satisfied it yet, tripping ASP.NET Core's Development-only DI validation. Added `DevelopmentCurrentUserContext` (`PettyCash.Api/Development/`), a fixed placeholder implementation registered only under `IsDevelopment()` (D-035). No Domain or Application changes. Pending final client-side re-verification (restore/build/test/run, `/health`, OpenAPI) before Sprint 5.1 is marked closed.
- **Runtime verification findings (2026-07-15):** `/swagger` returning 404 was confirmed as expected — no Swagger UI is registered anywhere (D-036); `/openapi/v1.json` is the only OpenAPI artifact this sprint. `/health` returning Unhealthy was root-caused (via a temporary Development-only `/health/detail` diagnostic endpoint, since removed) to no PostgreSQL server running on `127.0.0.1:5432` — there was no docker-compose file, provisioning script, or any other way to get a persistent Postgres instance running locally. This was a missing-infrastructure gap, not an application defect; no application code or health-check behavior was changed.
- **Local development infrastructure added:** `database/docker-compose.yml` — PostgreSQL 16, matching `appsettings.json`'s `PettyCashDev` connection string exactly (`pettycash_dev` / `postgres` / `postgres`, port 5432), with a persistent named volume (`pettycash_pgdata`) and a `pg_isready` healthcheck. `database/README.md` documents start/stop/remove and applying the existing `InitialCreate` migration. Root `README.md` created (previously empty) with full local run instructions.
- **Client verification round 3 (2026-07-15):** docker compose, restore, build, test (112 passing), run, `/health` Healthy, and OpenAPI all confirmed. `dotnet ef database update` failed — startup project `PettyCash.Api` didn't reference `Microsoft.EntityFrameworkCore.Design`.
- **Tooling fix (2026-07-15):** added an explicit, design-time-only `Microsoft.EntityFrameworkCore.Design` `PackageReference` to `PettyCash.Api.csproj` (`PrivateAssets="all"`, matching Infrastructure's existing reference pattern). Infrastructure's own reference doesn't flow to Api via `ProjectReference` because of that same `PrivateAssets="all"`, and `dotnet ef` needs it on the *startup* project specifically. No new package version; no application code changed. Details: DECISIONS.md D-037.
- **Client verification round 4 (2026-07-15):** `dotnet ef database update` re-run succeeded — `InitialCreate` migration applied cleanly against the local Postgres instance. This was the last open item from D-037/round 3. **Sprint 5.1 is now fully complete and verified: `docker compose up -d`, `dotnet restore`, `dotnet build`, `dotnet test` (112 passing), `dotnet run`, `/health` Healthy, OpenAPI, and `dotnet ef database update` (InitialCreate applied) all confirmed.**

## Sprint 4 — Infrastructure (Development) — 2026-07-13
- Added `PettyCash.Infrastructure` project (Postgres dev adapter): `PettyCashDbContext`, Fluent API configurations, four repository/service implementations, DI registration, dev seed data.
- Added `PettyCash.Infrastructure.Tests` project: Testcontainers-based integration tests against real Postgres (no mocks).
- Domain change (approved): `SettlementLine` gained a private parameterless constructor and a settable `LineId`, enabling EF Core materialization. No behavior change — see DECISIONS.md D-021.
- `backend/PettyCash.sln` updated with both new projects.
- **Not done, needs a manual step on the client machine:** `dotnet ef migrations add InitialCreate` (no migration files exist yet).

## Milestone 0.3 — Application layer — 2026-07-13
- Added `PettyCash.Application` project: 9 commands, 2 queries, repository/external-service interfaces, DTOs, `ISettlementAuthorizationPolicy`, FluentValidation validators, Application exception hierarchy, DI registration.
- Added `PettyCash.Application.Tests`: in-memory-fake-based test suite, 93 tests total (combined with Domain).
- Post-approval fix: corrected a test-code compile error (target-typed `new()` against an interface) — no production code changed.

## Milestone 0.2 — Domain layer — 2026-07-13
- Added `PettyCash.Domain` project: `Settlement` aggregate, `SettlementLine`, value objects, state machine, domain events. 37 unit tests.
- Self-review correction: `Rejected` modeled as its own persisted status with an explicit `ReopenForEdit` transition, not an auto-revert to Draft (D-014).

## Milestone 0.1 / 0.1.1 — Architecture — 2026-07-13
- Established Clean Architecture layering, domain model, DB schema, state machine, RBAC model, API surface, UI navigation map.
- Resolved a stack conflict with the pre-architecture `CONTEXT.md` scaffold (FastAPI/Postgres-as-primary) — confirmed .NET backend, Postgres dev-only / SharePoint production System of Record.
- Created `TECH_STACK.md` as the authoritative Locked/Flexible technology record; demoted `CONTEXT.md` to historical notes.

## Milestone 0 — Requirements & risk analysis — 2026-07-13
- Initial functional/non-functional requirements, risk lists, and stack recommendation.
