# Local Development Database

This folder provisions the **local-only** PostgreSQL instance used by `dotnet run` /
`dotnet watch` while developing `PettyCash.Api`. It is not used in production — production
uses SharePoint as the System of Record (see `../ARCHITECTURE.md` §2, `../TECH_STACK.md`).

It is also unrelated to `PettyCash.Infrastructure.Tests`, which uses **Testcontainers** to spin
up its own throwaway Postgres container per test run — that one needs Docker too, but never
touches this named volume or this persistent container.

## Prerequisites

- Docker Desktop (or any Docker/Testcontainers-compatible engine) running locally.

## Start the database

From this folder (`database/`):

```bash
docker compose up -d
```

This starts a Postgres 16 container named `pettycash-postgres-dev`, listening on
`localhost:5432`, with database `pettycash_dev` / user `postgres` / password `postgres` —
matching `backend/src/PettyCash.Api/appsettings.json`'s `ConnectionStrings:PettyCashDev`
exactly. Data persists in the named Docker volume `pettycash_pgdata`.

Check it's healthy:

```bash
docker compose ps
```

## Stop the database (keeps data)

```bash
docker compose stop
```

Restart later with `docker compose start` (or `up -d` again) — the data in the
`pettycash_pgdata` volume is untouched.

## Stop and remove the container (keeps data)

```bash
docker compose down
```

The named volume survives this — the next `docker compose up -d` reconnects to the same data.

## Remove everything, including all data

```bash
docker compose down -v
```

This deletes the `pettycash_pgdata` volume permanently. Use this if you want a completely
fresh database (e.g. to re-test migrations from scratch).

## After starting for the first time

The database itself will be empty (no tables) until EF Core migrations are applied. From
`backend/`:

```bash
dotnet ef database update --project src/PettyCash.Infrastructure --startup-project src/PettyCash.Api
```

This applies the `InitialCreate` migration already on disk under
`backend/src/PettyCash.Infrastructure/Migrations/`.

## Verifying the fix

Once the container is running and migrations are applied, `dotnet run` from
`backend/src/PettyCash.Api` should report `/health` as `Healthy` (Postgres reachable).
