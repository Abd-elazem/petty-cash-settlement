# TECH_STACK.md — Approved Technology Stack

Legend: **Locked** = will not be revisited without your explicit request or a critical blocker that makes it impossible to proceed. **Flexible** = decided provisionally, open to revision without special justification.

---

## Backend
**ASP.NET Core (.NET 9)** — **Locked**
- Reason: first-party parity with Microsoft Graph SDK, Entra External ID libraries, and Azure Key Vault; CANEX IT is Microsoft-centric; long-term support alignment with the rest of the Microsoft toolchain this project depends on (SharePoint, D365FO, Power Automate).
- Alternatives considered: FastAPI (Python) — was in the pre-architecture CONTEXT.md scaffold, rejected 2026-07-13 (see DECISIONS.md D-001, D-011).

## Frontend
**React + TypeScript** — **Locked**
- Reason: mobile-first requirement (Guide §5.3), wide component ecosystem, strong typing story with TS, team direction already set in early scaffolding.
- Alternatives considered: none seriously contested.

## Architecture Style
- **Clean Architecture** (Domain / Application / Infrastructure / Api) — **Locked**
- **Dependency Injection** — **Locked** (standard ASP.NET Core built-in container; no third-party DI framework needed at this scale)
- **REST API** — **Locked**. Reason: must support future Android/iOS/Desktop clients consuming the *same* backend API without backend changes. GraphQL/gRPC were considered and rejected — no evidence of a client need for flexible querying, and REST is the simpler fit alongside the Power Automate premium connector patterns already in use for D365FO.
- **Mobile-first responsive web app** — **Locked**, mandated by Guide §5.3 ("must work well on mobile phones").

## Persistence
- **PostgreSQL** (development-only implementation of `ISettlementRepository`) — **Locked**. Provider: Npgsql + EF Core (exact version pinned at Milestone 0.4, not an architectural decision).
- **SharePoint** (production System of Record) — **Locked**. Reason: explicit business requirement, Guide §5.4–5.5 — not an engineering preference, not open to revision on technical grounds alone.
- Both implementations satisfy the same repository interface; Domain/Application never reference either directly — **Locked** (DECISIONS.md D-002).
- **Entity Framework Core 9.0.4** — **Locked**. Introduced at Sprint 4. Reason: standard .NET ORM, first-party Npgsql provider, owned-entity support fits the Settlement/SettlementLine aggregate boundary directly (D-022) without hand-written SQL mapping. Version pinned to exactly 9.0.4 (not just "9") after a NU1605 package-downgrade error — see D-028.
- **Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4** — **Locked**. The Postgres provider for EF Core; also supplies native `xmin`-based optimistic concurrency (D-023), avoiding a hand-rolled RowVersion column. Must move in lockstep with Microsoft.EntityFrameworkCore — see D-028.
- Alternatives considered for persistence tooling: Dapper (rejected — more manual mapping for the owned-collection aggregate shape, no material benefit at this project's query complexity) and raw ADO.NET (rejected — pure overhead for a dev-only adapter).

## Identity
- **JWT (interim) → Entra External ID (target)** — **Flexible**, explicitly flagged for reconsideration (see ASSUMPTIONS.md A-014). Not locked because building a JWT layer now creates throwaway work if Entra is adopted immediately instead — this needs one more explicit decision from you before either option is locked.

## Microsoft Integration
- **Microsoft Graph SDK** (SharePoint access) — **Locked**, mandated by Guide.
- **Power Automate** (approval flow, F&O journal creation) — **Locked**, mandated by Guide §6.
- **Dynamics 365 F&O connector** (premium, via Power Automate) — **Locked**, mandated by Guide §6.2.
- **Azure Key Vault** (secrets) — **Locked**, mandated by Guide §5.4.

## Testing
- **xUnit** for backend unit/contract tests — **Locked**. Standard framework for .NET, no reason to deviate at this stage.
- **Testcontainers.PostgreSql** — **Locked**. Introduced at Sprint 4 for `PettyCash.Infrastructure.Tests`, per explicit instruction not to mock where a real Postgres test database gives better confidence. Spins up a disposable, real Postgres container per test run. Requires Docker (or a compatible container runtime) on the machine running the tests — see ASSUMPTIONS.md A-017.
  Alternatives considered: a shared, manually-provisioned test database — rejected (state leaks between runs, environment drift, harder to run in CI); mocking `DbContext`/`DbSet` — explicitly rejected by instruction and on principle (an ORM mapping bug is exactly what a mock can't catch).

## Validation
- **FluentValidation** (+ FluentValidation.DependencyInjectionExtensions) — **Locked**. Introduced at Milestone 0.3 for command validation. Reasoning: fluent, testable rule syntax; keeps validation logic out of command/handler bodies; assembly-scanning DI registration means new validators are picked up automatically. Alternatives considered: hand-rolled validation (rejected — more boilerplate, no consistent error-aggregation shape) and Data Annotations (rejected — weaker for cross-field rules like the AddLineCommand car-plate/odometer pairing check).

## Versions
Exact NuGet package versions for `PettyCash.Domain`/`.Application`/`.Infrastructure` and their test projects are centrally managed in `backend/Directory.Packages.props` (Central Package Management, introduced at Sprint 4 — D-028) — individual `.csproj` files reference packages by name only, with no per-project `Version` attribute. Frontend (`package.json`, not yet created) will track its own versions separately when the React app exists.

**Currently locked at exactly 9.0.4 as one matched set** (must not be bumped independently of each other): `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.DependencyInjection.Abstractions`.

**Intentionally versioned independently** (different ecosystems, no shared transitive dependency on the 9.0.4 EF Core line, so no reason to force alignment): `FluentValidation`/`FluentValidation.DependencyInjectionExtensions` (11.10.0), `Microsoft.NET.Test.Sdk`/`xunit`/`xunit.runner.visualstudio` (test tooling), `Testcontainers.PostgreSql` (3.10.0).
