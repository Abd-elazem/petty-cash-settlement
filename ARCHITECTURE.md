# ARCHITECTURE.md — Petty Cash Settlement System
_Living document. Last updated: Vertical Slices 8–11 documentation synchronization (2026-07-17)._
Business source of truth: `docs/Developer-Guide.docx`
Approved technology choices: `TECH_STACK.md` (Locked vs Flexible status lives there, not duplicated here).
Decision reasoning: `docs/DECISIONS.md`. Open risks: `docs/ASSUMPTIONS.md`. Change history: `CHANGELOG.md`.
Solution file: `backend/PettyCash.sln` — every project (Domain, Application, Infrastructure, Api, and their test
projects so far) is added as each is created; this is enforced by convention, not tooling.

---

## 1. Layers (Clean Architecture)

```
Domain        → Entities, value objects, domain state machine, domain services (VAT calc). No dependencies.
Application   → Use cases (commands/queries), IRepository / IJournalConnector / IPhotoStore interfaces,
                validation, authorization policies. Depends on Domain only.
Infrastructure→ Adapters:
                - PostgresSettlementRepository (dev/local implementation of ISettlementRepository)
                - SharePointSettlementRepository (production implementation — real System of Record)
                - GraphPhotoStore, EntraIdentityProvider, FnOJournalConnector, AuditLogWriter
                Depends on Application (implements its interfaces).
Api           → REST controllers (ASP.NET Core), auth middleware, DTO mapping. Depends on Application only.
WebClient     → React SPA. Talks only to the REST API. Zero Microsoft SDK/token exposure (Guide §5.4).
```

Rule enforced: **Application layer never references `Microsoft.Graph`, `SharePoint`, `Npgsql`/EF, or any storage-specific type.** Only Infrastructure knows which repository implementation is active. This is what makes "swap Postgres for SharePoint without touching business logic" true in practice.

---

## 2. Persistence Strategy (confirmed — see DECISIONS D-002, D-011)

- **Local/dev:** PostgreSQL, behind `ISettlementRepository`. Fast iteration, real SQL for local testing, no Microsoft tenant dependency during early development.
- **Production:** SharePoint lists/library, per Guide §5.4–5.5 — this is an explicit business requirement, not a technical preference, and is not up for revision by engineering convenience.
- Both are interchangeable implementations of the same interface. **No code outside Infrastructure may know which one is active.** Each adapter is directly verified against the Application interface contracts (round-trip, not-found, concurrency) as it's built; a single shared/parameterized suite run against both Postgres and SharePoint is deferred until the SharePoint adapter exists, to avoid baking Postgres-specific assumptions into what should be storage-agnostic tests (D-027).
- Consequence for schema design: the logical schema below is written to be implementable in either a relational table or a SharePoint list without change to Domain/Application code.

---

## 3. Domain Model

**Aggregate: `Settlement`** (root) — owns lifecycle and invariants.
- `SettlementLine` (entity, child of Settlement, not independently addressable)
- Value objects: `Money` (amount + currency, currency fixed to EGP — A-005), `VatBreakdown` (gross/net/vat), `OdometerReading`

**Reference data (read models, not part of the aggregate):**
- `AppUserProfile` — spender identity + routing data
- `CategoryMapping` — category → account/dimension/tax rules
- `ApprovalRecord` — one per approval decision (append-only)
- `AuditLogEntry` — append-only, one per use-case execution

Settlement is the aggregate root (not Header+Lines as independent entities) so line mutations always go through header invariants (status must be Draft to edit; TotalAmount recomputes on line change).

---

## 4. Entity Relationships

```
AppUserProfile 1 ──── * Settlement
Settlement     1 ──── * SettlementLine
SettlementLine * ──── 1 CategoryMapping   (resolved by code, snapshotted — not a live FK)
Settlement     1 ──── * ApprovalRecord
Settlement     1 ──── * AuditLogEntry
SettlementLine 1 ──── * ReceiptPhoto (metadata only; binary in photo store)
```

`CategoryMapping` values are copied onto the `SettlementLine` at submit time, not live-looked-up at journal-creation time (D-004): Finance may edit the mapping list after a settlement is submitted, and already-submitted settlements must not silently retarget accounts.

---

## 5. Database Schema (logical — implementable as Postgres tables or SharePoint lists)

### Settlement
| Field | Type | Constraint |
|---|---|---|
| RequestId | GUID | PK |
| Version | int | starts at 1; increments on resubmit-after-reject |
| SettlementDate | date | required |
| Purpose | string(500) | required |
| SpenderId | string | FK → AppUserProfile.AppUserId |
| SpenderNameSnapshot | string | copied at creation |
| WorkerIdSnapshot | string | copied at creation |
| ApproverEmailSnapshot | string | copied at creation |
| TotalAmount | decimal(18,2) | computed, persisted |
| Currency | string(3) | fixed "EGP" (A-005) |
| Status | enum | see §6 |
| ApprovalComment | string(1000) | nullable |
| JournalBatchNumber | string | nullable |
| CreatedAtUtc / ModifiedAtUtc | datetime | audit |
| _(concurrency)_ | — | **Not an application-level column** — resolved concretely at Sprint 4 via each store's native mechanism (Postgres `xmin`, future SharePoint `__etag`), not a hand-maintained RowVersion field. See D-023. |

### SettlementLine
| Field | Type | Constraint |
|---|---|---|
| LineId | GUID | PK |
| RequestId | GUID | FK → Settlement |
| LineNo | int | unique per RequestId |
| CategoryCode | string | required |
| ExpenseMainAccountSnapshot | string | copied from CategoryMapping at submit |
| DimensionDefaultsSnapshot | string | copied at submit |
| GrossAmount | decimal(18,2) | > 0 |
| IsVat | bool | |
| VatAmount / NetAmount | decimal(18,2) | server-computed, never trusted from client |
| Notes | string(1000) | nullable |
| CarPlate | string | required if CategoryMapping.KmRequired |
| OdometerKm | decimal | required if CategoryMapping.KmRequired |

### ReceiptPhoto
| Field | Type |
|---|---|
| PhotoId | GUID PK |
| LineId | GUID FK |
| StorageRef | string (library path/driveItemId, or object-storage key in dev) |
| UploadedAtUtc | datetime |

### AppUserProfile / CategoryMapping / ApprovalRecord / AuditLogEntry
Map directly to Guide §5.2 / §5.5 with `Active`/audit fields added. `AuditLogEntry` is new — not in the original Guide schema — additive only (A-013).

**New fields beyond the Guide's SharePoint schema:** `Settlement.Version` (reject→edit→resubmit cycle, A-002/A-003) — additive/non-breaking in either storage backend. (The RowVersion concurrency field originally sketched here was superseded by D-023: native `xmin`/`__etag` per store, no extra column needed.)

---

## 6. State Machine

**Revised at Milestone 0.2 (see D-014):** `Rejected` is its own persisted status — matching the Guide §5.5 SharePoint Status choice list exactly (Draft / Submitted / Approved / Rejected / Journalled / Posted) — not an instant auto-revert to Draft. The spender sees the rejection and comment, then takes an explicit `ReopenForEdit` action that moves Rejected → Draft and increments Version. This preserves the Rejected state the Guide's own schema expects to exist, instead of silently skipping past it.

```
Draft ──submit──► Submitted ──approve──► Approved ──journal created──► Journalled ──(external, AP)──► Posted
  ▲                    │
  │                 reject
  │                    ▼
  └──reopenForEdit── Rejected   (ApprovalComment set on reject; Version increments on reopen, not on reject)
```

- Transitions only fire through Application-layer use cases — never set directly by Infrastructure or by Power Automate writing a status field arbitrarily. Power Automate reads `Status=Submitted` to trigger, and calls back into the **API** (`/approve`, `/reject`, `/journal`) to record outcomes (D-006). This keeps invariants (e.g., "Journalled only after JournalBatchNumber write succeeds," Guide §6.4) enforced in one place.
- Editing lines is only permitted in `Draft`. `Rejected` is read-only until `ReopenForEdit` is called.
- `Posted` is informational only — reflects AP's manual D365FO action; nothing in this system sets it (Guide §7: the system never posts).

---

## 7. RBAC Model

| Role | Can do |
|---|---|
| **Spender** | Create/edit own Draft settlements, submit, view own settlements only |
| **Approver** (Manager) | View settlements routed to them, approve/reject with comment |
| **AP Accountant** | Read-only view of Journalled settlements + journal numbers; no posting action exists in-app |
| **Finance Content Owner** | CRUD `CategoryMapping` only |
| **App Admin** | CRUD `AppUserProfile` |
| **System/Service** | Power Automate callback + F&O sync; scoped to status-transition + journal-writeback endpoints only |

Enforcement point: Application-layer authorization policies per use case (e.g., `SubmitSettlementUseCase` checks `Settlement.SpenderId == currentUser.Id` regardless of role).

---

## 8. Folder Structure (as implemented on disk, under `backend/`)

```
backend/
  PettyCash.sln
  src/
    PettyCash.Domain/
      Common/            (AggregateRoot<TId>, ValueObject, IDomainEvent)
      Exceptions/         (DomainException hierarchy)
      Settlements/         (Settlement, SettlementLine, state machine, value objects, Events/)
    PettyCash.Application/
      Abstractions/        (ISettlementRepository, ICategoryMappingRepository, IAppUserProfileRepository,
                            IPhotoStore, IAuditLogger, IVatConfiguration, ICurrentUserContext, CurrentUser)
      Authorization/       (ISettlementAuthorizationPolicy)
      Common/              (ICommand/ICommandHandler, IQuery/IQueryHandler — hand-rolled, D-017)
      DTOs/                (SettlementDto, SettlementLineDto, SettlementSummaryDto)
      Exceptions/          (AppException hierarchy: NotFound, Forbidden, Validation, Concurrency)
      Settlements/
        Commands/          (one file per command: record + validator + handler co-located)
        Queries/
        SettlementMapper.cs
      DependencyInjection/ (AddApplication() extension method)
    PettyCash.Infrastructure/
      Postgres/
        PettyCashDbContext.cs         (DbSets are `internal` — never leaves this project; see D-021/AssemblyInfo.cs for the one deliberate test-only exception)
        PettyCashDbContextFactory.cs   (design-time factory for `dotnet ef` tooling)
        ConfigurationVatConfiguration.cs
        Configurations/                (Fluent API: SettlementConfiguration incl. owned Lines/value objects, CategoryMappingConfiguration, AppUserProfileConfiguration, AuditLogEntryConfiguration)
        Repositories/                  (PostgresSettlementRepository, PostgresCategoryMappingRepository, PostgresAppUserProfileRepository, PostgresAuditLogger)
      DependencyInjection/ (AddInfrastructure() extension method)
      Migrations/           (`20260713120000_InitialCreate.cs`, `.Designer.cs`, `PettyCashDbContextModelSnapshot.cs`)
    PettyCash.Api/
      Program.cs             (minimal hosting model, top-level statements)
      GlobalExceptionHandler.cs
      appsettings.json, appsettings.Development.json
      Properties/launchSettings.json
      Development/           (DevelopmentCurrentUserContext.cs — IsDevelopment() only, D-035)
      Endpoints/             (SettlementsEndpoints.cs + request DTOs — VS1 through VS11)
  tests/
    PettyCash.Domain.Tests/
    PettyCash.Application.Tests/
      Fakes/              (in-memory repository/context fakes — no mocking library, D-017-adjacent choice)
      Settlements/Commands/, Settlements/Queries/, Authorization/, Validators/
    PettyCash.Infrastructure.Tests/
      PostgresContainerFixture.cs   (Testcontainers-managed Postgres, shared across the "Postgres" xUnit collection)
      SettlementRepositoryTests.cs, SettlementConcurrencyTests.cs, ReferenceDataRepositoryTests.cs, AuditLoggerTests.cs, MigrationTests.cs
web/
  src/ (React SPA — not yet created)
```

Every `.csproj` created is added to `PettyCash.sln` as part of the same change — see the project's own commit/turn in TODO.md, not a separate step.

---

## 9. API Specification (v1 — surface only, no code)

Base: `/api/v1`

| Method | Route | Role | Purpose |
|---|---|---|---|
| POST | `/settlements` | Spender | Create Draft — **implemented, Vertical Slice 1** |
| PUT | `/settlements/{id}` | Spender | Update Draft header |
| POST | `/settlements/{id}/lines` | Spender | Add line — **implemented, Vertical Slice 2** |
| PUT | `/settlements/{id}/lines/{lineId}` | Spender | Edit line (Draft only) — **implemented, Vertical Slice 6** |
| DELETE | `/settlements/{id}/lines/{lineId}` | Spender | Remove line (Draft only) — **implemented, Vertical Slice 7** |
| POST | `/settlements/{id}/lines/{lineId}/photos` | Spender | Upload receipt photo — **not yet implemented at Application layer, see D-020/A-016** |
| POST | `/settlements/{id}/submit` | Spender | Draft → Submitted — **implemented, Vertical Slice 3** |
| GET | `/settlements/mine` | Spender | List own settlements — **implemented, Vertical Slice 5** |
| GET | `/settlements/{id}` | Spender/Approver/AP | Get detail (ownership/role checked) — **implemented, Vertical Slice 4** |
| POST | `/settlements/{id}/approve` | Approver, or System (flow callback) | Submitted → Approved — **implemented, Vertical Slice 8** |
| POST | `/settlements/{id}/reject` | Approver, or System | Submitted → Rejected + comment — **implemented, Vertical Slice 9** |
| POST | `/settlements/{id}/reopen` | Spender | Rejected → Draft, Version++ (D-014) — **implemented, Vertical Slice 10** |
| POST | `/settlements/{id}/journal` | System only | Approved → Journalled, writes JournalBatchNumber — **implemented, Vertical Slice 11** |
| GET | `/category-mappings` | Any authenticated | Populate line category dropdown |
| CRUD | `/admin/category-mappings` | Finance Content Owner | Maintain mapping list |
| CRUD | `/admin/users` | App Admin | Maintain spender profiles |
| POST | `/receipts/parse` | Spender | Optional AI pre-fill (feature-flagged, D-009) |

All mutating endpoints: idempotency key = `RequestId` (+ `Version` where relevant), D-008.

---

## 10. UI Navigation Map

```
Login
 └─ My Settlements (list, filter by status)
     ├─ New Settlement → Header form → Add Line(s) [+photo, +AI-assist toggle] → Review → Submit
     ├─ Settlement Detail (read-only once Submitted; shows ApprovalComment if Rejected)
     └─ Rejected Settlement → Edit (reopens as Draft, Version++) → Submit

Approver view (role-gated, same app):
 └─ Pending Approvals → Settlement Detail → Approve / Reject (+comment)

Admin:
 ├─ User Profiles (CRUD)
 └─ Category Mappings (CRUD)
```

---

## 11. Cross-cutting

- **Logging:** structured (JSON), correlation ID = RequestId, one log entry per use-case execution minimum.
- **Audit:** every status transition writes an `AuditLogEntry` (who, when, from-status, to-status).
- **Feature flags:** AI receipt parsing (D-009), future anomaly/duplicate detection — off by default, config-driven, no Domain/Application branching to support "off."

---

## 12. Application Layer (Milestone 0.3)

**Use cases implemented** — one command/query handler each, matching §9's API surface except photo upload (deferred, D-020):
CreateDraftSettlement, AddLine, UpdateLine, RemoveLine, Submit, Approve, Reject, ReopenForEdit, RecordJournal (commands); GetSettlementById, GetMySettlements (queries). All mutating commands return the full `SettlementDto` (not a partial/line-level DTO) so the client always has a fresh total and line list after any edit — a simplification made during self-review rather than the original per-endpoint-shaped-response plan.

**Two-tier exception model:**
- `PettyCash.Domain.Exceptions.DomainException` (from Milestone 0.2) — business-rule violations inside the aggregate (e.g. "cannot submit with no lines"). Handlers let these propagate uncaught; a future Api-layer exception filter maps them to 400 Bad Request.
- `PettyCash.Application.Exceptions.AppException` (new) — orchestration-level failures: `NotFoundException` (404), `ForbiddenException` (403), `ValidationException` (400, wraps FluentValidation failures), `ConcurrencyException` (409).
Handlers don't wrap Domain exceptions into Application ones — a Domain rule violation and an authorization failure are different kinds of "no," and collapsing them would lose that distinction for the Api layer.

**Authorization:** centralized in `ISettlementAuthorizationPolicy` (Authorization/), not inlined per-handler — every ownership/role check for the RBAC model in §7 has exactly one implementation. `EnsureCanApproveOrReject` treats `UserRole.System` (the Power Automate callback, D-006) as an authorized caller without an ownership check, in the same code path as the human-approver check, rather than a special-cased bypass elsewhere.

**Identity handling:** commands never accept an identity field from the client (D-016) — `CreateDraftSettlementCommand` has no `SpenderId` parameter; the handler resolves the spender from `ICurrentUserContext.Current`.

**Idempotency:** `RecordJournalCommand` implements D-008 concretely — see D-018.

**Reference data:** `CategoryMappingReadModel`/`AppUserProfileReadModel` (Abstractions/) are plain records, not Domain entities — see D-019. Resolved via `ICategoryMappingRepository`/`IAppUserProfileRepository` and snapshotted onto the aggregate per D-004.

**Deferred:** photo upload (`IPhotoStore` exists, no command wired to it yet — D-020, ASSUMPTIONS.md A-016). No `IUnitOfWork` (D-015, with a documented audit-ordering limitation on `IAuditLogger`). No MediatR (D-017).

**Testing:** `PettyCash.Application.Tests` uses hand-written in-memory fakes (Fakes/) for every interface — no mocking library. Covers: every command handler's happy path + at least one authorization-failure path + at least one not-found path; the full authorization policy matrix in isolation; FluentValidation validators independent of handlers; and the D-008/D-018 idempotency behavior explicitly (`Handle_RetriedWithSameJournalNumber_IsIdempotent_DoesNotThrow` / `Handle_RetriedWithDifferentJournalNumber_Throws`).

---

## 13. Infrastructure Layer — Postgres dev adapter (Sprint 4)

**What's implemented:** `PettyCashDbContext` + Fluent API configurations for all four persisted shapes (Settlement incl. owned Lines/value objects, CategoryMappingReadModel, AppUserProfileReadModel, AuditLogEntry); `PostgresSettlementRepository`, `PostgresCategoryMappingRepository`, `PostgresAppUserProfileRepository`, `PostgresAuditLogger`; `ConfigurationVatConfiguration`; `AddInfrastructure()` DI registration; dev seed data (3 category mappings, 1 spender profile) via `HasData`.

**Mapping approach:** Settlement.Lines is an EF *owned collection* (OwnsMany), not a `DbSet<SettlementLine>` — matches the Domain rule that lines aren't independently addressable (D-022). Money/VatBreakdown/OdometerReading materialize via EF's constructor-parameter binding (their constructor parameter names already match property names) — no Domain change needed for any value object. Settlement/SettlementLine themselves needed one small Domain change to support this — see D-021, approved and applied.

**Concurrency:** Postgres's native `xmin` column via a shadow `uint` property (`HasColumnType("xid")` + `IsConcurrencyToken()`), not a hand-rolled RowVersion. Note: the originally-used `UseXminAsConcurrencyToken()` extension method was found obsolete/removed in the pinned Npgsql 9.0.4 during build verification and replaced with this current officially-documented mechanism — same outcome, current API (D-023). `DbUpdateConcurrencyException` is caught inside `PostgresSettlementRepository` and re-thrown as `PettyCash.Application.Exceptions.ConcurrencyException` — no EF-specific exception type ever crosses the Infrastructure boundary.

**Transactions:** each repository method (`AddAsync`/`UpdateAsync`) wraps its own explicit transaction, giving atomicity across a Settlement header + its owned Lines (the aggregate's real consistency boundary). Deliberately NOT extended to cover `IAuditLogger`'s separate write — see D-026 for why that would be the wrong kind of improvement.

**Not registered by `AddInfrastructure()`:** `ICurrentUserContext` (needs real auth, out of scope this sprint) and `IPhotoStore` (needs SharePoint/Graph or a resolved A-016, both out of scope) — D-025. A host wiring only `AddApplication()` + `AddInfrastructure()` has an intentionally incomplete container until a later sprint fills these in.

**Testing:** `PettyCash.Infrastructure.Tests` uses Testcontainers.PostgreSql — a real, disposable Postgres container, no mocks (ASSUMPTIONS.md A-017 notes the Docker prerequisite this implies). Covers round-trip/aggregate persistence (including VAT + odometer owned values), not-found returns null, spender-scoped queries, status-transition + Version-increment persistence, line removal, optimistic-concurrency conflict (two contexts racing on one row), seed-data verification, audit log round-trip, and post-migration table existence.

**Known gap when this section was first written, now resolved:** migrations were hand-authored (no command-execution access to the user's machine, confirmed) rather than tool-generated — `InitialCreate` + Designer + ModelSnapshot exist under `Migrations/`. See D-023's migration-detail note and TODO.md's Sprint 4 fourth round for the full writeup.

---

## 14. Api Layer — Foundation (Sprint 5)

**What's implemented:** `PettyCash.Api` project (`Microsoft.NET.Sdk.Web`), minimal-hosting-model `Program.cs`, `GlobalExceptionHandler`, `appsettings.json`/`appsettings.Development.json`, `Properties/launchSettings.json`. No controllers, no endpoints beyond `/health` — by explicit Sprint 5.1 scope.

**Dependency direction verified:** `PettyCash.Api.csproj` references `PettyCash.Application` and `PettyCash.Infrastructure` only — no `PettyCash.Domain` project reference. `GlobalExceptionHandler` matches Domain exceptions by **namespace string** (`"PettyCash.Domain.Exceptions"`) rather than importing `PettyCash.Domain.Exceptions` and pattern-matching on `DomainException` directly — a `using` there would have technically compiled (Domain types are transitively visible via Application's own reference) but would violate the "never reference Domain directly" rule in substance, not just form. Caught and fixed during this sprint's own self-review, not shipped as the first draft.

**Exception handling:** `IExceptionHandler` + `AddProblemDetails()` (current ASP.NET Core 8/9 pattern, no third-party library). Maps `NotFoundException→404`, `ForbiddenException→403`, `ValidationException→400` (with a FluentValidation-derived `errors` extension), `ConcurrencyException→409`, any Domain-namespace exception→400, everything else→500 (logged at Error — the mapped ones are expected client outcomes, not server faults, and aren't logged as errors).

**OpenAPI:** built-in `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()`/`MapOpenApi()`), not Swashbuckle — Microsoft's own .NET 9 templates stopped defaulting to Swashbuckle in favor of this. No Swagger UI wired up yet (just the OpenAPI JSON document, exposed only in Development) — a visualizer (Scalar or Swagger UI) is a cheap addition once there's an endpoint worth visualizing.

**API versioning:** `Asp.Versioning.Http 8.1.0` (the .NET 9-targeted line, not 10.0.0 which targets .NET 10), configured with a URL-segment reader matching the `/api/v1` base path already documented in §9. Deliberately NOT wired to per-version OpenAPI documents: per Microsoft's own .NET Blog, that integration wasn't supported until .NET 10/Asp.Versioning 10 — attempting it on .NET 9 would mean hand-building glue Microsoft hadn't shipped, for no benefit while zero endpoints exist to version.

**Logging:** structured JSON console (`AddJsonConsole()`), zero new packages — `Microsoft.Extensions.Logging.Console` ships with `Microsoft.NET.Sdk.Web`. Serilog was deliberately not added this sprint given how much package-version friction Sprint 4 hit; revisit if richer sinks (e.g. file, Seq) become a real need.

**Health check:** `AspNetCore.HealthChecks.NpgSql 9.0.0` against the same `PettyCashDev` connection string Infrastructure uses — verifies real Postgres connectivity, not just "the process is running." Mapped at `/health`. Liveness/readiness split deliberately deferred (nothing yet distinguishes them).

**`TreatWarningsAsErrors`:** enabled, but scoped to `PettyCash.Api.csproj` only, not a solution-wide `Directory.Build.props`. Domain/Application/Infrastructure had just passed a fully verified, frozen build when this sprint started; retroactively enabling this there risks breaking that approved state on a warning with no way to verify or fix it locally. Applying it to fresh code written carefully this sprint honors the standing "keep WarningsAsErrors enabled" instruction without gambling with already-approved work.

**Not done, explicitly out of scope per Sprint 5.1:** business controllers/endpoints, authentication, SharePoint/Entra/Graph/Power Automate/D365FO integration.

**DI composition gap closed:** `ICurrentUserContext` (deliberately unregistered by `AddInfrastructure()`, D-025) had nothing satisfying it, which failed ASP.NET Core's Development-only startup DI validation on `dotnet run`. `DevelopmentCurrentUserContext` (`PettyCash.Api/Development/`) now fills it — a single fixed placeholder identity, registered only under `IsDevelopment()` in `Program.cs`, with zero Domain or Application changes (D-035). This is a composition fix, not a Sprint 5.2 start: still no real authentication, no claims parsing, no business endpoints.

---

## 15. Api Layer — Vertical Slice 1 (Create Draft Settlement)

**What's implemented:** `POST /api/v1/settlements` — the first business endpoint, `PettyCash.Api/Endpoints/SettlementsEndpoints.cs` + `CreateSettlementRequest.cs`. Reuses `CreateDraftSettlementCommand`/`CreateDraftSettlementCommandHandler` (Milestone 0.3) completely unchanged; the endpoint resolves the command handler and `IValidator<CreateDraftSettlementCommand>` from the container (both already registered by `AddApplication()`), validates explicitly, and returns the resulting `SettlementDto` as-is — no separate Api response DTO (D-024's no-duplicate-DTO precedent).

**Pattern chosen:** Minimal API (`MapPost` on an `IEndpointRouteBuilder` group), not an MVC controller — `PettyCash.Api` has no `AddControllers()` registration, and `Program.cs` has used the minimal hosting model exclusively since Sprint 5.1 (D-039). The versioned route group (`/api/v{version:apiVersion}/settlements`, using the `Asp.Versioning.Http` foundation from D-030) is built inside `SettlementsEndpoints.MapSettlementsEndpoints()` itself, so `Program.cs` stays a single `app.MapSettlementsEndpoints();` call.

**Validation:** invoked explicitly by the endpoint delegate, not by a generic pipeline (D-040) — Application registers FluentValidation validators but no handler calls them; the Api layer is the intended caller. Failures throw the existing `PettyCash.Application.Exceptions.ValidationException`, reusing `GlobalExceptionHandler`'s already-built 400/`errors` mapping.

**Bug fixed during this slice's self-review (D-038):** `DevelopmentCurrentUserContext`'s hard-coded dev identity (`"dev-local-user"`) didn't match the seeded `AppUserProfile` row (`"spender.demo"`, Sprint 4), which would have made this endpoint 404 on every local call — the two were never previously exercised together. Fixed in the Api-layer dev fixture only; no Domain/Application change.

**Testing:** `PettyCash.Api.Tests` — `ApiWebApplicationFactory` boots the real `Program`/host against a disposable Testcontainers Postgres instance (no mocks, matching `PettyCash.Infrastructure.Tests`'s convention) and applies the real `InitialCreate` migration before each test run. `CreateSettlementEndpointTests` covers: happy path (201, `Location` header, `SettlementDto` body incl. fields auto-filled from the seeded profile), empty purpose → 400, default/unset settlement date → 400, over-max-length purpose → 400.

**Not done, explicitly out of scope this slice:** every other command-backed endpoint (Update/Add/Remove line, Submit, Approve, Reject, Reopen, RecordJournal), `GET /settlements/{id}` and `GET /settlements/mine` (queries already exist at Application layer, unwired at Api layer), authentication.

---

## 16. Api Layer — Vertical Slice 2 (Add Settlement Line)

**What's implemented:** `POST /api/v1/settlements/{settlementId}/lines` — added to the same `SettlementsEndpoints.cs` route group as Vertical Slice 1, plus `AddSettlementLineRequest.cs`. Reuses `AddLineCommand`/`AddLineCommandHandler` (Milestone 0.3) completely unchanged. `SettlementId` is bound from the route (`{settlementId:guid}`), not the request body — there is no way for a client to post a body whose settlement id disagrees with the URL.

**Response shape:** `200 OK` with the updated `SettlementDto`, not `201 Created` — unlike settlement creation, adding a line mutates an existing aggregate rather than creating a new independently-addressable resource (`SettlementLine` is explicitly not independently addressable, D-003), so there is no new resource URI to report via `Location`.

**Validation and error mapping:** identical pattern to Vertical Slice 1 (D-040) — the endpoint explicitly calls `IValidator<AddLineCommand>.ValidateAsync`, throwing the existing `ValidationException` on failure. Two additional error paths are exercised for the first time by this slice, using machinery that already existed but was previously untested at the Api layer: `NotFoundException` for both an unknown `SettlementId` and an unknown/inactive `CategoryCode` (both → 404), and a Domain-namespace exception (`DomainValidationException`, e.g. a fuel category posted without an odometer reading) → 400 via `GlobalExceptionHandler`'s namespace-string match (D-031).

**No Domain/Application/Infrastructure change.** `AddLineCommand`, its validator, its handler, `ISettlementAuthorizationPolicy.EnsureCanEdit`, and the seeded `CategoryMapping` rows (`OFFICE_SUPPLIES`, `FUEL`, `GOVERNMENT_FEES`) were all already in place from Milestone 0.3/Sprint 4 — this slice is pure Api-layer wiring, matching Vertical Slice 1's shape.

**Testing:** `PettyCash.Api.Tests/Settlements/AddSettlementLineEndpointTests.cs` (6 tests — corrected from an earlier 8-test draft; see CHANGELOG.md VS2 entry) — non-fuel category happy path (incl. `TotalAmount` recomputation), fuel category with a valid odometer reading, zero gross amount (400, FluentValidation), car plate without odometer (400, FluentValidation's paired-fields rule), unknown category code (404), unknown settlement id (404). No Docker/Testcontainers changes — reuses the existing `ApiWebApplicationFactory`/`"Api"` xUnit collection from Vertical Slice 1.

**Not done, explicitly out of scope this slice:** Update/Remove line, Submit, Approve, Reject, Reopen, RecordJournal endpoints; `GET /settlements/{id}`/`GET /settlements/mine`; authentication (continues using `DevelopmentCurrentUserContext` per explicit client instruction — A-014 remains unresolved and unaddressed by this slice).

---

## 17. Api Layer — Vertical Slice 3 (Submit Settlement)

**What's implemented:** `POST /api/v1/settlements/{settlementId}/submit` — added to the same `SettlementsEndpoints.cs` route group as Vertical Slices 1 and 2. Reuses `SubmitSettlementCommand`/`SubmitSettlementCommandHandler` (Milestone 0.3) completely unchanged.

**Response shape:** `200 OK` with the updated `SettlementDto` — same reasoning as Vertical Slice 2 (D-042/D-003): Submit transitions an existing Settlement's status; no new resource is created.

**Validation and error mapping:** same explicit-validation pattern as Slices 1 and 2 (D-040). `SubmitSettlementCommandValidator` checks only that `SettlementId` is non-empty (route binding already guarantees a well-formed Guid). The real business invariants — settlement must be in Draft status and must have at least one line — are enforced inside `Settlement.Submit()` itself and throw Domain-namespace exceptions, mapped to 400 by `GlobalExceptionHandler`'s namespace-string match (D-031). Unknown `SettlementId` throws `NotFoundException` — mapped to 404.

**No Domain/Application/Infrastructure change.** All VS3 components (`SubmitSettlementAsync` delegate, `SubmitSettlementCommand` handler DI registration in `ApplicationServiceCollectionExtensions`, and `SubmitSettlementEndpointTests.cs`) were already present in the repository when Vertical Slice 3 was scoped. Documentation was the only gap.

**Testing:** `PettyCash.Api.Tests/Settlements/SubmitSettlementEndpointTests.cs` (4 tests): Draft-with-line → Submitted (200), Draft-with-no-lines → 400 (Domain rule), unknown settlement ID → 404, already-Submitted settlement re-submitted → 400 (Domain state-machine rule). Reuses the existing `ApiWebApplicationFactory`/`"Api"` xUnit collection.

**Not done, explicitly out of scope this slice:** Update/Remove line, Approve, Reject, Reopen, RecordJournal endpoints; `GET /settlements/{id}`/`GET /settlements/mine`; authentication (continues using `DevelopmentCurrentUserContext` — A-014 remains open).

---

## 18. Api Layer — Vertical Slice 4 (Get Settlement Detail)

**What's implemented:** `GET /api/v1/settlements/{settlementId}` — added to `SettlementsEndpoints.cs`. Reuses `GetSettlementByIdQueryHandler` (Milestone 0.3) completely unchanged. Route parameter bound via `{settlementId:guid}`. No request body; no validator invocation (a Guid route constraint is sufficient — the handler throws `NotFoundException` or `ForbiddenException` for business-level rejections). Returns `200 OK` with the full `SettlementDto`.

**Error mapping:** `NotFoundException` → 404; `ForbiddenException` → 403. Both via `GlobalExceptionHandler` (already built, Sprint 5.1). No new mapping needed.

**Route ordering:** `/mine` (VS5) is registered above `/{settlementId:guid}` in the route group, so the literal segment is resolved first. The Guid constraint would reject `"mine"` anyway, but the explicit ordering makes intent clear.

**Testing:** `PettyCash.Api.Tests/Settlements/GetSettlementEndpointTests.cs` (3 tests): existing owned settlement → 200 with full DTO, unknown settlement ID → 404, settlement with two lines → 200 with correct line count and TotalAmount. Reuses `ApiWebApplicationFactory`/`"Api"` xUnit collection.

**No Domain/Application/Infrastructure change.**

---

## 19. Api Layer — Vertical Slice 5 (List My Settlements)

**What's implemented:** `GET /api/v1/settlements/mine` — added to `SettlementsEndpoints.cs`. Reuses `GetMySettlementsQueryHandler` (Milestone 0.3) completely unchanged. No request body; no validator (query carries no client-supplied parameters — identity is always resolved server-side from `ICurrentUserContext`, consistent with D-016). Returns `200 OK` with `IReadOnlyList<SettlementSummaryDto>`.

**Route ordering:** registered above `/{settlementId:guid}` in the route group — see §18.

**Testing:** `PettyCash.Api.Tests/Settlements/GetMySettlementsEndpointTests.cs` (3 tests): endpoint returns 200 with a non-null list, a newly-created settlement appears in the list with correct summary fields (Purpose/Status/TotalAmount), and the response deserialises cleanly to `IReadOnlyList<SettlementSummaryDto>` (confirming the lightweight projection, not the full `SettlementDto` with Lines). Note: shared Testcontainers database across the `"Api"` collection; empty-list assertion is not safe and is deliberately avoided.

**No Domain/Application/Infrastructure change.**

---

## 20. Api Layer — Vertical Slice 6 (Update Settlement Line)

**What's implemented:** `PUT /api/v1/settlements/{settlementId}/lines/{lineId}` — added to `SettlementsEndpoints.cs` + `UpdateSettlementLineRequest.cs`. Reuses `UpdateLineCommandHandler` (Milestone 0.3) completely unchanged. Both IDs bound from route; request body carries the updated field values (CategoryCode, GrossAmount, IsVat, Notes, CarPlate, OdometerKm). Returns `200 OK` with the updated `SettlementDto` (D-042/D-003 — line not independently addressable; the parent aggregate is the returned resource).

**Validation:** explicit `IValidator<UpdateLineCommand>.ValidateAsync` call (D-040). Failures throw `PettyCash.Application.Exceptions.ValidationException` → 400 with `errors` extension. Domain rule violations (e.g. editing a non-Draft settlement) throw Domain-namespace exceptions → 400 via `GlobalExceptionHandler`'s namespace-string match (D-031).

**Error paths tested:** zero amount → 400 (FluentValidation), car-plate-without-odometer → 400 (FluentValidation paired-fields rule), unknown settlement → 404, unknown category → 404.

**Testing:** `PettyCash.Api.Tests/Settlements/UpdateSettlementLineEndpointTests.cs` (6 tests). Reuses `ApiWebApplicationFactory`/`"Api"` xUnit collection.

**No Domain/Application/Infrastructure change.**

---

## 21. Api Layer — Vertical Slice 7 (Remove Settlement Line)

**What's implemented:** `DELETE /api/v1/settlements/{settlementId}/lines/{lineId}` — added to `SettlementsEndpoints.cs`. Reuses `RemoveLineCommandHandler` (Milestone 0.3) completely unchanged. Both IDs bound from route; no request body. Returns `200 OK` with the updated `SettlementDto` (line removed, TotalAmount recomputed — same D-042/D-003 reasoning as VS6).

**Validation:** explicit `IValidator<RemoveLineCommand>.ValidateAsync` call for consistency with D-040 (the validator only checks both Guids are non-empty — the `:guid` route constraints already guarantee this in practice, but the call is kept so the pattern is uniform across all mutating endpoints).

**Error paths:** unknown settlement → 404 (`NotFoundException`); unknown line on a known settlement → 400 (`Settlement.RemoveLine()` throws a Domain-namespace exception, mapped to 400 by `GlobalExceptionHandler`'s namespace-string match, D-031).

**Testing:** `PettyCash.Api.Tests/Settlements/RemoveSettlementLineEndpointTests.cs` (4 tests): only line removed → 200 with empty Lines and zero TotalAmount, one-of-two lines removed → 200 with correct remaining line and TotalAmount, unknown settlement → 404, unknown line on known settlement → 400. Reuses `ApiWebApplicationFactory`/`"Api"` xUnit collection.

**No Domain/Application/Infrastructure change.**

---

## 22. Api Layer — Vertical Slice 8 (Approve Settlement)

**What's implemented:** `POST /api/v1/settlements/{settlementId}/approve` — added to `SettlementsEndpoints.cs`. Reuses `ApproveSettlementCommandHandler` (Milestone 0.3) unchanged.

**Authorization and behavior:** caller must be `UserRole.Approver` with matching `ApproverEmailSnapshot`, or `UserRole.System` (Power Automate callback path, D-006). Submitted → Approved only; invalid status transitions surface Domain-state errors mapped to 400.

**Testing:** `PettyCash.Api.Tests/Settlements/ApproveSettlementEndpointTests.cs` (5 tests): approver happy path, spender forbidden (403), wrong-state (400), unknown settlement (404), double-approve (400).

**No Domain/Application/Infrastructure change.**

---

## 23. Api Layer — Vertical Slice 9 (Reject Settlement)

**What's implemented:** `POST /api/v1/settlements/{settlementId}/reject` — added to `SettlementsEndpoints.cs` + `RejectSettlementRequest.cs`. Reuses `RejectSettlementCommandHandler` unchanged.

**Validation and behavior:** mandatory rejection comment (validator and Domain-level enforcement), approver/system authorization model same as VS8, and status transition Submitted → Rejected (D-014).

**Testing:** `PettyCash.Api.Tests/Settlements/RejectSettlementEndpointTests.cs` (6 tests): approver happy path, spender forbidden (403), empty/missing comment (400), wrong-state (400), unknown settlement (404).

**No Domain/Application/Infrastructure change.**

---

## 24. Api Layer — Vertical Slice 10 (Reopen Settlement)

**What's implemented:** `POST /api/v1/settlements/{settlementId}/reopen` — added to `SettlementsEndpoints.cs`. Reuses `ReopenSettlementCommandHandler` unchanged.

**Behavior:** owner-only reopen, Rejected → Draft, `Version++`, and lines preserved (D-014).

**Testing:** `PettyCash.Api.Tests/Settlements/ReopenSettlementEndpointTests.cs` (5 tests): reopen happy path, lines retained, wrong-state (400), unknown settlement (404), reopen then resubmit cycle.

**No Domain/Application/Infrastructure change.**

---

## 25. Api Layer — Vertical Slice 11 (Record Journal Entry)

**What's implemented:** `POST /api/v1/settlements/{settlementId}/journal` — added to `SettlementsEndpoints.cs` + `RecordJournalRequest.cs`. Reuses `RecordJournalCommandHandler` unchanged.

**Authorization and behavior:** System-role only, Approved → Journalled transition, and idempotent retry semantics for same journal batch number (D-018); conflicting second batch number on an already-journalled settlement is rejected.

**Testing:** `PettyCash.Api.Tests/Settlements/RecordJournalEndpointTests.cs` (7 tests): system happy path, idempotent same-batch retry (200), conflicting batch (400), wrong-state (400), spender forbidden (403), validation failure (400), unknown settlement (404).

**No Domain/Application/Infrastructure change.**
