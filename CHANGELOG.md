# CHANGELOG

Human-readable summary of what changed, sprint by sprint. `docs/DECISIONS.md` is the authoritative record of *why*; this file is *what*, briefly.

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
