# AI_HANDOFF.md — Petty Cash Settlement System

**Purpose:** This is the entry-point document for any AI session (or new developer) resuming work on this project. Read this file first, then follow the pointers below. Do not re-derive architecture from scratch — it already exists and is documented.

_Last updated: 2026-07-15 (Sprint 5.1 closure, D-037 re-verification)._

---

## 1. Project Summary

CANEX Aluminum's petty cash settlement process is currently paper-based: spenders fill a paper form, staple receipts, get a manager's signature, and hand it to an AP accountant who re-types every line into Dynamics 365 Finance & Operations (D365FO) as a general journal.

This project replaces that with a digital flow: **digital form → SharePoint → approval → ready (unposted) journal in D365FO → AP checks and posts.** The system never posts a journal itself — AP always posts manually after checking physical receipts.

Two phases (per `docs/Developer-Guide.docx`):
- **Phase 1:** entry app + SharePoint storage (spender-facing form, photo capture).
- **Phase 2:** Power Automate approval flow + automatic unposted-journal creation in D365FO.

Business source of truth: `docs/Developer-Guide.docx`. Do not treat this handoff or any other engineering doc as a replacement for it — if in doubt about a business rule, the Guide wins.

---

## 2. Current Branch

`develop`

---

## 3. Latest Release Tag

`v0.5.2`

(Confirm against `git tag` / `git log` at the start of any session — this file is a snapshot, not a live query.)

---

## 4. Current Verified State

**Sprint 5.1 — API Foundation: CLOSED (2026-07-15), fully verified.**

All verification steps confirmed by the client, including the D-037 fix:
- `docker compose up -d` ✅
- `dotnet restore` ✅
- `dotnet build` ✅
- `dotnet test` ✅ (112 passing)
- `dotnet run` ✅
- `/health` ✅ Healthy
- OpenAPI (`/openapi/v1.json`) ✅
- `dotnet ef database update` ✅ (`InitialCreate` applied)

**No open items remain for Sprint 5.1.** D-037 ("Api project needs `Microsoft.EntityFrameworkCore.Design` for `dotnet ef`") is fully decided, applied, **and re-verified** — see `docs/DECISIONS.md` correction below.

Layers implemented and passing:
- `PettyCash.Domain` — aggregate, value objects, state machine, domain events. 37 tests.
- `PettyCash.Application` — 9 commands, 2 queries, interfaces, DTOs, authorization policy, validators. 93 combined tests (Domain+Application).
- `PettyCash.Infrastructure` — Postgres dev adapter (repositories, EF configurations, migrations). Tested via Testcontainers, no mocks.
- `PettyCash.Api` — minimal hosting, exception handling, versioning foundation, OpenAPI, health check. No business endpoints yet.
- `database/docker-compose.yml` — local Postgres 16 matching `PettyCashDev` connection string.

Total: 112 automated tests passing, confirmed on the client's machine (not just in an isolated session).

---

## 5. Frozen Layers

A "frozen" layer may only be touched under the five conditions logged against each prior exception (see `docs/DECISIONS.md` D-021 for the template of what "properly justified" looks like). Frozen does **not** mean "never changed" — it means changes require the explicit justification protocol in `PROJECT_RULES.md` §3, not casual editing.

| Layer | Status | Frozen since | Notes |
|---|---|---|---|
| `PettyCash.Domain` | Frozen (except bug fixes) | Milestone 0.2 (2026-07-13) | One approved exception since freezing: D-021 (EF materialization support — parameterless ctor + settable `LineId`). |
| `PettyCash.Application` | Frozen (except bug fixes) | Milestone 0.3 (2026-07-13) | No exceptions taken yet. |
| `PettyCash.Infrastructure` (Postgres adapter) | Implementation complete, not formally "frozen" | — | Still expected to gain the SharePoint adapter (Milestone 0.5) as a sibling, not a replacement. |
| `PettyCash.Api` (foundation) | Sprint 5.1 scope closed | 2026-07-15 | No business endpoints yet — those are new scope (Milestone 0.6), not a frozen-layer exception. |

If a future task appears to require changing Domain or Application behavior (not just adding to Infrastructure/Api), **stop and flag it explicitly** rather than assuming it's allowed.

---

## 6. Current Roadmap

Per `docs/TODO.md`, in order:

1. ~~Milestone 0 — Requirements & risk analysis~~ Done
2. ~~Milestone 0.1 / 0.1.1 — Core architecture + TECH_STACK.md~~ Done
3. ~~Milestone 0.2 — Domain layer~~ Done, frozen
4. ~~Milestone 0.3 — Application layer~~ Done, frozen
5. ~~Sprint 4 — Infrastructure (Postgres dev adapter)~~ Done
6. ~~Sprint 5.1 — API Foundation~~ **Done, closed, re-verified**
7. **Milestone 0.5 — SharePoint + Entra adapters (production)** — not started
8. **Milestone 0.6 — API business endpoints + minimal React UI** — not started
9. Backlog (post-MVP): duplicate/anomaly checks, Power BI balance report, budget validation (A-006), approver delegation (A-007)

---

## 7. Current Task

**None in progress.** Sprint 5.1 is closed. Awaiting client direction on whether to proceed to Milestone 0.5 (SharePoint + Entra) or Milestone 0.6 (business endpoints + UI) next.

_(Update this section the moment a new task starts — see §11.)_

---

## 8. Outstanding Architectural Decisions

These need an explicit client decision before (or during) the milestone that depends on them — see `docs/ASSUMPTIONS.md` for full detail:

- **A-005** — Single currency (EGP) for MVP, no multi-currency. Needs confirmation.
- **A-006** — Budget / over-settlement validation — not specified by the Guide, needs business sign-off before Milestone 0.5/0.6 if in scope.
- **A-007** — Approver delegation (manager on leave, etc.) — same, needs business sign-off.
- **A-014** — Identity: JWT now vs. Entra External ID immediately. `TECH_STACK.md` marks this **Flexible**, explicitly flagged for reconsideration now that the .NET stack is confirmed. Needed before Milestone 0.6 auth work starts.
- **A-016** — Whether `ReceiptPhoto` belongs inside the `Settlement` aggregate's consistency boundary. Blocks photo-upload command wiring (D-020).
- **D-006** (Power Automate calls back into API rather than writing SharePoint directly) — logged as "Recommended," needs validation with the Phase 2 flow owner.

Do not silently resolve any of these by picking an option — surface them to the client per `PROJECT_RULES.md`'s ambiguity rule.

---

## 9. Verification Checklist

Before marking **any** sprint/milestone closed, all of the following must be confirmed — by the client, on the client's machine, not assumed from a clean build in an isolated session:

- [ ] `docker compose up -d` (from `database/`) — Postgres container healthy
- [ ] `dotnet restore` — no NU1605 or other restore errors
- [ ] `dotnet build` — zero errors (warnings-as-errors scope per D-032: `PettyCash.Api` only, for now)
- [ ] `dotnet test` — all tests passing, count explicitly stated (currently 112)
- [ ] `dotnet run` — starts without DI validation failures
- [ ] `GET /health` — returns Healthy (real Postgres connectivity, not just process liveness — D-034)
- [ ] OpenAPI document reachable (`/openapi/v1.json`) — Development environment only
- [ ] `dotnet ef database update` — migrations apply cleanly against a fresh or existing volume
- [ ] `backend/PettyCash.sln` includes every project created this session (convention, `ARCHITECTURE.md` §8)
- [ ] `CHANGELOG.md`, `docs/TODO.md`, `docs/DECISIONS.md`, and (if relevant) `ARCHITECTURE.md` updated to match what was actually verified — not what was merely written

A milestone/sprint is **not** closed until every applicable box above is checked by the client and recorded in `CHANGELOG.md`/`docs/TODO.md`.

---

## 10. Git Workflow

- Working branch: `develop`.
- Release tags follow `vMAJOR.MINOR.PATCH` (current: `v0.5.2`).
- One `.csproj`/project is added to `backend/PettyCash.sln` in the same change that creates it (no separate step) — `ARCHITECTURE.md` §8.
- Commit granularity: small, reviewable milestones per the project's standing instruction (see root project instructions) — avoid bundling unrelated layers into one commit.
- Do not rewrite history on `develop`. Tag a release only after the client has completed the Verification Checklist (§9) for that unit of work.
- This AI session has no direct git execution access on the client's machine (confirmed — migrations, builds, and tests are hand-authored/reasoned about here and verified by the client separately). Do not claim a commit or tag was made unless the client performed it.

---

## 11. Resume Instructions

When resuming this project in a new AI session:

1. Read this file (`AI_HANDOFF.md`) in full.
2. Read `PROJECT_RULES.md` in full — it governs *how* to work, this file describes *where things stand*.
3. Cross-check §4 (Current Verified State) against `CHANGELOG.md`'s most recent entry and `docs/TODO.md`'s most recent status line — if they disagree, treat `CHANGELOG.md`/`docs/TODO.md` as more current and flag the discrepancy to the client rather than silently trusting this file.
4. Check §7 (Current Task) — if empty, ask the client which roadmap item (§6) to start, per the "ask for clarification on ambiguous requirements" rule. Do not pick one unprompted.
5. Before writing any code, check §5 (Frozen Layers) — if the task touches Domain or Application, stop and confirm the change is genuinely required and would satisfy the frozen-layer exception protocol (`PROJECT_RULES.md` §3) before proceeding.
6. Review `docs/ASSUMPTIONS.md` and §8 (Outstanding Architectural Decisions) for anything the current task depends on.

---

## 12. Stop Conditions

Stop and wait for explicit client input rather than proceeding, whenever:

- A task appears to require changing a frozen layer (Domain/Application) for a reason other than a genuine technical limitation with all five D-021-style conditions satisfied.
- A requirement is ambiguous or not covered by `docs/Developer-Guide.docx`, `ARCHITECTURE.md`, `TECH_STACK.md`, or `docs/DECISIONS.md`.
- An outstanding architectural decision (§8) blocks the task at hand.
- A verification step (§9) cannot be run in this session (e.g., no execution access to the client's machine) — state this explicitly rather than assuming success.
- A change would affect more than one milestone's worth of scope at once (e.g., "while I was in there I also...") — flag it as a separate, explicitly reviewable unit instead of folding it in.
- Any new business rule is discovered that is not in the Developer Guide — do not invent one; ask.

---

## 13. MANDATORY — Update After Every Completed Vertical Slice

**This section must be updated every time a vertical slice (a complete, independently verifiable unit of work — see `PROJECT_RULES.md` §4) is completed and verified. This is not optional and is not deferred to "later cleanup."**

On completion of a vertical slice, update in the same turn:
- §3 (Latest Release Tag) — if a new tag was cut.
- §4 (Current Verified State) — what is now built and verified, matching what `CHANGELOG.md`/`docs/TODO.md` say.
- §5 (Frozen Layers) — if a layer's freeze status changed, or a new frozen-layer exception was taken.
- §6 (Current Roadmap) — check off / update the completed item.
- §7 (Current Task) — set to the next task, or explicitly "None in progress — awaiting client direction" if nothing is queued.
- §8 (Outstanding Architectural Decisions) — remove resolved items, add newly discovered ones.

**Change log for this section itself** (append one line per update, do not delete history):

- 2026-07-15 — Sprint 5.1 closed and fully re-verified including D-037; document created.
