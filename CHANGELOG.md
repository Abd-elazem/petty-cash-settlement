# CHANGELOG

Human-readable summary of what changed, sprint by sprint. `docs/DECISIONS.md` is the authoritative record of *why*; this file is *what*, briefly.

## Sprint 5.1 — API Foundation — 2026-07-14
- Added `PettyCash.Api` project: minimal-hosting-model `Program.cs`, `GlobalExceptionHandler` (ProblemDetails, RFC 9457), API versioning foundation (Asp.Versioning.Http 8.1.0), built-in OpenAPI (`Microsoft.AspNetCore.OpenApi`), structured JSON console logging, Postgres-backed health check at `/health`.
- References only Application and Infrastructure — verified no Domain project reference; exception handler matches Domain exceptions by namespace string to avoid even a transitive-but-direct type dependency.
- No business endpoints, no authentication, no SharePoint/Entra/Graph/Power Automate/D365FO — explicitly out of scope this sprint.
- `backend/PettyCash.sln` and `Directory.Packages.props` updated.
- **Follow-up fix (same sprint):** local verification found `dotnet run` failing — `ICurrentUserContext` was correctly unregistered (D-025) but nothing satisfied it yet, tripping ASP.NET Core's Development-only DI validation. Added `DevelopmentCurrentUserContext` (`PettyCash.Api/Development/`), a fixed placeholder implementation registered only under `IsDevelopment()` (D-035). No Domain or Application changes. Pending final client-side re-verification (restore/build/test/run, `/health`, OpenAPI) before Sprint 5.1 is marked closed.
- **Runtime verification findings (2026-07-15):** `/swagger` returning 404 was confirmed as expected — no Swagger UI is registered anywhere (D-036); `/openapi/v1.json` is the only OpenAPI artifact this sprint. `/health` returning Unhealthy was root-caused (via a temporary Development-only `/health/detail` diagnostic endpoint, since removed) to no PostgreSQL server running on `127.0.0.1:5432` — there was no docker-compose file, provisioning script, or any other way to get a persistent Postgres instance running locally. This was a missing-infrastructure gap, not an application defect; no application code or health-check behavior was changed.
- **Local development infrastructure added:** `database/docker-compose.yml` — PostgreSQL 16, matching `appsettings.json`'s `PettyCashDev` connection string exactly (`pettycash_dev` / `postgres` / `postgres`, port 5432), with a persistent named volume (`pettycash_pgdata`) and a `pg_isready` healthcheck. `database/README.md` documents start/stop/remove and applying the existing `InitialCreate` migration. Root `README.md` created (previously empty) with full local run instructions.
- **Client verification round 3 (2026-07-15):** docker compose, restore, build, test (112 passing), run, `/health` Healthy, and OpenAPI all confirmed. `dotnet ef database update` failed — startup project `PettyCash.Api` didn't reference `Microsoft.EntityFrameworkCore.Design`.
- **Tooling fix (2026-07-15):** added an explicit, design-time-only `Microsoft.EntityFrameworkCore.Design` `PackageReference` to `PettyCash.Api.csproj` (`PrivateAssets="all"`, matching Infrastructure's existing reference pattern). Infrastructure's own reference doesn't flow to Api via `ProjectReference` because of that same `PrivateAssets="all"`, and `dotnet ef` needs it on the *startup* project specifically. No new package version; no application code changed. Details: DECISIONS.md D-037. Awaiting client re-run to confirm.

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
