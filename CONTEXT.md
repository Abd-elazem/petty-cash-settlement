> **Historical note (2026-07-13):** This file originally held exploratory technology notes written before the architecture phase (FastAPI, Postgres-as-primary, JWT-only). Those notes are **superseded** and this file is **no longer authoritative**. Do not read it for current decisions — it's kept only as a historical record of the pre-architecture scaffold.
>
> Authoritative documentation, in order of authority:
> 1. `DECISIONS.md` — decision log; source of truth for what's Locked vs Flexible
> 2. `TECH_STACK.md` — approved technologies, versions, Locked/Flexible status
> 3. `ARCHITECTURE.md` — living architecture: layers, domain model, schema, API surface
> 4. `ASSUMPTIONS.md` — open items and risk levels
> 5. `docs/TODO.md` — milestone progress
>
> This file should not be edited further except to append additional historical notes.

---

## Original exploratory notes (superseded, kept for history)

Backend: FastAPI
Frontend: React + TypeScript
Architecture: Clean Architecture
Auth: JWT (temporary), Entra later
Database: PostgreSQL during development
External services are mocked until integration.
