# CHANGELOG

Human-readable summary of what changed, sprint by sprint. `docs/DECISIONS.md` is the authoritative record of *why*; this file is *what*, briefly.

## CategoryMappingsEndpointTests — Test gap closure — 2026-07-21
- Previous session confirmed repository implemented through VS12 with one gap: `CategoryMappingsEndpointTests.cs` absent from `PettyCash.Api.Tests/Settlements/`.
- Added `CategoryMappingsEndpointTests.cs` (6 integration tests): 401 for anonymous, 200 for Spender and Approver roles, correct DTO shape, all 3 seed rows verified (OFFICE_SUPPLIES/FUEL/GOVERNMENT_FEES with correct DisplayName and KmRequired), internal accounting fields absent from JSON response.
- No production code changed.
- **Verification (AI-executed via Desktop Commander):**
  - `dotnet test` ✅ **230 tests passing** (37 Domain + 59 Application + 40 Infrastructure + 94 Api)
  - `npm run build` ✅ clean
- **Awaiting client verification.**

## VS12 Resume — Full repository inspection and verification — 2026-07-21
- Full repository inspection confirmed the entire VS12 ("Journal Processing Integration") scope was already implemented. No new production code was required.
- **Backend verified complete (VS8–VS11, auth/authz, SharePoint infrastructure, CategoryMappings, approver inbox):**
  - `SettlementsEndpoints.cs` has 12 routes covering all operations with `RequireAuthorization` per role policy.
  - `GetApproverInboxEndpointTests.cs` (5 tests) and `AuthorizationCoverageEndpointTests.cs` (7 tests including Entra JWT path) present and correct.
  - `ApiWebApplicationFactory` extended with `CreateAnonymousClient`, `CreateEntraAnonymousClient`, `CreateEntraEnabledClient` (JWT signing via `JwtSecurityTokenHandler`).
  - Full SharePoint infrastructure wired: `SharePointFoundationServiceCollectionExtensions` with 4 SP repository implementations, Graph API client, retry/correlation/error mapping.
  - Second EF migration (`AddCategoryDisplayNameAndNullableDimensions`) present.
  - `CategoryMappingsEndpoints` with `GetCategoryMappingsQuery` registered in `Program.cs`.
  - `DevelopmentAuthenticationHandler`, `ClaimsCurrentUserContext`, `AuthenticationServiceCollectionExtensions` all present.
- **Frontend verified complete:** React SPA with MSAL auth, `ProtectedRoute`/`ManagerRoute`, 9 pages (Dashboard, MySettlements, NewSettlement, SettlementDetail, ManagerInbox, ManagerSettlementDetail, SignIn, Unauthorized, NotFound), all hooks including `useApproverInbox`/`useApproveSettlement`/`useRejectSettlement`, full `settlementsClient` (13 operations) and `categoriesClient`, all feature components.
- **Verification run (2026-07-21, AI-executed via Desktop Commander):**
  - `dotnet restore` ✅ (all projects up to date)
  - `dotnet build` ✅ (0 warnings, 0 errors, TreatWarningsAsErrors active on Api)
  - `dotnet ef database update` ✅ (already at latest migration — `AddCategoryDisplayNameAndNullableDimensions`)
  - `dotnet test` ✅ **224 tests passing** (37 Domain + 59 Application + 40 Infrastructure + 88 Api)
  - `npm run build` ✅ (tsc + vite, zero TypeScript errors; MSAL chunk-size advisory is expected and non-blocking)
- Documentation (AI_HANDOFF.md, CHANGELOG.md, docs/TODO.md, ARCHITECTURE.md) updated to reflect the verified repository state.

## Documentation synchronization — auth/authz + backend-aware health checks — 2026-07-17
- Synchronized `AI_HANDOFF.md`, `ARCHITECTURE.md`, `CHANGELOG.md`, and `docs/TODO.md` with the repository's implemented state after approved production hardening.
- Resolved documentation drift on Api dependency direction: Api is documented as depending on Application and Infrastructure composition roots (while still keeping no direct Domain reference).
- Added explicit architecture documentation for Entra/development authentication foundation, policy-based endpoint authorization, auth/authz API coverage tests, backend-aware `/health` selection, and Postgres/SharePoint repository contract parity tests.
- Updated roadmap/todo language to mark documentation synchronization complete and keep remaining hardening as future work.

## Vertical Slices 8–11 — Approve / Reject / Reopen / Record Journal — 2026-07-17
- Endpoints and tests for VS8–VS11 were found fully implemented in the repository:
  - **VS8** — `POST /api/v1/settlements/{settlementId}/approve`, reusing `ApproveSettlementCommandHandler`. Test file: `ApproveSettlementEndpointTests.cs` (5 tests).
  - **VS9** — `POST /api/v1/settlements/{settlementId}/reject`, body `RejectSettlementRequest`, reusing `RejectSettlementCommandHandler`. Test file: `RejectSettlementEndpointTests.cs` (6 tests).
  - **VS10** — `POST /api/v1/settlements/{settlementId}/reopen`, reusing `ReopenSettlementCommandHandler`. Test file: `ReopenSettlementEndpointTests.cs` (5 tests).
  - **VS11** — `POST /api/v1/settlements/{settlementId}/journal`, body `RecordJournalRequest`, reusing `RecordJournalCommandHandler` (idempotent same-batch retry, D-018). Test file: `RecordJournalEndpointTests.cs` (7 tests).
- No Domain/Application/Infrastructure code changes required. Verification status: **CLOSED (2026-07-17).**

## Vertical Slices 4–7 — Get Settlement / List My Settlements / Update Line / Remove Line — 2026-07-16
- All four endpoints found fully implemented; documentation was stale. Per PROJECT_RULES.md §15, code accepted as-is.
- VS4: `GET /api/v1/settlements/{id}` — `GetSettlementEndpointTests.cs` (3 tests).
- VS5: `GET /api/v1/settlements/mine` — `GetMySettlementsEndpointTests.cs` (3 tests).
- VS6: `PUT /api/v1/settlements/{id}/lines/{lineId}` — `UpdateSettlementLineEndpointTests.cs` (6 tests).
- VS7: `DELETE /api/v1/settlements/{id}/lines/{lineId}` — `RemoveSettlementLineEndpointTests.cs` (4 tests).
- **Verification status: CLOSED (2026-07-17). Fully verified by the client.**

## Vertical Slice 3 — Submit Settlement — 2026-07-16
- `POST /api/v1/settlements/{settlementId}/submit` — `SubmitSettlementEndpointTests.cs` (4 tests). All components found already present.
- **Verification status: CLOSED (2026-07-16). Fully verified by the client.**

## Vertical Slice 2 — Add Settlement Line — 2026-07-16
- Added `POST /api/v1/settlements/{settlementId}/lines`, `AddSettlementLineRequest.cs`, `AddSettlementLineEndpointTests.cs` (6 tests).
- Post-verification fix: `AddSettlementLineRequest.cs` missing from disk — recreated (D-043).
- **Verification status: CLOSED (2026-07-16). Fully verified by the client. 116 tests passing.**

## Vertical Slice 1 — Create Draft Settlement — 2026-07-15/16
- Added `POST /api/v1/settlements`, `CreateSettlementRequest.cs`, `PettyCash.Api.Tests` project, `ApiWebApplicationFactory`, `CreateSettlementEndpointTests` (4 tests).
- Post-implementation fixes: CS0246/CS1061 missing `using` directives; Testcontainers connection-string eager-capture bug (D-041); D-038 dev UserId mismatch.
- **Verification status: CLOSED (2026-07-16). Fully verified by the client.**

## Sprint 5.1 — API Foundation — 2026-07-14/15
- `PettyCash.Api`: `GlobalExceptionHandler`, API versioning, OpenAPI, health check, `DevelopmentCurrentUserContext` (D-035).
- Fixes: docker-compose added (D-036); `Microsoft.EntityFrameworkCore.Design` reference on startup project (D-037).
- **CLOSED (2026-07-15). 112 tests passing, all checklist items confirmed.**

## Sprint 4 — Infrastructure (Development) — 2026-07-13
- `PettyCash.Infrastructure`: `PettyCashDbContext`, 4 repositories, seed data, Testcontainers tests (40 tests).
- Domain change (approved): D-021 (`SettlementLine` parameterless ctor + settable `LineId`).
- Migration hand-authored: `20260713120000_InitialCreate`.

## Milestone 0.3 — Application layer — 2026-07-13
- 9 commands, 2 queries, validators, DTOs, authorization policy, DI. 59 tests.

## Milestone 0.2 — Domain layer — 2026-07-13
- `Settlement` aggregate, state machine, value objects. 37 unit tests.

## Milestone 0.1 / 0.1.1 — Architecture — 2026-07-13
- Clean Architecture layering, RBAC model, API surface, `TECH_STACK.md`. `CONTEXT.md` demoted to historical.

## Milestone 0 — Requirements & risk analysis — 2026-07-13
- Initial requirements, risk lists, stack recommendation.
