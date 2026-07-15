# Petty Cash Settlement System

Replaces the paper petty-cash settlement form with a digital app: spender submits a
settlement → manager approves → an unposted journal is created in D365FO → AP checks
and posts. Full business context: `docs/Developer-Guide.docx`.

Authoritative project documentation, in order of authority:
1. `docs/DECISIONS.md` — decision log; source of truth for what's Locked vs Flexible
2. `TECH_STACK.md` — approved technologies, versions, Locked/Flexible status
3. `ARCHITECTURE.md` — living architecture: layers, domain model, schema, API surface
4. `docs/ASSUMPTIONS.md` — open items and risk levels
5. `docs/TODO.md` — milestone progress
6. `CHANGELOG.md` — human-readable summary of what changed, sprint by sprint

## Solution layout

```
backend/    ASP.NET Core 9 solution (Domain / Application / Infrastructure / Api), Clean Architecture
database/   Local development PostgreSQL (docker-compose) — see database/README.md
frontend/   React + TypeScript SPA (not yet started)
docs/       Decision log, assumptions register, milestone tracking, business source doc
```

## Running locally

1. Start the local development database (see `database/README.md` for full detail):
   ```bash
   cd database
   docker compose up -d
   ```
2. Apply migrations (first run only, or after a fresh `docker compose down -v`):
   ```bash
   cd backend
   dotnet ef database update --project src/PettyCash.Infrastructure --startup-project src/PettyCash.Api
   ```
3. Run the API:
   ```bash
   cd backend
   dotnet run --project src/PettyCash.Api
   ```
4. Verify:
   - `http://localhost:5080/health` → `Healthy`
   - `http://localhost:5080/openapi/v1.json` → OpenAPI document (no Swagger UI is wired up yet — see `docs/DECISIONS.md` D-036)

## Tests

```bash
cd backend
dotnet test
```

`PettyCash.Infrastructure.Tests` uses Testcontainers and needs Docker running — it spins up
its own throwaway Postgres container, separate from the persistent one in `database/`.
