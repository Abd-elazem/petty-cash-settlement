# AI_HANDOFF.md â€” Petty Cash Settlement System

**Purpose:** This is the entry-point document for any AI session (or new developer) resuming work on this project. Read this file first, then follow the pointers below. Do not re-derive architecture from scratch â€” it already exists and is documented.

_Last updated: 2026-07-21 (VS12 Update Settlement Header — backend + frontend + integration tests implemented; awaiting client verification)._
Vertical Slice Roadmap

âœ“ VS1 - Create Draft Settlement
âœ“ VS2 - Add Settlement Line
âœ“ VS3 - Submit Settlement
âœ“ VS4 - Get Settlement Detail (verified 2026-07-17)
âœ“ VS5 - List My Settlements (verified 2026-07-17)
âœ“ VS6 - Update Settlement Line (verified 2026-07-17)
âœ“ VS7 - Remove Settlement Line (verified 2026-07-17)
âœ“ VS8 - Approve Settlement (verified 2026-07-17)
âœ“ VS9 - Reject Settlement (verified 2026-07-17)
âœ“ VS10 - Reopen Settlement (verified 2026-07-17)
âœ“ VS11 - Record Journal Entry (verified 2026-07-17)
---

## 1. Project Summary

CANEX Aluminum's petty cash settlement process is currently paper-based: spenders fill a paper form, staple receipts, get a manager's signature, and hand it to an AP accountant who re-types every line into Dynamics 365 Finance & Operations (D365FO) as a general journal.

This project replaces that with a digital flow: **digital form â†’ SharePoint â†’ approval â†’ ready (unposted) journal in D365FO â†’ AP checks and posts.** The system never posts a journal itself â€” AP always posts manually after checking physical receipts.

Two phases (per `docs/Developer-Guide.docx`):
- **Phase 1:** entry app + SharePoint storage (spender-facing form, photo capture).
- **Phase 2:** Power Automate approval flow + automatic unposted-journal creation in D365FO.

Business source of truth: `docs/Developer-Guide.docx`. Do not treat this handoff or any other engineering doc as a replacement for it â€” if in doubt about a business rule, the Guide wins.

---

## 2. Current Branch

`develop`

---

## 3. Latest Release Tag

`v0.5.2`

(Confirm against `git tag` / `git log` at the start of any session â€” this file is a snapshot, not a live query.)

---

## 4. Current Verified State

**Backend MVP through Vertical Slice 11: CLOSED (2026-07-17). Implemented, tested, and client-verified.**
`SettlementsEndpoints.cs` includes VS1â€“VS11 endpoints (Create, Add Line, Submit, Get Detail, List Mine, Update Line, Remove Line, Approve, Reject, Reopen, Record Journal). All endpoint tests are present in `PettyCash.Api.Tests/Settlements` and aligned with the implemented routes and handlers. Verification status is client-confirmed for VS1â€“VS11.

**Authentication/authorization foundation: implemented.**
`Program.cs` now applies authentication + authorization middleware and registers `AddAuthenticationFoundation(...)`. Development mode uses `DevelopmentAuthenticationHandler`; Entra-enabled mode uses JWT bearer validation + claims-based current-user resolution. Endpoint policies are enforced in `SettlementsEndpoints.cs` (spender role, approve/reject composite policy, system-only journal callback, and view policy).

**Persistence parity + health hardening: implemented.**
Contract parity tests run against both Postgres and SharePoint repository implementations (`SettlementRepositoryContractParityTests.cs`). `/health` now follows the active backend selection (`SharePoint:Enabled`): SharePoint mode probes Graph connectivity (`SharePointGraphHealthCheck`), Postgres mode probes `PettyCashDev` via Npgsql.

**Manager Workflow frontend: verification pass complete (2026-07-19); awaiting client build-verification.**
Original implementation (see previous entry) plus fixes from the verification pass:
- **Role-based nav guard (fix):** `AppLayout` was showing the Manager Inbox nav entry to all authenticated users. Fixed: `AuthUser` extended with `isManager: boolean` (read from `account.idTokenClaims.roles`, the MSAL-cached ID token claim â€” no new scope or API call). Nav entry is now conditionally rendered only when `user.isManager === true`.
- **Route protection (fix):** Manager routes (`/manager-inbox`, `/manager-inbox/:requestId`) had no role guard â€” any authenticated user could navigate to them directly. Fixed: added `ManagerRoute` component (`src/auth/ManagerRoute.tsx`) that redirects non-managers to `/unauthorized`; both routes now wrapped in `<ManagerRoute>` in `router.tsx`.
- **Mutation hook pattern (fix):** `useApproveSettlement` and `useRejectSettlement` used `isApproving`/`isRejecting` state in the `useCallback` dependency array for duplicate-call prevention, causing the callback reference to be recreated on each state change. Aligned with the established codebase pattern (ref-based guard, empty dep array) matching `useSubmitSettlement` and all other existing mutation hooks.
- **Verification points confirmed correct (no change needed):** reject comment validation exactly matches `RejectSettlementCommandValidator` (`NotEmpty` + `MaximumLength(1000)`); approve/reject page stays open after decision with state from backend response; action buttons disappear automatically because `isSubmitted` is derived from backend-returned status; returning to inbox re-fetches from backend so processed settlements are gone; empty-inbox state renders correctly from the `settlements.length === 0` check.
- **Files added:** `src/auth/ManagerRoute.tsx`.
- **Files modified:** `src/auth/AuthContext.tsx` (`isManager` field on `AuthUser`), `src/layout/AppLayout.tsx` (conditional nav), `src/router.tsx` (`ManagerRoute` wrapper), `src/features/settlements/hooks/useApproveSettlement.ts` (ref pattern), `src/features/settlements/hooks/useRejectSettlement.ts` (ref pattern). Zero backend changes.

**Manager Workflow frontend: implemented (2026-07-19); see above for verification-pass fixes.**
New files: `useApproverInbox.ts`, `useApproveSettlement.ts`, `useRejectSettlement.ts`, `ManagerInboxTable.tsx`, `ManagerInboxCard.tsx`, `RejectSettlementDialog.tsx`, `ManagerInboxPage.tsx`, `ManagerSettlementDetailPage.tsx`. Modified: `settlementsClient.ts` (`getInbox` added), `router.tsx` (`/manager-inbox`, `/manager-inbox/:requestId`), `AppLayout.tsx` (Manager Inbox nav entry), `styles.css` (manager action panel + button-danger color fix). Zero backend changes.

**Frontend Stabilization Sprint: completed and build-verified (2026-07-19).**
`frontend/` now includes completed authentication flow/shell foundations, My Settlements list/detail experiences, create-draft workflow foundations, and line add/edit/remove/submit workflow foundations:
- centralized `AuthProvider`/`AuthContext` for authentication state (authenticated/loading/error/user), with no duplicated UI auth logic;
- completed sign-in/sign-out flow via MSAL and context actions;
- session restoration on refresh through active-account resolution from cached MSAL accounts;
- protected-route enforcement driven by auth context state;
- responsive app shell with sidebar + top app bar + profile menu + sign-out action;
- navigation entries for Dashboard and My Settlements;
- global loading/error UI components used in auth/sign-in/layout flows;
- typed API layer preserved, with auth hardening updates (interaction-required token redirect path, response 401 logout cleanup, startup-auth initialization fallback UI, explicit active-account cleanup on logout);
- My Settlements page implemented using existing API client endpoint (`GET /api/v1/settlements/mine`) with loading/empty/error+retry states, client-side search/status filtering, newest-first sorting, refresh action, status badges, responsive desktop table + mobile cards, and settlement click-through to detail placeholder route.
- Settlement Detail page implemented using existing API client endpoint (`GET /api/v1/settlements/{settlementId}`) with reusable `useSettlement` hook and UI components (`SettlementHeader`, `SettlementMetadata`, `SettlementLineTable`, `SettlementLineCard`), deep-link route support (`/my-settlements/:requestId`), loading/error/not-found states, back navigation, and responsive lines rendering.
- Contract verification/fixes in this batch: frontend `SettlementSummaryDto` aligned exactly with backend contract (removed unsupported `version` field); `SettlementDto` confirmed aligned; status mapping confirmed against backend enum values (`Draft`, `Submitted`, `Approved`, `Rejected`, `Journalled`, `Posted`); stable keys confirmed (`requestId`, `lineId`); list sorting remains based on backend date values.
- New Settlement workflow implemented using existing API endpoint (`POST /api/v1/settlements`) with reusable `SettlementForm`, `FormField`, `FormActions`, and `useCreateSettlement`; route `/settlements/new`; dashboard + My Settlements navigation actions; client-side UX validation aligned with backend constraints (required date, required purpose, max length 500); loading/save state and duplicate-submit prevention; API error display; and success redirect to `/my-settlements/{requestId}`.
- Add Settlement Line workflow implemented using existing API endpoint (`POST /api/v1/settlements/{settlementId}/lines`) with reusable `SettlementLineForm`, `SettlementLineFields`, `SettlementLineActions`, and `useAddSettlementLine`; Draft-only Add Line controls in Settlement Detail; dynamic mileage fields shown only for mileage-required category input (`FUEL`); client-side UX validation aligned with backend constraints (required category, gross amount > 0, notes <= 1000, and car plate/odometer pairing for mileage-required category); duplicate-submit prevention; API error display; and immediate line/total updates from backend mutation response.
- Update/Remove Settlement Line workflows implemented using existing API endpoints (`PUT /api/v1/settlements/{settlementId}/lines/{lineId}`, `DELETE /api/v1/settlements/{settlementId}/lines/{lineId}`) with Draft-only per-line actions, shared Add/Edit `SettlementLineForm` reuse (no duplicated UI), `DeleteConfirmationDialog`, `useUpdateSettlementLine`, and `useRemoveSettlementLine`; prefilled edit values; loading and duplicate-operation prevention; API/validation feedback; and settlement replacement from backend responses after update/delete (no local total recalculation).
- Submit Settlement workflow implemented using existing API endpoint (`POST /api/v1/settlements/{settlementId}/submit`) with `useSubmitSettlement` and `SubmitSettlementDialog`; Draft + at-least-one-line gated submit action; confirmation dialog; loading and duplicate-submit prevention; backend/API error display; and settlement replacement from backend response on success so status/totals/read-only transition are backend-driven.
### Stabilization Sprint Summary
- Scope: reliability/consistency/maintainability pass only; no new business features introduced.
- Refactors performed:
  - Centralized duplicated API error parsing into `frontend/src/api/apiErrorMessage.ts` and reused it across create/add/update/remove/submit hooks.
  - Reduced duplicate dialog implementations by introducing shared `ConfirmationDialog` and reusing it in `DeleteConfirmationDialog` and `SubmitSettlementDialog`.
  - Added accessibility hardening to confirmation dialogs: keyboard trap within dialog, Escape-to-close (when not pending), initial focus placement, and focus return to triggering element on close.
  - Added abort-signal support to list/detail client calls (`settlementsClient.getMine`, `settlementsClient.getById`) and unmount-safe cancellation handling in `useMySettlements` and `useSettlement`.
- Issues found and fixed:
  - Duplicate error parsing logic in mutation hooks (fixed via shared helper).
  - Duplicate dialog structure/behavior (fixed via shared dialog component).
  - Missing dialog focus management and focus return (fixed).
  - No request cancellation on auto-fetch unmount paths for list/detail hooks (fixed with AbortController wiring and canceled-request guards).
- Remaining technical debt:
  - SettlementDetail page still owns substantial line-form orchestration logic; can be split further into dedicated hooks/components when manager workflow starts.
  - Mileage-required behavior remains temporary frontend inference from `categoryCode === \"FUEL\"` until backend exposes explicit category metadata.
  - Bundle size warning (>500 kB chunk) remains; defer optimization until feature set stabilizes further to avoid premature code-splitting churn.
### Contract Verification
- Endpoints used:
  - `POST /api/v1/settlements/{settlementId}/lines`
  - `PUT /api/v1/settlements/{settlementId}/lines/{lineId}`
  - `DELETE /api/v1/settlements/{settlementId}/lines/{lineId}`
  - `POST /api/v1/settlements/{settlementId}/submit` (no request body)
  - `GET /api/v1/settlements/{settlementId}` (read path still available for refresh/retry flows)
  - `GET /api/v1/settlements/mine`
  - `POST /api/v1/settlements`
- DTOs verified:
  - `CreateSettlementRequest` and `SettlementSummaryDto` remain aligned with backend usage.
  - `AddSettlementLineRequest` matches backend `AddSettlementLineRequest` record (categoryCode, grossAmount, isVat, notes, carPlate, odometerKm).
  - `UpdateSettlementLineRequest` matches backend `UpdateSettlementLineRequest` record (same shape as add).
  - Remove line uses route params only (no body), matching backend handler/endpoint contract.
  - Submit request matches backend contract as no-body request (route parameter only).
  - Response state remains `SettlementDto` from backend for create/add/update/delete/submit; frontend replaces state from backend response directly where mutation returns updated aggregate.
- Frontend/backend contract mismatches found:
  - No payload/endpoint mismatch found across implemented employee workflows (create/get-mine/get-detail/add/update/delete/submit).
  - Category mileage requirement metadata is still inferred in frontend from `categoryCode === \"FUEL\"`; this is a temporary UX-only inference until backend exposes explicit category metadata to the client.
Verification in this session: `npm.cmd run build` executed in `frontend/`; build completed successfully. No `lint` script is currently configured in `frontend/package.json`.
Runtime API verification note: attempted backend reachability check to `http://localhost:5080/health` failed (`Unable to connect to the remote server`), so live authenticated runtime regression verification could not be completed in this session for: My Settlements list, Settlement Detail, Create Draft, Add Line, Update Line, Delete Line, Submit Settlement, read-only transition after submit, and full page-to-page workflow navigation against the live API.

**Vertical Slices 8â€“11 â€” Manager + Journal Workflow batch: CLOSED (2026-07-17). Fully verified by the client.**
`POST /api/v1/settlements/{settlementId}/approve` (VS8), `POST /api/v1/settlements/{settlementId}/reject` (VS9), `POST /api/v1/settlements/{settlementId}/reopen` (VS10), `POST /api/v1/settlements/{settlementId}/journal` (VS11) â€” all in `SettlementsEndpoints.cs`, all reusing frozen Application-layer command handlers. Test files: `ApproveSettlementEndpointTests.cs` (5), `RejectSettlementEndpointTests.cs` (6), `ReopenSettlementEndpointTests.cs` (5), `RecordJournalEndpointTests.cs` (7), all using the shared `[Collection("Api")]` fixture and identity overrides in `ApiWebApplicationFactory`.
Client verification confirmed: implemented, tested, and verified on the client machine.

**Vertical Slices 4â€“7 â€” Employee Workflow batch: CLOSED (2026-07-17). Fully verified by the client.**
`GET /api/v1/settlements/{settlementId}` (VS4), `GET /api/v1/settlements/mine` (VS5), `PUT /api/v1/settlements/{settlementId}/lines/{lineId}` (VS6), `DELETE /api/v1/settlements/{settlementId}/lines/{lineId}` (VS7) â€” all in `SettlementsEndpoints.cs`, all reusing frozen Application-layer query/command handlers (Milestone 0.3). `UpdateSettlementLineRequest.cs` already on disk. Test files: `GetSettlementEndpointTests.cs` (3), `GetMySettlementsEndpointTests.cs` (3), `UpdateSettlementLineEndpointTests.cs` (6), `RemoveSettlementLineEndpointTests.cs` (4) â€” all using the shared `[Collection("Api")]` fixture. All code was found fully implemented in the repository at session start; documentation was the only gap. No Domain/Application/Infrastructure change.
Client verification confirmed: `docker compose up -d` âœ…, `dotnet restore` âœ…, `dotnet build` âœ…, `dotnet test` âœ… (all passing, includes VS4â€“7 tests), `dotnet run` âœ….

**Vertical Slice 3 â€” Submit Settlement: CLOSED (2026-07-16). Fully verified by the client.**
`POST /api/v1/settlements/{settlementId}/submit` (`PettyCash.Api/Endpoints/SettlementsEndpoints.cs`), reusing the existing, unchanged `SubmitSettlementCommand`/Handler/Validator. Returns 200 with the updated `SettlementDto`. `SubmitSettlementEndpointTests.cs` (4 tests: happy path Draft+lineâ†’Submitted, no-linesâ†’400, unknown IDâ†’404, double-submitâ†’400) was already present in the repository and already included in the verified 116-test count â€” no new production code or test file was required. Investigation confirmed by reading `SettlementsEndpoints.cs`, `SubmitSettlementCommand.cs`, `ApplicationServiceCollectionExtensions.cs`, and `GlobalExceptionHandler.cs`. See `CHANGELOG.md`'s Vertical Slice 3 entry for full detail.
Client verification confirmed: `docker compose up -d` âœ…, `dotnet restore` âœ…, `dotnet build` âœ…, `dotnet test` âœ… (116 passing).

**Vertical Slice 2 â€” Add Settlement Line: CLOSED (2026-07-16). Fully verified by the client.**
`POST /api/v1/settlements/{settlementId}/lines` (`PettyCash.Api/Endpoints/SettlementsEndpoints.cs` + `AddSettlementLineRequest.cs`), reusing the existing, unchanged `AddLineCommand`/Handler/Validator. Returns 200 with the updated `SettlementDto` (D-042). Post-verification defect found and fixed: `AddSettlementLineRequest.cs` reported created in an earlier turn but absent from disk â€” recreated and confirmed present via read-back before resubmitting (D-043). See `docs/DECISIONS.md` D-042/D-043 and `CHANGELOG.md`'s Vertical Slice 2 entry for full detail.
Client verification confirmed: `docker compose up -d` âœ…, `dotnet restore` âœ…, `dotnet build` âœ…, `dotnet test` âœ… (116 passing), `dotnet run --project src/PettyCash.Api` âœ….

**Vertical Slice 1 â€” Create Draft Settlement: CLOSED (2026-07-16). Fully verified by the client.**
`POST /api/v1/settlements` (`PettyCash.Api/Endpoints/SettlementsEndpoints.cs`), reusing the existing, unchanged `CreateDraftSettlementCommand`/Handler. `PettyCash.Api.Tests` added (Testcontainers-backed, no mocks, 4 integration tests). Post-implementation fixes applied and verified: two missing `using` directives in `ApiWebApplicationFactory.cs` (CS0246/CS1061) and an `AddInfrastructure()` eager connection-string capture bug that prevented the Testcontainers override from taking effect (D-041). See `docs/DECISIONS.md` D-038/D-039/D-040/D-041 and `CHANGELOG.md`'s Vertical Slice 1 entry for full detail.
Client verification confirmed: `docker compose up -d` âœ…, `dotnet restore` âœ…, `dotnet build` âœ…, `dotnet test` âœ… (all tests passing, including 4 new `PettyCash.Api.Tests`).

**Sprint 5.1 â€” API Foundation: CLOSED (2026-07-15), fully verified.**

All verification steps confirmed by the client, including the D-037 fix:
- `docker compose up -d` âœ…
- `dotnet restore` âœ…
- `dotnet build` âœ…
- `dotnet test` âœ… (112 passing)
- `dotnet run` âœ…
- `/health` âœ… Healthy
- OpenAPI (`/openapi/v1.json`) âœ…
- `dotnet ef database update` âœ… (`InitialCreate` applied)

**No open items remain for Sprint 5.1.** D-037 ("Api project needs `Microsoft.EntityFrameworkCore.Design` for `dotnet ef`") is fully decided, applied, **and re-verified** â€” see `docs/DECISIONS.md` correction below.

Layers implemented and passing:
- `PettyCash.Domain` â€” aggregate, value objects, state machine, domain events. 37 tests.
- `PettyCash.Application` â€” 9 commands, 2 queries, interfaces, DTOs, authorization policy, validators. 93 combined tests (Domain+Application).
- `PettyCash.Infrastructure` â€” Postgres dev adapter (repositories, EF configurations, migrations). Tested via Testcontainers, no mocks.
- `PettyCash.Api` â€” minimal hosting, exception handling, versioning foundation, OpenAPI, health check, and business endpoints through VS11.
- `database/docker-compose.yml` â€” local Postgres 16 matching `PettyCashDev` connection string.
- `frontend/` â€” React + TypeScript web client with MSAL auth context flow, protected routes, responsive shell, Dashboard/My Settlements navigation, and typed API client wiring.
Automated tests: client-verified passing status across the implemented backend scope (including VS1â€“VS11 endpoint coverage).

---

## 5. Frozen Layers

A "frozen" layer may only be touched under the five conditions logged against each prior exception (see `docs/DECISIONS.md` D-021 for the template of what "properly justified" looks like). Frozen does **not** mean "never changed" â€” it means changes require the explicit justification protocol in `PROJECT_RULES.md` آ§3, not casual editing.

| Layer | Status | Frozen since | Notes |
|---|---|---|---|
| `PettyCash.Domain` | Frozen (except bug fixes) | Milestone 0.2 (2026-07-13) | One approved exception since freezing: D-021 (EF materialization support â€” parameterless ctor + settable `LineId`). |
| `PettyCash.Application` | Frozen (except bug fixes) | Milestone 0.3 (2026-07-13) | No exceptions taken yet. |
| `PettyCash.Infrastructure` (Postgres adapter) | Implementation complete, not formally "frozen" | â€” | Still expected to gain a production SharePoint adapter as a sibling, not a replacement. |
| `PettyCash.Api` (foundation) | Sprint 5.1 scope closed | 2026-07-15 | Vertical Slice 1 added the first business endpoint on top of this foundation (not a frozen-layer exception â€” Api was never frozen, only Domain/Application are). |

If a future task appears to require changing Domain or Application behavior (not just adding to Infrastructure/Api), **stop and flag it explicitly** rather than assuming it's allowed.

---

## 6. Current Roadmap

Per `docs/TODO.md`, in order:

1. ~~Milestone 0 â€” Requirements & risk analysis~~ Done
2. ~~Milestone 0.1 / 0.1.1 â€” Core architecture + TECH_STACK.md~~ Done
3. ~~Milestone 0.2 â€” Domain layer~~ Done, frozen
4. ~~Milestone 0.3 â€” Application layer~~ Done, frozen
5. ~~Sprint 4 â€” Infrastructure (Postgres dev adapter)~~ Done
6. ~~Sprint 5.1 â€” API Foundation~~ **Done, closed, re-verified**
7. ~~Vertical Slice 1 â€” Create Draft Settlement~~ **Done, closed, verified (2026-07-16)**
7b. ~~Vertical Slice 2 â€” Add Settlement Line~~ **Done, closed, verified (2026-07-16)**
7c. ~~Vertical Slice 3 â€” Submit Settlement~~ **Done, closed, verified (2026-07-16)** â€” endpoint, handler, and tests were already present in the repository; no new code was required
7d. ~~Vertical Slice 4 â€” Get Settlement Detail~~ **Done, closed, verified (2026-07-17)**
7e. ~~Vertical Slice 5 â€” List My Settlements~~ **Done, closed, verified (2026-07-17)**
7f. ~~Vertical Slice 6 â€” Update Settlement Line~~ **Done, closed, verified (2026-07-17)**
7g. ~~Vertical Slice 7 â€” Remove Settlement Line~~ **Done, closed, verified (2026-07-17)**
7h. ~~Manager + Journal Workflow Batch (VS8 Approve / VS9 Reject / VS10 Reopen / VS11 Record Journal)~~ **Done, closed, verified (2026-07-17)**
7i. **Vertical Slice 12 — Update Settlement Header** — implemented 2026-07-21; awaiting client verification
8. SharePoint + Entra production adapters â€” partially complete (foundation implemented; production hardening/integration rollout remains)
9. Remaining API/UI work (photo upload flow, admin endpoints, broader workflow screens beyond implemented draft create + line add/edit/remove/submit + list/detail views, and replacing temporary frontend category inference with backend-exposed metadata) â€” partially complete after Frontend Stabilization Sprint
10. Backlog (post-MVP): duplicate/anomaly checks, Power BI balance report, budget validation (A-006), approver delegation (A-007)

---

## 7. Current Task

**Vertical Slice 12 — Update Settlement Header (2026-07-21): IMPLEMENTATION COMPLETE. Awaiting client verification.**

`PUT /api/v1/settlements/{settlementId}` — updates `SettlementDate` and `Purpose` on a Draft settlement. Reuses `Settlement.UpdateHeader()` already present in Domain (prior frozen-layer exception D-044). No Domain change this session.

**Files created this session:**
- `backend/src/PettyCash.Application/Settlements/Commands/UpdateSettlementHeaderCommand.cs` (command record + validator + handler)
- `backend/src/PettyCash.Api/Endpoints/UpdateSettlementHeaderRequest.cs` (wire DTO)
- `backend/tests/PettyCash.Api.Tests/Settlements/UpdateSettlementHeaderEndpointTests.cs` (6 integration tests)
- `frontend/src/features/settlements/hooks/useUpdateSettlementHeader.ts` (mutation hook)
- `frontend/src/features/settlements/components/EditSettlementHeaderForm.tsx` (inline edit form with client-side validation)

**Files modified this session:**
- `backend/src/PettyCash.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs` (registered `UpdateSettlementHeaderCommandHandler`)
- `backend/src/PettyCash.Api/Endpoints/SettlementsEndpoints.cs` (added `MapPut("/{settlementId:guid}", UpdateHeaderAsync)` + private method)
- `frontend/src/types/settlements.ts` (added `UpdateSettlementHeaderRequest` type)
- `frontend/src/api/settlementsClient.ts` (added `updateHeader` method, updated import)
- `frontend/src/pages/SettlementDetailPage.tsx` (wired `useUpdateSettlementHeader`, `isEditingHeader` state, "Edit Header" button gated on `isDraft`, `EditSettlementHeaderForm`, `handleSaveHeader`, `isUpdatingHeader` in `lineOperationPending`)

**Not yet run:** `dotnet build`, `dotnet test`, `npm run build` — no execution access this session.

**Note:** Migration `20260721103939_AddCategoryDisplayNameAndNullableDimensions` also pending `dotnet ef database update` client verification from a prior session.

_(Update this section the moment a new task starts — see §11.)_

## 8. Outstanding Architectural Decisions

These need an explicit client decision before (or during) the milestone that depends on them â€” see `docs/ASSUMPTIONS.md` for full detail:

- **A-005** â€” Single currency (EGP) for MVP, no multi-currency. Needs confirmation.
- **A-006** â€” Budget / over-settlement validation â€” not specified by the Guide, needs business sign-off before production integration work if in scope.
- **A-007** â€” Approver delegation (manager on leave, etc.) â€” same, needs business sign-off.
- **A-014** â€” Identity strategy for remaining client-side experience (login UX/token acquisition flow sequencing). Backend authentication foundation is now implemented; final product auth flow still requires explicit business/UX confirmation.
- **A-016** â€” Whether `ReceiptPhoto` belongs inside the `Settlement` aggregate's consistency boundary. Blocks photo-upload command wiring (D-020).
- **D-006** (Power Automate calls back into API rather than writing SharePoint directly) â€” logged as "Recommended," needs validation with the Phase 2 flow owner.

Do not silently resolve any of these by picking an option â€” surface them to the client per `PROJECT_RULES.md`'s ambiguity rule.

---

## 9. Verification Checklist

Before marking **any** sprint/milestone closed, all of the following must be confirmed â€” by the client, on the client's machine, not assumed from a clean build in an isolated session:

- [ ] `docker compose up -d` (from `database/`) â€” Postgres container healthy
- [ ] `dotnet restore` â€” no NU1605 or other restore errors
- [ ] `dotnet build` â€” zero errors (warnings-as-errors scope per D-032: `PettyCash.Api` only, for now)
- [ ] `dotnet test` â€” all tests passing, count explicitly stated
- [ ] `dotnet run` â€” starts without DI validation failures
- [ ] `GET /health` â€” returns Healthy for the active backend (SharePoint Graph probe when SharePoint is enabled; Postgres connectivity when Postgres is enabled)
- [ ] OpenAPI document reachable (`/openapi/v1.json`) â€” Development environment only
- [ ] `dotnet ef database update` â€” migrations apply cleanly against a fresh or existing volume
- [ ] `backend/PettyCash.sln` includes every project created this session (convention, `ARCHITECTURE.md` آ§8)
- [ ] `CHANGELOG.md`, `docs/TODO.md`, `docs/DECISIONS.md`, and (if relevant) `ARCHITECTURE.md` updated to match what was actually verified â€” not what was merely written

A milestone/sprint is **not** closed until every applicable box above is checked by the client and recorded in `CHANGELOG.md`/`docs/TODO.md`.

---

## 10. Git Workflow

- Working branch: `develop`.
- Release tags follow `vMAJOR.MINOR.PATCH` (current: `v0.5.2`).
- One `.csproj`/project is added to `backend/PettyCash.sln` in the same change that creates it (no separate step) â€” `ARCHITECTURE.md` آ§8.
- Commit granularity: small, reviewable milestones per the project's standing instruction (see root project instructions) â€” avoid bundling unrelated layers into one commit.
- Do not rewrite history on `develop`. Tag a release only after the client has completed the Verification Checklist (آ§9) for that unit of work.
- This AI session has no direct git execution access on the client's machine (confirmed â€” migrations, builds, and tests are hand-authored/reasoned about here and verified by the client separately). Do not claim a commit or tag was made unless the client performed it.

---

## 11. Resume Instructions

When resuming this project in a new AI session:

1. Read this file (`AI_HANDOFF.md`) in full.
2. Read `PROJECT_RULES.md` in full â€” it governs *how* to work, this file describes *where things stand*.
3. Cross-check آ§4 (Current Verified State) against `CHANGELOG.md`'s most recent entry and `docs/TODO.md`'s most recent status line â€” if they disagree, treat `CHANGELOG.md`/`docs/TODO.md` as more current and flag the discrepancy to the client rather than silently trusting this file.
4. Check آ§7 (Current Task) â€” if empty, ask the client which roadmap item (آ§6) to start, per the "ask for clarification on ambiguous requirements" rule. Do not pick one unprompted.
5. Before writing any code, check آ§5 (Frozen Layers) â€” if the task touches Domain or Application, stop and confirm the change is genuinely required and would satisfy the frozen-layer exception protocol (`PROJECT_RULES.md` آ§3) before proceeding.
6. Review `docs/ASSUMPTIONS.md` and آ§8 (Outstanding Architectural Decisions) for anything the current task depends on.

---

## 12. Stop Conditions

Stop and wait for explicit client input rather than proceeding, whenever:

- A task appears to require changing a frozen layer (Domain/Application) for a reason other than a genuine technical limitation with all five D-021-style conditions satisfied.
- A requirement is ambiguous or not covered by `docs/Developer-Guide.docx`, `ARCHITECTURE.md`, `TECH_STACK.md`, or `docs/DECISIONS.md`.
- An outstanding architectural decision (آ§8) blocks the task at hand.
- A verification step (آ§9) cannot be run in this session (e.g., no execution access to the client's machine) â€” state this explicitly rather than assuming success.
- A change would affect more than one milestone's worth of scope at once (e.g., "while I was in there I also...") â€” flag it as a separate, explicitly reviewable unit instead of folding it in.
- Any new business rule is discovered that is not in the Developer Guide â€” do not invent one; ask.

---

## 13. MANDATORY â€” Update After Every Completed Vertical Slice

**This section must be updated every time a vertical slice (a complete, independently verifiable unit of work â€” see `PROJECT_RULES.md` آ§4) is completed and verified. This is not optional and is not deferred to "later cleanup."**

On completion of a vertical slice, update in the same turn:
- آ§3 (Latest Release Tag) â€” if a new tag was cut.
- آ§4 (Current Verified State) â€” what is now built and verified, matching what `CHANGELOG.md`/`docs/TODO.md` say.
- آ§5 (Frozen Layers) â€” if a layer's freeze status changed, or a new frozen-layer exception was taken.
- آ§6 (Current Roadmap) â€” check off / update the completed item.
- آ§7 (Current Task) â€” set to the next task, or explicitly "None in progress â€” awaiting client direction" if nothing is queued.
- آ§8 (Outstanding Architectural Decisions) â€” remove resolved items, add newly discovered ones.

**Change log for this section itself** (append one line per update, do not delete history):

- 2026-07-15 â€” Sprint 5.1 closed and fully re-verified including D-037; document created.
- 2026-07-15 â€” Vertical Slice 1 (Create Draft Settlement) implemented and self-reviewed: آ§3/آ§4/آ§5/آ§6/آ§7 updated. Not yet client-verified; do not mark closed until آ§9's checklist is confirmed.
- 2026-07-16 â€” Vertical Slice 1 closed and fully verified by the client (incl. post-implementation fixes D-041, CS0246/CS1061): آ§4/آ§6/آ§7 updated; last-updated header updated.
- 2026-07-16 â€” Vertical Slice 2 (Add Settlement Line) closed and fully verified by the client (116 tests passing), incl. post-verification fix D-043 (missing file recreated and confirmed present): آ§4/آ§6/آ§7 updated; آ§7 set to Vertical Slice 3 with scope explicitly flagged as unconfirmed, not started; last-updated header updated.
- 2026-07-16 â€” Client confirmed Vertical Slice 3 scope = Submit Settlement (`SubmitSettlementCommand`). آ§6/آ§7 updated to remove the "scope not yet formally defined" note and record the confirmed scope. Documentation-only change, no code touched.
- 2026-07-16 â€” Vertical Slice 3 closed and fully verified by the client (116 tests passing, confirmed). Investigation showed all VS3 components (endpoint, handler registration, tests) were already present in the repository; no new code was required. آ§4/آ§6/آ§7 updated; آ§7 set to Vertical Slice 4 with scope explicitly flagged as not yet defined, awaiting client direction; last-updated header updated.
- 2026-07-17 â€” VS4â€“7 (Employee Workflow batch) closed and fully verified by the client (all tests passing). All code was found fully implemented in the repository at session start; documentation was the only gap. آ§3 roadmap block, آ§4, آ§6, آ§7, آ§13 updated; آ§7 set to Manager Workflow Batch (VS8â€“10), confirmed by client, not yet started. Last-updated header updated.
- 2026-07-17 â€” Documentation synchronization: repository state confirmed VS8â€“VS11 implemented, tested, and client-verified. Updated آ§3 roadmap block, آ§4 current verified state, آ§6 roadmap, and آ§7 current task to remove pending-language and align with code as source of truth.
- 2026-07-17 â€” Documentation synchronization follow-up: added auth/authz foundation, policy coverage, Postgres/SharePoint contract parity testing, and backend-aware health-check state; updated roadmap/verification wording to remove stale \"auth not started\" language.
- 2026-07-17 â€” Frontend Batch 1 implemented: initialized `frontend/` React+TypeScript app, added routing/layout, MSAL auth + protected routes, settlements API client, environment config, dashboard placeholder, and verified with `npm.cmd run build`; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-17 â€” Frontend Batch 2 implemented: added centralized AuthProvider/AuthContext, completed sign-in/sign-out/session-restore flow, finalized responsive shell (sidebar/top bar/profile menu/sign out), added Dashboard/My Settlements navigation and global loading/error UI, and verified with `npm.cmd run build`; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-17 â€” Frontend Batch 3 implemented: replaced My Settlements placeholder with full list UX (loading/empty/error/retry, search, status filter, newest-first sort, refresh, responsive table/cards, status badges), added settlement detail placeholder route, applied auth hardening (interaction-required redirect in API token flow, 401-triggered logout cleanup, startup initialization fallback UI, sign-out active-account cleanup), and verified with `npm.cmd run build`; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-17 â€” Frontend Batch 4 implemented: replaced Settlement Detail placeholder with full detail UX (header/metadata/lines, loading/error/not-found/retry, back navigation, deep-link route support, responsive line table/cards), added `useSettlement` and reusable detail components, aligned `SettlementSummaryDto` with backend contract, verified status mapping/stable keys/date-based sorting assumptions, and verified with `npm.cmd run build`; backend runtime API verification was blocked by unreachable local endpoint; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-19 â€” Frontend Vertical Slice 1 implemented: added complete create-draft workflow (`/settlements/new`) using existing `POST /api/v1/settlements`, reusable `SettlementForm`/`FormField`/`FormActions`, `useCreateSettlement`, Dashboard/My Settlements navigation actions, client-side validation aligned to backend constraints, duplicate-submit prevention, API/validation feedback, and redirect to created settlement detail; verified with `npm.cmd run build`; runtime API verification was blocked by unreachable local backend endpoint; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-19 â€” Frontend Vertical Slice 2 implemented: added complete add-line workflow for Draft settlements using existing `POST /api/v1/settlements/{settlementId}/lines`, reusable `SettlementLineForm`/`SettlementLineFields`/`SettlementLineActions`, `useAddSettlementLine`, Draft-only add controls in Settlement Detail, conditional mileage fields for mileage-required category, client-side UX validation aligned to backend constraints, duplicate-submit prevention, and backend-response-driven line/total updates; verified with `npm.cmd run build`; runtime API verification was blocked by unreachable local backend endpoint; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-19 â€” Frontend Vertical Slice 3 implemented: added complete update/remove workflows for Draft settlement lines using existing `PUT /api/v1/settlements/{settlementId}/lines/{lineId}` and `DELETE /api/v1/settlements/{settlementId}/lines/{lineId}`, shared add/edit form reuse, Draft-only per-line edit/delete controls, delete confirmation dialog, `useUpdateSettlementLine`, `useRemoveSettlementLine`, duplicate-operation prevention, and backend-response-driven settlement replacement (including totals); added Contract Verification section with endpoint/DTO checks and temporary frontend category inference note; verified with `npm.cmd run build`; runtime API verification was blocked by unreachable local backend endpoint; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-19 â€” Frontend Vertical Slice 4 implemented: added complete submit workflow for Draft settlements using existing `POST /api/v1/settlements/{settlementId}/submit` (no-body request), `useSubmitSettlement`, Draft+line-count gated submit action, `SubmitSettlementDialog`, duplicate-submit prevention, backend/API error handling, and backend-response-driven status/totals/read-only transition (editing controls hidden after backend status change); verified with `npm.cmd run build`; runtime API verification was blocked by unreachable local backend endpoint; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-19 â€” Frontend Stabilization Sprint completed: no new features added; refactored duplicate mutation error parsing into shared helper, consolidated confirmation dialogs into shared accessible component (focus trap + focus return), added abort-signal support for list/detail fetches with unmount-safe cancellation handling, revalidated frontend/backend contracts for create/get-mine/get-detail/add/update/delete/submit, and verified with `npm.cmd run build`; runtime live API regression verification remained blocked by unreachable local backend endpoint; updated آ§4/آ§6/آ§7 and last-updated header.
- 2026-07-19 â€” Manager Workflow frontend implemented: `getInbox` API client method, `useApproverInbox`/`useApproveSettlement`/`useRejectSettlement` hooks, `ManagerInboxTable`/`ManagerInboxCard`/`RejectSettlementDialog` components, `ManagerInboxPage`/`ManagerSettlementDetailPage` pages, routes `/manager-inbox` and `/manager-inbox/:requestId`, Manager Inbox sidebar nav entry, manager action panel CSS; zero backend changes; all reused component prop signatures verified before use; آ§4/آ§7 and last-updated header updated; awaiting client `npm run build` verification.
- 2026-07-19 â€” Manager Workflow verification pass: added `isManager` to `AuthUser` (read from MSAL ID token `roles` claim), added `ManagerRoute` guard, gated nav entry on `user.isManager`, aligned `useApproveSettlement`/`useRejectSettlement` to ref-based duplicate-call-prevention pattern matching `useSubmitSettlement`; confirmed reject validation, post-decision state, inbox re-fetch, and empty-inbox behaviour correct; آ§4/آ§7 and last-updated header updated; awaiting client `npm run build` verification.
- 2026-07-19 â€” Frontend dev-auth env vars: added `VITE_DEV_USERNAME` / `VITE_DEV_ROLE` to `env.d.ts`, `env.ts` (`appEnv.devUsername`, `appEnv.devRole`), `.env`, and `.env.example`; `DevAuthContext.tsx` now derives `DEV_USER` (username, display name, `isManager`) from those vars instead of hardcoded literals; defaults preserve previous behaviour (`spender.demo` / `Spender`) so no `.env` change is required to keep existing local setups working; آ§7 and last-updated header updated; awaiting client `npm run build` verification.
- 2026-07-20 â€” Development CORS configured: `AddCors`/`UseCors` added to `Program.cs` under `IsDevelopment()` guards; policy `"DevSpa"` allows `http://localhost:5173` and `http://localhost:5174`, any header, any method, no credentials; `UseCors` placed before auth middleware so OPTIONS preflight requests clear before the auth pipeline; zero production change, no new packages, no new files; آ§7 and last-updated header updated; awaiting client `dotnet build` + runtime CORS verification.
- 2026-07-20 â€” Category Mappings vertical slice (full-stack, frozen-layer exception approved): Backend â€” `DisplayName` added to `CategoryMappingReadModel` record and `CategoryMappingConfiguration` (EF column + updated seed data), migration `20260720000001_AddCategoryMappingDisplayName` (ADD COLUMN + backfill UPDATE + DROP DEFAULT), `PettyCashDbContextModelSnapshot` updated, `CategoryMappingDto` (Application/DTOs, 3 public fields only), `GetCategoryMappingsQuery`+`GetCategoryMappingsQueryHandler` (Application/Settlements/Queries), handler registered in `ApplicationServiceCollectionExtensions`, `CategoryMappingsEndpoints` (Api/Endpoints, `GET /api/v1/category-mappings`, separate file + route group), `Program.cs` (`MapCategoryMappingsEndpoints()`). Frontend â€” `CategoryMappingDto` type added to `types/settlements.ts`, new `api/categoriesClient.ts` (`getAll`), new `features/settlements/hooks/useCategoryMappings.ts` (load-on-mount, abort-signal, retry), `SettlementLineFields.tsx` category text input replaced with `<select>` populated from `categories` prop, `SettlementLineForm.tsx` forwards `categories`+`categoriesLoading` props, `SettlementDetailPage.tsx` imports `useCategoryMappings`, removes hardcoded `categoryRequiresMileage` function, derives `showMileageFields` from `categories.find(...).kmRequired`, shows category error+retry inline, passes `categories`+`categoriesLoading` to form; awaiting client `dotnet build` + `dotnet ef database update` + `npm run build` verification.

- 2026-07-21 â€” Snapshot seed data corrected: previous session's edit had incorrectly removed `DimensionDefaults` from `OFFICE_SUPPLIES` and `GOVERNMENT_FEES` snapshot seed rows based on a misreading of the constructor argument order. Re-verification against `ReferenceData.cs` and `CategoryMappingConfiguration.HasData` confirmed all three rows have non-null `DimensionDefaults`; snapshot restored to include them. No `UpdateData` needed in the migration (database rows written by `InitialCreate` are already correct). Model/migrations/snapshot/HasData now fully consistent. Awaiting client `dotnet restore && dotnet build && dotnet ef database update && dotnet test` verification.
- 2026-07-21 — CategoryMappingsEndpointTests.cs added (6 tests): 401 for anonymous, 200 for any authenticated role, DTO shape, seed data correctness, internal-field non-exposure; Api.Tests 88→94; total backend 224→230; no production code changed; awaiting client dotnet test + npm run build verification.
- 2026-07-21 — VS12 (Update Settlement Header) implemented: `UpdateSettlementHeaderCommand`/Validator/Handler (Application), DI registration, `UpdateSettlementHeaderRequest` DTO, `PUT /{settlementId:guid}` endpoint + private handler (Api), 6 integration tests (Api.Tests), `useUpdateSettlementHeader` hook, `EditSettlementHeaderForm` component, `SettlementDetailPage` integration (frontend). No Domain change — `Settlement.UpdateHeader()` already present. §4/§6/§7 updated; last-updated header updated; awaiting client `dotnet build` + `dotnet test` + `npm run build` verification.

# Session Start Protocol

Every new AI session must:

1. Read this file.
2. Read PROJECT_RULES.md.
3. Read any documents referenced by those files.
4. Determine the current task.
5. Do not rely on previous chat history.
6. Implement only the current task.
7. Stop after local verification is required.
