# Decisions

Format: ID | Decision | Reasoning | Alternatives considered | Status

---

**D-001 — Backend stack: .NET (ASP.NET Core 9).**
Reasoning: first-party parity with Microsoft Graph SDK, Entra External ID libraries, and Key Vault integration; CANEX IT is Microsoft-centric.
Alternatives: FastAPI (Python) — was in the initial CONTEXT.md scaffold; superseded by this decision on 2026-07-13 (see D-011).
Status: **Locked (approved 2026-07-13).** See TECH_STACK.md.

**D-002 — SharePoint is the production System of Record. PostgreSQL is a local/development-only implementation of the same repository interface.**
Reasoning: Guide §5.4–5.5 explicitly mandates SharePoint as production storage — this is a business requirement, not an engineering preference. Postgres in dev gives fast iteration and real SQL testing without depending on a live Microsoft tenant during early development.
Alternatives considered: Postgres as permanent system-of-record with SharePoint as a synced export — rejected by client (2026-07-13); SharePoint-only with no dev database — rejected as slower to iterate against.
Consequence: `ISettlementRepository` has two implementations (`PostgresSettlementRepository`, `SharePointSettlementRepository`); both must pass the same contract-test suite so dev/prod behavior doesn't drift.
Status: **Locked (approved 2026-07-13).** See TECH_STACK.md.

**D-012 — Clean Architecture, built-in DI, REST API, mobile-first React frontend; all Microsoft services AND PostgreSQL sit behind adapters — no exceptions.**
Reasoning: business logic must remain completely independent of infrastructure (client's explicit architecture rule); future Android/iOS/Desktop clients must consume the same REST API unchanged; PostgreSQL is infrastructure too, not a privileged exception just because it's used in dev — it goes behind `ISettlementRepository` exactly like SharePoint does (reinforces D-002).
Alternatives: none seriously considered — these were specified directly, not derived.
Status: **Locked (approved 2026-07-13).** See TECH_STACK.md.

**D-003 — Settlement is the aggregate root; lines are child entities, not independently addressable.**
Reasoning: preserves invariants (TotalAmount consistency, edit-only-in-Draft rule) in one place.
Status: Decided.

**D-004 — CategoryMapping values are snapshotted onto the SettlementLine at submit time, not live-looked-up at journal-creation time.**
Reasoning: Finance may edit the mapping list after a settlement is submitted; already-submitted settlements must not silently retarget accounts.
Status: Decided.

**D-005 — Single currency (EGP) for MVP; no multi-currency support.**
Status: Assumption — see ASSUMPTIONS.md A-005. Needs client confirmation.

**D-006 — Power Automate does not write directly to SharePoint status fields; it calls back into the API's `/approve`, `/reject`, `/journal` endpoints.**
Reasoning: keeps state-machine invariants enforced in exactly one place, preventing the "Status=Journalled set before write-back succeeds" bug class warned against in Guide §6.4.
Trade-off: flow needs a service-account credential against the API rather than using its native SharePoint connector directly — more setup, materially safer.
Status: Recommended — validate with Phase 2/flow owner.

**D-007 — Photos stored in a SharePoint document library (one folder per RequestId), not list attachments.**
Reasoning: Guide §5.5 already flags this as preferable when lines carry multiple photos.
Status: Decided, consistent with Guide's stated preference.

**D-008 — RequestId (+ Version) is the idempotency key for all mutating operations, especially journal creation.**
Reasoning: Guide §6.4 requires try/catch + resubmission on journal-creation failure; without an idempotency key, retries risk duplicate D365FO journals.
Status: Decided. Must be enforced in the F&O connector adapter.

**D-009 — AI receipt pre-fill is a feature flag, off by default.**
Reasoning: Guide §5.6 marks this explicitly optional.
Status: Decided (scope). Vendor still open — A-010.

**D-010 — Rejected settlements eventually return to Draft (same RequestId, Version increments) rather than creating a new RequestId.**
Reasoning: preserves one continuous audit history per settlement instead of fragmenting across RequestIds.
Status: Assumption — see ASSUMPTIONS.md A-002/A-003. Needs client confirmation. **Refined by D-014.**

**D-014 — `Rejected` is its own persisted status (matching Guide §5.5's SharePoint Status choice list exactly), not an instant auto-revert to Draft.**
Reasoning: caught during Milestone 0.2 domain modeling — D-010 as originally written conflicted with the Guide's own explicit schema, which lists Draft/Submitted/Approved/Rejected/Journalled/Posted as distinct values. The spender sees the rejection and comment, then takes an explicit `ReopenForEdit` action (matches the UI Nav Map already in ARCHITECTURE.md) which moves Rejected → Draft and increments Version. An instant auto-revert would silently discard the Rejected state the Guide's own schema expects to exist.
Alternatives: keep D-010 as originally written (instant revert) — rejected, contradicts source schema.
Status: Decided (self-review correction). Not Locked — still depends on A-002/A-003 client confirmation for the edit-lock semantics around it.

**D-011 — Reconciliation with pre-existing CONTEXT.md scaffold (2026-07-13).**
Context: the repo already contained a `CONTEXT.md` specifying FastAPI + PostgreSQL + JWT-then-Entra, which conflicted with D-001/D-002 as first proposed. Client resolved both conflicts directly:
  - Backend: .NET confirmed (overrides CONTEXT.md's FastAPI).
  - Persistence: Postgres is dev-only, SharePoint is production System of Record (clarifies, doesn't fully overturn, CONTEXT.md's "external services are mocked until integration" framing).
`CONTEXT.md` has been updated to match. Auth approach (JWT now, Entra later) was not contested and stands — see A-014 note in ASSUMPTIONS.md if that needs revisiting.
Status: Confirmed by client (2026-07-13).

---

## Milestone 0.3 — Application layer

**D-015 — No `IUnitOfWork` abstraction; repositories persist immediately on `AddAsync`/`UpdateAsync`.**
Reasoning: SharePoint has no cross-list transaction primitive (Header + Lines are separate lists, Guide §5.5), so a UnitOfWork interface would promise an atomicity guarantee the production adapter can't actually deliver. Introducing one now would be complexity in service of a false abstraction.
Consequence (documented, not solved): if a command's repository write succeeds but the subsequent `IAuditLogger.LogAsync` call fails, that transition goes unlogged — see the KNOWN LIMITATION note on `IAuditLogger`. An outbox pattern would close this properly; deferred until audit completeness is a hard requirement, not a best-effort one.
Status: Decided.

**D-016 — Mutating commands never accept an identity field (e.g. SpenderId) in their payload; identity is always resolved server-side from `ICurrentUserContext`.**
Reasoning: accepting identity as client input on `CreateDraftSettlementCommand` would let a buggy or malicious client create a settlement "as" someone else. The server is the only source of truth for who is making the request — this was a self-review correction made while writing the handler, not the first draft of the design.
Status: Decided.

**D-017 — Hand-rolled `ICommand`/`ICommandHandler`/`IQuery`/`IQueryHandler` interfaces instead of adopting MediatR.**
Reasoning: with ~9 commands and 2 queries, a mediator/dispatch-pipeline library is complexity with no current payoff — DI registration explicitly maps each command to its handler, which is easy to read and step through. Revisit only if the use-case count grows enough that a pipeline (cross-cutting behaviors like logging/validation-as-middleware) earns its keep.
Alternatives: MediatR — rejected for now on unnecessary-complexity grounds, not ruled out permanently.
Status: Decided.

**D-018 — `RecordJournalCommand` is idempotent: the same JournalBatchNumber on an already-Journalled settlement is a no-op success; a different one throws.**
Reasoning: this is the concrete implementation of D-008 (RequestId/Version as idempotency key) applied to the journal-writeback callback specifically — a retried Power Automate flow run (Guide §6.4's try/catch + resubmission) must not fail or attempt a duplicate transition, but a genuinely different journal number reported for the same settlement is a real conflict, not a safe retry, and must still surface as an error.
Status: Decided. Covered by `RecordJournalCommandHandlerTests`.

**D-019 — CategoryMapping and AppUserProfile are Application-level read models (plain records returned by repository interfaces), not Domain entities.**
Reasoning: neither carries business invariants or behavior of its own — they're Finance/IT-maintained reference data (Guide §5.2/§5.5). Modeling them as Domain aggregates would add ceremony with no business rules to enforce; Application resolves them and snapshots the relevant fields onto SettlementLine at write time (reinforces D-004).
Status: Decided.

**D-020 — Photo upload is NOT implemented this milestone; `IPhotoStore` exists as an interface, but no `UploadReceiptPhotoCommand` is wired to it.**
Reasoning: Settlement/SettlementLine (frozen after Milestone 0.2) has no ReceiptPhoto concept, so there's currently nowhere in the aggregate to persist a photo's StorageRef. Building a workaround (e.g. a side-channel dictionary keyed by LineId) to avoid touching frozen Domain code would be exactly the kind of hacky patch Clean Architecture is meant to prevent. This needs an explicit decision on whether ReceiptPhoto belongs inside the Settlement aggregate's consistency boundary — logged as ASSUMPTIONS.md A-016, not silently worked around.
Status: Decided (deferral). Blocks photo upload specifically, not Milestone 0.4 generally.

---

## Sprint 4 — Infrastructure (Development)

**D-021 — Frozen-Domain change: `SettlementLine` gets a private parameterless constructor and `LineId` changes from get-only to a private setter.**
All five conditions for touching a frozen layer are met, checked explicitly:
1. *Required by a technical limitation:* EF Core cannot materialize an entity from `internal SettlementLine(int lineNo, ..., decimal vatRatePercent, bool kmRequired, ...)` — `vatRatePercent`/`kmRequired` are transient inputs, not stored columns, so no constructor-parameter binding is possible. `LineId` with no setter at all (not even private) gives EF no way to write a value back to it either.
2. *Business behavior unchanged:* no validation rule, state transition, or public method signature changed. `Apply()`'s invariants (category required, fuel lines need odometer, etc.) still run on every domain-driven mutation exactly as before.
3. *Additive/infrastructure-oriented:* a new private constructor plus loosening one property from get-only to a private setter — nothing removed, nothing public changed.
4. *Documented here.*
5. *Identified during Sprint 4 self-review*, in direct response to the deliverable "PostgreSQL implementation of all repository interfaces" being otherwise unachievable.
This mirrors the exact precedent already accepted for `Settlement`'s own private constructor in Milestone 0.2 ("Rehydration always restores an already-valid state, so bypassing the factory's validation here is safe") — not a new kind of compromise, the same one applied consistently to the child entity that was missed the first time.
Status: Approved by client, applied.

**D-022 — Settlement.Lines is mapped as an EF "owned collection" (OwnsMany), not a normal `DbSet<SettlementLine>` with a `HasMany`/`WithOne` relationship.**
Reasoning: SettlementLine has its own identity (LineId) but, per its own class-level Domain doc comment, is "not independently addressable" — it's never queried or persisted outside its owning Settlement. That is precisely what EF's owned-entity concept models (supports keyed entities since EF Core 5, not just simple value-object-style owned types). There is deliberately no `DbSet<SettlementLine>` anywhere; the aggregate boundary in code matches the aggregate boundary in the schema.
Status: Decided.

**D-023 — Optimistic concurrency uses Postgres's native `xmin` system column, not an application-level `RowVersion` column.**
Reasoning: ARCHITECTURE.md's original schema sketch (§5, written at Milestone 0.1) proposed an explicit `RowVersion` field for this purpose, but Domain (Milestone 0.2) never actually added one — only `AggregateRoot.Version`, which is the business reject/resubmit counter (A-002/A-003), not a concurrency token, and conflating the two would be wrong. `xmin` gives real optimistic concurrency at zero Domain cost and zero extra column to maintain. `DbUpdateConcurrencyException` is caught in `PostgresSettlementRepository.UpdateAsync` and re-thrown as `PettyCash.Application.Exceptions.ConcurrencyException`, so nothing above Infrastructure ever sees an EF/Npgsql-specific exception type.
**API correction (post-build-verification, same day):** `UseXminAsConcurrencyToken()` — originally used here — was obsoleted in Npgsql.EntityFrameworkCore.PostgreSQL 7.0 and does not exist in 9.0.4 (the version this project pins), causing a compilation error. Replaced with Npgsql's own currently-documented mechanism: a shadow `uint` property named `xmin`, configured via standard EF Core Fluent API (`HasColumnType("xid")`, `ValueGeneratedOnAddOrUpdate()`, `IsConcurrencyToken()`) rather than the removed provider-specific extension method. Same outcome (real xmin-based concurrency), current supported API, not a custom workaround — verified against Npgsql's own concurrency-tokens documentation before applying.
Alternatives considered: an explicit `RowVersion`/`byte[]` column as originally sketched — rejected as redundant with what Postgres already provides for free.
Consequence for the future SharePoint adapter: SharePoint's `__etag` serves the same role natively — both adapters get real optimistic concurrency without Domain needing to model either mechanism, consistent with D-002's "same interface, different native mechanism per store" intent.
Status: Decided, corrected.
**Migration detail (confirmed via npgsql/efcore.pg#145, #3270):** EF's migration generator would normally emit `AddColumn<uint>("xmin", type: "xid", ...)` for this shadow property — that operation is deliberately omitted from `InitialCreate.Up()`, since `xmin` already exists as an implicit Postgres system column and adding it fails with `42701`. Model metadata (Designer/Snapshot) still describes the property for EF's own concurrency tracking; no DDL targets it.

**D-024 — `CategoryMappingReadModel`/`AppUserProfileReadModel`/`AuditLogEntry` (Application-layer records, D-019/A-013) are mapped directly as EF entity types — no parallel Infrastructure-only DTO classes were created for them.**
Reasoning: `ICategoryMappingRepository`/`IAppUserProfileRepository` are read-only interfaces (no Add/Update methods), and all three records' constructor parameter names already match their property names, so EF's constructor-parameter binding materializes them directly — a separate Infrastructure entity class plus mapping code would be pure duplication with no behavioral benefit. `AuditLogEntry` gets a shadow-property `Id` (EF-only, not on the record) for its primary key, configured entirely in `AuditLogEntryConfiguration` without touching the Application-layer type.
Status: Decided.

**D-025 — `ICurrentUserContext` and `IPhotoStore` are NOT registered by `AddInfrastructure()`.**
Reasoning: `ICurrentUserContext` needs real authentication (explicitly out of scope this sprint — stop condition excludes "Authentication implementation"). `IPhotoStore` needs either SharePoint/Graph (explicitly excluded this sprint) or a dev-only object-store choice that's premature while A-016 (whether ReceiptPhoto belongs in the aggregate at all) is still unresolved. A host wiring `AddApplication()` + `AddInfrastructure()` today has an incomplete DI container for those two interfaces specifically — expected and documented, not a bug to chase down mid-sprint.
Status: Decided (scope boundary, not a technical limitation).

**D-026 — Each repository method (`AddAsync`/`UpdateAsync`) wraps its own explicit Postgres transaction; no cross-method or cross-repository transaction was introduced, even though Postgres could technically support one (e.g. spanning a Settlement update and its audit log write).**
Reasoning: extending atomicity to cover `IAuditLogger`'s separate `SaveChangesAsync` call (the gap documented on `IAuditLogger` since Milestone 0.3, D-015) would make the Postgres adapter's consistency guarantees strictly stronger than the future SharePoint adapter can ever offer — SharePoint has no cross-list transaction primitive at all. That would violate the "switching to SharePoint requires only a DI change" goal by giving dev and prod genuinely different failure behavior, not just a different storage engine. The known audit-ordering gap remains open by design, not fixed opportunistically just because Postgres makes it easy.
Status: Decided.

**D-027 — The full parameterized contract-test suite envisioned in D-002 ("both implementations must satisfy the same repository interfaces" — i.e. one shared, reusable test suite run against both Postgres and SharePoint) is deferred until the SharePoint adapter exists.**
Reasoning: writing a generic shared contract suite against only one implementation risks encoding Postgres-specific assumptions into what should be storage-agnostic tests, since there's nothing yet to check those assumptions against. This sprint's `PettyCash.Infrastructure.Tests` instead directly verifies the Postgres adapter's behavior against each Application interface's documented contract (round-trip, not-found returns null, concurrency conflict throws `ConcurrencyException`, etc.) — substantively the same confidence, without the risk of a premature abstraction.
Status: Decided (refines D-002's original plan, does not contradict it).

**D-028 — NU1605 fix: introduced Central Package Management (`backend/Directory.Packages.props`), all EF Core/Npgsql/Microsoft.Extensions packages aligned to exactly 9.0.4.**
*Root cause:* `PettyCash.Infrastructure.csproj` pinned `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.DependencyInjection.Abstractions`, and `Microsoft.Extensions.Configuration.Abstractions` at `9.0.0`, while `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4` (already correctly pinned) transitively requires `9.0.4`+ of those same packages. NuGet correctly refused to silently satisfy an explicit `9.0.0` reference with a resolved `9.0.4` — that's what NU1605 exists to catch, and restore stopping before compilation was the check working as intended, not a bug to route around.
*Versioning strategy chosen:* rather than bumping only the three reported packages, EVERY package in the EF Core/Npgsql/Microsoft.Extensions family across the whole solution was aligned to exactly `9.0.4` via Central Package Management (`ManagePackageVersionsCentrally` + `CentralPackageTransitivePinningEnabled`, both `true`). Reasoning:
  - Npgsql.EntityFrameworkCore.PostgreSQL tracks Microsoft.EntityFrameworkCore's version 1:1 within the 9.x line — these aren't independently versionable packages, they're a matched set, so "just bump the three reported ones" would have left the underlying coupling undocumented and likely to break again on the next package addition.
  - `CentralPackageTransitivePinningEnabled=true` doesn't just fix today's conflict — it pins transitive versions solution-wide going forward, so a future new package that happens to pull a different transitive Microsoft.Extensions.* version gets caught at restore time against one source of truth, not discovered as a build failure later.
  - No `<NoWarn>NU1605</NoWarn>` or `TreatWarningsAsErrors` weakening was used anywhere — the fix is a real version alignment, not a suppressed check. NU1605 remains a hard error by default, exactly as before.
*Deliberately NOT aligned to 9.0.4* (different ecosystems, no shared transitive dependency on the EF Core 9.0.4 line): `FluentValidation`/`FluentValidation.DependencyInjectionExtensions` (11.10.0), `Microsoft.NET.Test.Sdk`/`xunit`/`xunit.runner.visualstudio` (test tooling), `Testcontainers.PostgreSql` (3.10.0). Forcing these onto an unrelated version number would add churn with no conflict to resolve.
Status: Decided, applied. See TECH_STACK.md for the locked version list.

**Correction (same day, second build-verification round):** a follow-up NU1605-equivalent conflict surfaced on `Microsoft.EntityFrameworkCore.Relational` resolving as both 9.0.1 and 9.0.4. Traced the actual dependency floors (verified against nuget.org's own listing for each package, not assumed):
- `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4` → `Microsoft.EntityFrameworkCore.Relational (>= 9.0.1 && < 10.0.0)` — a looser floor than this decision originally assumed ("Npgsql tracks EF Core 1:1" was true for the packages actually declared here, but Npgsql's own stated minimum for Relational specifically is 9.0.1, not 9.0.4).
- `Microsoft.EntityFrameworkCore.Design 9.0.4` → `Microsoft.EntityFrameworkCore.Relational (>= 9.0.4)`.
`Microsoft.EntityFrameworkCore.Relational` is never referenced directly by this solution — it's transitive-only. It had no explicit `PackageVersion` entry of its own in `Directory.Packages.props`, so with two different direct dependencies supplying two different floors, resolution wasn't unified across the graph. Fixed by adding an explicit `<PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="9.0.4" />` — completing central pinning for a package that was already implicated in the graph, not overriding or downgrading anything (9.0.4 was already the higher of the two floors).

---

## Sprint 5 — API Foundation

**D-029 — `Microsoft.AspNetCore.OpenApi` (built-in), not Swashbuckle.**
Reasoning: verified against current Microsoft documentation before choosing — Swashbuckle.AspNetCore stopped being the ASP.NET Core project-template default starting .NET 9, replaced by the first-party `Microsoft.AspNetCore.OpenApi` package (`AddOpenApi()`/`MapOpenApi()`). Using the current default rather than the previously-standard third-party package is what "current ASP.NET Core best practices" (the sprint's own wording) means concretely here.
Status: Decided.

**D-030 — `Asp.Versioning.Http` pinned at `8.1.0`, not the newer `10.0.0`.**
Reasoning: verified via Microsoft's own .NET Blog before pinning — Asp.Versioning 10.0.0 is purpose-built for .NET 10's OpenAPI pipeline; 8.x is the line that targets .NET 8/9. Picking the newest available version without checking its target framework would have repeated exactly the kind of unverified-assumption mistake that cost three fix-rounds in Sprint 4.
Consequence: per-API-version OpenAPI documents are NOT wired up (Asp.Versioning-to-built-in-OpenAPI integration wasn't shipped until the 10.0.0/.NET 10 pairing) — acceptable now since zero endpoints exist to document per-version yet.
Status: Decided.

**D-031 — `GlobalExceptionHandler` matches Domain exceptions by namespace string, not by importing `PettyCash.Domain.Exceptions`.**
Reasoning: Sprint 5's explicit rule is Api references only Application and Infrastructure, never Domain directly. A `using PettyCash.Domain.Exceptions;` plus a `DomainException` pattern match would have compiled fine (Domain types are transitively visible through Application's own project reference) but would violate the rule's intent — Api code would be naming and depending on a Domain type. Caught during this sprint's own self-review (first draft had the import), fixed before considering the sprint done.
Status: Decided.

**D-032 — `TreatWarningsAsErrors` enabled on `PettyCash.Api.csproj` only, not solution-wide via `Directory.Build.props`.**
Reasoning: Domain/Application/Infrastructure had just been verified as a fully passing, frozen build (112 tests) when this sprint started. Retroactively applying a stricter compiler setting solution-wide risks surfacing a latent warning-turned-error in already-approved code with no way for this session to see or fix it (no local build access). Scoping the setting to the new project honors "keep WarningsAsErrors enabled" for everything being written now without gambling with already-approved work. Revisit solution-wide once the client can confirm frozen projects have zero warnings.
Status: Decided.

**D-033 — No Serilog or other structured-logging library; `AddJsonConsole()` (built into `Microsoft.Extensions.Logging.Console`, ships with `Microsoft.NET.Sdk.Web`) used instead.**
Reasoning: satisfies ARCHITECTURE.md §11's "structured (JSON)" logging requirement with zero new NuGet packages, given how much version-resolution friction Sprint 4 hit from package additions. Revisit if a real need for file/Seq/other sinks emerges — not a permanent architectural ceiling, just the minimal choice for a foundation sprint.
Status: Decided.

**D-034 — Health check verifies real Postgres connectivity (`AspNetCore.HealthChecks.NpgSql`) against the same `PettyCashDev` connection string Infrastructure uses, rather than a bare "process is alive" check.**
Reasoning: a health check that only confirms the process is running gives false confidence in a system whose main external dependency (the database) is exactly what's most likely to fail independently of the process itself.
Status: Decided.

**D-035 — `DevelopmentCurrentUserContext` (in `PettyCash.Api/Development/`) registered for `ICurrentUserContext`, gated strictly to `builder.Environment.IsDevelopment()`, not a general-purpose fallback.**
Reasoning: D-025 deliberately left `ICurrentUserContext` unregistered by `AddInfrastructure()` pending real authentication. That correctly surfaced as a `dotnet run` failure — ASP.NET Core's default `ValidateOnBuild`/`ValidateScopes` checks (which only run in Development) caught the missing registration at startup. The fix registers one fixed, hard-coded `CurrentUser` (Spender role) behind the existing `ICurrentUserContext` interface, only inside the same `IsDevelopment()` branch already used elsewhere in `Program.cs`. No Domain or Application change: the interface Application already depends on is unchanged, so this is a pure Infrastructure-adjacent/Api composition addition, not a contract change. Production remains correctly unresolvable for this interface until real auth (JWT/Entra, A-014) is registered — that gap should keep failing loudly outside Development rather than being silently papered over.
Status: Decided.

**D-036 — No Swagger UI is exposed at any route, including `/swagger`; `/openapi/v1.json` (built-in `Microsoft.AspNetCore.OpenApi`) is the only OpenAPI artifact this sprint. `/swagger` returning 404 is expected, not a defect.**
Reasoning: reconfirms D-029 concretely. `Swashbuckle.AspNetCore` (which is what conventionally serves an interactive UI at `/swagger`) is not referenced anywhere in `PettyCash.Api.csproj`, and no UI middleware is registered in `Program.cs` — only `AddOpenApi()`/`MapOpenApi()`, which serves the raw JSON document and nothing else. There is no route for Swagger UI to 404 *from* in the sense of "almost working" — it was never mapped. A future sprint can add a UI (Scalar is the natural first-party-adjacent choice once there's an endpoint worth visualizing, per D-029's own note) but that is new scope, not a Sprint 5.1 gap.
Status: Decided.

**D-037 — Added an explicit `Microsoft.EntityFrameworkCore.Design` `PackageReference` to `PettyCash.Api.csproj` (startup project), with the same `PrivateAssets="all"` / `IncludeAssets` pattern already used on Infrastructure's reference. No new central package version — resolves against the existing `9.0.4` pin (D-028).**
*Root cause:* `dotnet ef database update --project src\PettyCash.Infrastructure --startup-project src\PettyCash.Api` failed with "Your startup project 'PettyCash.Api' doesn't reference Microsoft.EntityFrameworkCore.Design." `PettyCash.Infrastructure.csproj` already references the Design package (needed since Sprint 4, to hand-author the `InitialCreate` migration), so it looked present in the graph. But that reference carries `PrivateAssets="all"`, which by design excludes it from flowing to any project that references Infrastructure via `ProjectReference` — including `PettyCash.Api`. `dotnet ef` resolves its design-time services against the *startup* project specifically, not the migrations project, so Infrastructure holding the package was never sufficient on its own; this was a latent gap since Sprint 5.1 created the Api project, only surfaced now that `dotnet ef database update` was actually run against it.
*Why not just remove `PrivateAssets="all"` from Infrastructure's existing reference instead:* that would leak the Design package (and its analyzer/build assets) into every project that references Infrastructure, including transitively into anything that later references Api — broader than necessary and contrary to Sprint 4's own stated intent for that reference (build/tooling only, not a leaking dependency). Adding the same narrowly-scoped reference directly to Api is the smaller, more correct fix: exactly one project gets it, exactly where the tool needs it.
*Scope check:* build/analyzer/design-time package only — `PrivateAssets="all"` means it contributes nothing to `PettyCash.Api`'s runtime dependency graph or public API. No `Program.cs`, DI, controller, or any other application code changed. Pure tooling fix.
Status: Decided, applied, and client-verified (2026-07-15): `dotnet ef database update` re-run confirmed `InitialCreate` migration applied successfully.

---

## Vertical Slice 1 — Create Draft Settlement

**D-038 — Bug fix: `DevelopmentCurrentUserContext.Current.UserId` changed from `"dev-local-user"` to `"spender.demo"`.**
*Root cause:* seed data (`AppUserProfileConfiguration`, Sprint 4) creates one `AppUserProfile` keyed `"spender.demo"`; `DevelopmentCurrentUserContext` (Sprint 5.1, D-035) independently hard-coded `"dev-local-user"`. Never cross-checked before because no endpoint existed to join them — `CreateDraftSettlementCommandHandler` is the first code path to call `IAppUserProfileRepository.GetByAppUserIdAsync(user.UserId)`. Unfixed, every local call throws `NotFoundException(AppUserProfile, "dev-local-user")`.
*Fix:* changed the dev-only stub's id to match the seeded row (the seed row models a real admin-maintained profile per Guide §5.2; the stub should conform to it, not vice versa). No Domain/Application change.
Status: Decided, applied. Found during Vertical Slice 1 self-review, before any client-side run.

**D-039 — `POST /api/v1/settlements` is a Minimal API (`MapPost`), not an MVC controller.**
Reasoning: no `AddControllers()` exists anywhere in `PettyCash.Api`; `Program.cs` has used the minimal hosting model exclusively since Sprint 5.1. Adding MVC infrastructure for one endpoint is unneeded surface — Minimal APIs already give typed DI parameters and OpenAPI metadata.
Consequence: the versioned route group (`/api/v{version:apiVersion}/settlements`) is built inside `SettlementsEndpoints.MapSettlementsEndpoints()` itself, keeping `Program.cs`'s call site a single line.
Alternatives: MVC controllers — deferred on the same grounds as D-017/D-033; revisit if endpoint count/complexity later justifies it.
Status: Decided.

**D-040 — Validation is invoked explicitly in the endpoint delegate (`IValidator<T>.ValidateAsync`), not via a generic pipeline/filter.**
Reasoning: `Application.Tests/Validators/CommandValidatorTests.cs` confirms validators are registered but never invoked by handlers — validation-before-handling is the caller's job by design, and Api is that caller. The endpoint validates and throws the existing `ValidationException`, reusing `GlobalExceptionHandler`'s 400/`errors` mapping rather than a second error shape.
Alternatives: a generic `IEndpointFilter` — deferred until a second command-backed endpoint makes the duplication real rather than hypothetical.
Status: Decided.

**D-041 — `AddInfrastructure()` DbContext registration changed from eager connection-string capture to lazy resolution via the `IServiceProvider` overload of `AddDbContext`.**
*Root cause:* the original registration captured the connection string into a local variable at `AddInfrastructure()` call time (`Program.cs` calls this during service registration) and closed over it in the `AddDbContext` options delegate. `WebApplicationFactory.ConfigureWebHost`/`ConfigureAppConfiguration` (used by `ApiWebApplicationFactory` in `PettyCash.Api.Tests`) runs *after* `Program.cs`'s service-registration phase — so any in-memory config override injected by the test factory was merged into `IConfiguration` too late to affect the already-captured string. All 4 new `PettyCash.Api.Tests` integration tests consequently hit `localhost:5432` (the local dev Postgres) instead of the isolated Testcontainers container.
*Fix:* switched to the `AddDbContext<PettyCashDbContext>((IServiceProvider sp, DbContextOptionsBuilder options) => ...)` overload. The delegate now calls `sp.GetRequiredService<IConfiguration>().GetConnectionString("PettyCashDev")` at DbContext-construction time (per scope) rather than once at registration time, so it sees the fully-composed `IConfiguration` — including any test override — each time a DbContext is resolved.
*Production behavior:* unchanged. Dev and production both read `ConnectionStrings:PettyCashDev` from `appsettings.json`/environment variables; only the timing of the read changed (lazy per-scope vs. eager once-at-startup). The `configuration` parameter on `AddInfrastructure(IServiceCollection, IConfiguration)` is now unused in the method body; its signature is intentionally left intact to avoid a breaking change at the two call sites (`Program.cs`, `PettyCash.Infrastructure.Tests`).
*Alternatives considered:* replacing the DbContext service registration entirely inside `ApiWebApplicationFactory` (remove-then-re-add pattern sometimes used in WebApplicationFactory setups) — rejected as a heavier workaround that treats the symptom rather than the actual capture-timing defect in `AddInfrastructure()` itself.
Status: Decided, applied, client-verified 2026-07-16.

---

## Vertical Slice 2 — Add Settlement Line

**D-042 — `POST /api/v1/settlements/{settlementId}/lines` returns `200 OK` with the updated `SettlementDto`, not `201 Created`.**
Reasoning: consistent with D-003 (a `SettlementLine` is not an independently addressable resource — it has no `GET /lines/{id}`, it only exists as part of its parent `Settlement`). `201 Created` implies a new addressable resource at a `Location`; there is none here, only a mutation of an existing one. `200 OK` with the full updated parent representation lets the client re-sync `TotalAmount`/`Lines` in one round trip, same as any other line-mutation endpoint will (Update/Remove line, Milestone 0.6).
Status: Decided.

**D-043 — Post-verification defect: `AddSettlementLineRequest.cs` reported as created but absent from disk; recreated and re-verified before resubmission.**
*Root cause:* a `create_file`-equivalent tool call in an earlier turn reported success but the file was not actually persisted — confirmed by both a directory listing (file absent) and a direct read attempt (`ENOENT`). Not a namespace, folder, or `.csproj`-inclusion issue: `PettyCash.Api.csproj` has no explicit `Compile Include/Exclude`, so any `.cs` file physically present under the project directory is picked up by the SDK's default globbing.
*Fix:* rewrote the file with identical content via a different file-write tool, then verified it was actually present with an explicit read-back and a fresh directory listing before telling the client it was fixed.
*Process note:* going forward, a file-creation report is not treated as proof of a file's existence when a build depends on it — verify with a read-back before reporting a fix as done, not just after the write call returns success.
Status: Decided, applied, client-verified 2026-07-16.
