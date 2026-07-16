# PROJECT_RULES.md — Petty Cash Settlement System

**Purpose:** governs *how* work is done on this project. `AI_HANDOFF.md` governs *where things currently stand*. Read both before starting any task. Nothing in this file overrides `docs/Developer-Guide.docx` (business source of truth) or an explicit client instruction.

_Last updated: 2026-07-15._

---

## 1. Architecture Rules

- **Clean Architecture**, four layers: Domain → Application → Infrastructure → Api (+ WebClient). Dependencies point inward only. See `ARCHITECTURE.md` §1.
- Domain has zero dependencies. Application depends on Domain only. Infrastructure implements Application's interfaces. Api depends on Application and Infrastructure only — **never Domain directly**, even though Domain types are transitively visible (see D-031: matching by namespace string in `GlobalExceptionHandler` instead of importing `PettyCash.Domain.Exceptions`, specifically to avoid this).
- All external systems — SharePoint, Microsoft Graph, Power Automate, D365FO, Entra, **and PostgreSQL** — sit behind Application-defined interfaces. PostgreSQL is not a privileged exception just because it's used in development (D-012, D-002).
- `Settlement` is the aggregate root; `SettlementLine` is a child entity, not independently addressable (D-003). Any change that would let a line be queried/persisted outside its owning Settlement needs an explicit decision, not a quiet workaround.
- Two repository implementations must exist for `ISettlementRepository` (Postgres dev, SharePoint prod) satisfying the same interface contract (D-002). A shared parameterized contract-test suite is deferred until the SharePoint adapter exists (D-027) — don't build it prematurely against only one implementation.

---

## 2. Layer Boundaries

Enforce these mechanically, not just by convention:

- **Domain**: no references to any other project or NuGet package beyond what's needed for pure C# (no EF, no ASP.NET Core, no Microsoft.Graph).
- **Application**: references Domain only, plus FluentValidation and `Microsoft.Extensions.DependencyInjection.Abstractions`. No ASP.NET Core, no EF Core, no Npgsql, no Microsoft.Graph, no SharePoint/D365FO/Power Automate package of any kind.
- **Infrastructure**: references Domain + Application + whatever adapter-specific packages it needs (EF Core/Npgsql for the Postgres adapter; Graph SDK/SharePoint for the future SharePoint adapter). Domain and Application must have zero reference back to Infrastructure — this should be structurally impossible, not just unused.
- **Api**: references Application + Infrastructure only. Verify this explicitly (check `.csproj` references) whenever adding a new dependency here, not just at project creation.
- Before adding any new package reference to a project, check whether it violates that project's allowed dependency set above. If it seems necessary, that's a signal to reconsider which layer it belongs in — not to relax the boundary.

---

## 3. Frozen-Layer Policy

Domain and Application are frozen (except bug fixes) once approved by the client (currently: since Milestone 0.2 and 0.3 respectively — see `AI_HANDOFF.md` §5).

A change to a frozen layer is only acceptable when **all five** of the following hold, and each must be checked and documented explicitly in `docs/DECISIONS.md` (the template is D-021):

1. **Required by a genuine technical limitation** — not convenience, not a nicer-looking API, not "easier to test."
2. **Business behavior is unchanged** — no validation rule, state transition, or public method signature changes.
3. **The change is additive or infrastructure-oriented** — nothing removed, nothing public changed in a breaking way.
4. **It is documented** in `docs/DECISIONS.md` with the reasoning above stated explicitly, referencing this checklist.
5. **It was identified during self-review of a specific, named deliverable** — not spot-fixed opportunistically while doing unrelated work.

If any of the five doesn't clearly hold: **stop and ask the client**, per the Developer Guide/root instructions rule against assuming ambiguous requirements. Do not proceed on the assumption that "it's probably fine."

---

## 4. Vertical Slice Development Workflow

A **vertical slice** is a complete, independently verifiable unit of work spanning exactly the layers it needs to (e.g., "Domain layer skeleton," "Postgres Infrastructure adapter," "API foundation") — matching the Milestone/Sprint granularity already used in `docs/TODO.md`.

For each slice:

1. **Explain the design briefly before implementing** (root instruction: "Before implementing any major feature, explain the design briefly"). State which layer(s) are touched and confirm none are frozen without following §3.
2. **Build incrementally.** One `.csproj` per architectural concern; every new `.csproj` is added to `backend/PettyCash.sln` in the same change that creates it (`ARCHITECTURE.md` §8) — not a follow-up step.
3. **Write or update tests as part of the same slice**, not after. No mocking `DbContext`/`DbSet` — use Testcontainers or equivalent real infrastructure where the whole point is catching ORM-mapping bugs (see TECH_STACK.md's Testing section rationale).
4. **Self-review before declaring done** — see §11.
5. **State explicitly what could not be verified in this session** (e.g., no execution access to the client's machine) rather than asserting success. Every prior sprint in this project's history has required the client to actually run `dotnet build`/`test`/`run` — assume that pattern continues unless told otherwise.
6. **Update documentation in the same turn the slice is verified complete** — see §6.
7. **Do not start the next slice until the current one is confirmed** by the client's own verification (§7), unless explicitly told to proceed provisionally.

---

## 5. Coding Conventions

- Strong typing throughout; no implicit `dynamic`/`object` where a concrete type is available.
- Centralized validation via FluentValidation, one validator per command (Application layer) — not scattered inline checks.
- Two-tier exception model (`ARCHITECTURE.md` §12): `PettyCash.Domain.Exceptions.DomainException` for business-rule violations (let propagate uncaught from handlers), `PettyCash.Application.Exceptions.AppException` hierarchy for orchestration failures (`NotFoundException`, `ForbiddenException`, `ValidationException`, `ConcurrencyException`). Do not collapse the two — a Domain rule violation and an authorization failure are different kinds of failure and must stay distinguishable to the Api layer.
- Identity is always server-resolved via `ICurrentUserContext` — never accept an identity field (e.g. `SpenderId`) as command input from a client (D-016).
- Centralize authorization in `ISettlementAuthorizationPolicy` — one implementation per ownership/role check, never inlined per-handler duplicate logic.
- No introduction of a dispatch/mediator library (e.g. MediatR) unless the use-case count grows enough to justify it (D-017) — don't add abstraction ahead of demonstrated need.
- Central Package Management (`backend/Directory.Packages.props`) for all NuGet versions — no per-`.csproj` `<Version>` attributes. Before adding or bumping any EF Core/Npgsql/Microsoft.Extensions package, check whether it's part of the matched 9.0.4 set (`TECH_STACK.md`) and keep that set aligned — do not bump one member independently (this exact mistake cost three fix-rounds in Sprint 4, D-028).
- Verify third-party API existence against current official docs before using it — do not assume a remembered API still exists (the `UseXminAsConcurrencyToken()` removal in D-023 is the cautionary example).

---

## 6. Documentation Update Rules

The following files must stay synchronized with actual, client-verified state — not with what was merely written this session:

| File | Updated when |
|---|---|
| `ARCHITECTURE.md` | Any structural change: new layer content, schema change, state machine change, new API endpoint, new decision that revises a previously-documented design (e.g. D-023 correcting §5's original RowVersion sketch). |
| `CHANGELOG.md` | Every sprint/milestone, and every post-verification fix within one — dated, one entry per fix, in the style already used (see Sprint 5.1's four rounds). |
| `docs/TODO.md` | Every milestone/sprint status change; every self-review correction gets logged inline where the milestone is described. |
| `docs/DECISIONS.md` | Every decision, with ID, reasoning, alternatives considered, status. Corrections to prior decisions are appended as dated notes under the original ID (see D-023's "API correction" and D-028's "Correction" pattern) — **never silently edit a past decision's original text out**. |
| `docs/ASSUMPTIONS.md` | Every new open item/risk; resolved items marked resolved, not deleted. |
| `AI_HANDOFF.md` | After every completed vertical slice — mandatory, see that file's §13 and this file's §12. |

Rule: if a fix or correction is made to something already shipped in an earlier sprint, it is logged as a **new, dated entry referencing the original** (in `CHANGELOG.md` and/or `DECISIONS.md`), not as a retroactive edit that erases the fact that a mistake occurred. The project's own history (D-023, D-028, D-037) is a deliberate audit trail — preserve that pattern.

---

## 7. Verification Requirements

- No milestone/sprint is "done" until the checklist in `AI_HANDOFF.md` §9 is completed **by the client, on the client's machine**. A clean self-review is necessary but not sufficient.
- If this session has no execution access to the client's environment (confirmed to be the case historically — migrations were hand-authored, builds were client-run), state that plainly rather than implying verification happened.
- 112 passing tests (or whatever the current count is per `AI_HANDOFF.md` §4) must be re-confirmed, not assumed carried-forward, whenever a change touches a tested layer.
- A sprint marked "implementation complete" is explicitly **not** the same status as "verified"/"closed" — use these words precisely in `CHANGELOG.md`/`docs/TODO.md`, matching the existing convention in this project's history.

---

## 8. Git Workflow

- Working branch: `develop` (see `AI_HANDOFF.md` §2 for current value — check it hasn't changed).
- Tags: `vMAJOR.MINOR.PATCH`, cut only after a milestone/sprint passes the full Verification Checklist.
- Every new `.csproj` added to `backend/PettyCash.sln` in the same commit/turn it's created.
- Small, reviewable commits per layer/concern — do not bundle unrelated layers or fixes into one change.
- No history rewriting on `develop`.
- This AI does not execute git commands on the client's machine directly (no confirmed access) — describe what should be committed/tagged and let the client perform it, unless a future session confirms direct execution access exists.

---

## 9. Defect Discovery Protocol

When a defect, gap, or stale fact is found (in code, docs, or a prior AI session's output):

1. **Do not silently patch and move on.** State what was found, why it's wrong, and what the fix is.
2. **Determine whether it's a documentation staleness issue or an actual code/design defect** — these get different treatment. A stale status line (e.g., a decision marked "not yet re-verified" after it actually was) is corrected as a documentation update with a note of when/how it was confirmed. An actual code defect follows the fix pattern already established in this project's history (root-cause stated, fix applied, alternatives-considered where relevant, logged in `CHANGELOG.md` + `DECISIONS.md`).
3. **Trace root cause before fixing**, per this project's own established pattern (D-023, D-028, D-037 all state root cause explicitly, verified against external documentation/source where relevant — not guessed).
4. **Never silently suppress a check to make a defect go away** (e.g., no `<NoWarn>`, no weakening `TreatWarningsAsErrors`, no skipping a failing test) — fix the actual cause, per D-028's explicit precedent.
5. **Log the correction** in the appropriate file(s) per §6, dated, referencing what was corrected and why.

---

## 10. Self-Review Checklist

Before declaring any slice/task complete, check:

- [ ] Does this change stay within the layer-boundary rules (§2)? Check actual project references, not just intent.
- [ ] Does this change touch a frozen layer? If yes, does it satisfy all five conditions in §3, documented?
- [ ] Are all new packages part of the correct central-version group, or independently versioned for a documented reason (§5, `TECH_STACK.md`)?
- [ ] Have exceptions been modeled through the correct tier (Domain vs. Application), not collapsed together?
- [ ] Is identity/authorization handled centrally, not duplicated inline?
- [ ] Have `CHANGELOG.md`, `docs/TODO.md`, `docs/DECISIONS.md` (and `ARCHITECTURE.md`/`ASSUMPTIONS.md` if applicable) been updated to reflect exactly what was done — no more, no less?
- [ ] Has `AI_HANDOFF.md` been updated if this completes a vertical slice (§12 below)?
- [ ] Is there anything in this change that could not actually be verified in this session? If so, is that stated explicitly rather than implied as done?
- [ ] Did this session invent any architecture, requirement, or business rule not present in `docs/Developer-Guide.docx`, `ARCHITECTURE.md`, `TECH_STACK.md`, or an explicit client instruction? If so, remove it and ask instead.

---

## 11. Resume Protocol

Follow `AI_HANDOFF.md` §11 exactly. In summary: read `AI_HANDOFF.md` first, then this file; reconcile `AI_HANDOFF.md`'s stated state against `CHANGELOG.md`/`docs/TODO.md`'s latest entries (the latter win on conflict); confirm the current task before writing any code; check frozen-layer status before touching Domain/Application.

---

## 12. Rules for Updating AI_HANDOFF.md

- Update `AI_HANDOFF.md` §13 (and the sections it lists: §3–§8) **in the same turn** a vertical slice is verified complete — not deferred to a later cleanup pass.
- Every update to §13 appends a dated line to that section's change log; never delete prior history from it.
- If a client correction arrives (e.g., "this was already re-verified," as happened with D-037), update `AI_HANDOFF.md` and `docs/DECISIONS.md` immediately, in the same response, before doing anything else requested in that message — stale status is treated as a defect (§9), not a low-priority cleanup.
- `AI_HANDOFF.md` must never claim a verification step succeeded unless the client explicitly confirmed it. If unconfirmed, say so plainly in §4/§9 rather than optimistically marking it done.

---

## 13. Rules for Keeping Documentation Synchronized

- Treat `docs/DECISIONS.md` as the single source of truth for *why*; `CHANGELOG.md` for *what changed, when*; `ARCHITECTURE.md` for *current structural state*; `docs/TODO.md` for *milestone progress*; `AI_HANDOFF.md` for *where a new session should pick up*. Do not duplicate long reasoning across files — cross-reference by ID/section instead (this project's existing convention).
- Whenever two documents could be read as disagreeing (e.g., `AI_HANDOFF.md` says "not yet re-verified" but `CHANGELOG.md` says it was), resolve the conflict immediately by checking which is more recent/authoritative, correct the stale one, and note the correction — do not leave the contradiction for a future session to discover.
- `CONTEXT.md` remains historical-only (superseded 2026-07-13) — never treat it as authoritative, and do not revive it as a live document.
- Any new decision that revises a previously-documented one is appended as a dated correction under the original decision ID, preserving the original text (see D-023, D-028 pattern) — this file's own §6 states the same rule; it is restated here because it is the most commonly missed convention.


##13. Rules Repository Is The Source of Truth

Never rely on previous AI conversations.

Always use:

- Git
- AI_HANDOFF.md
- PROJECT_RULES.md
- Repository documentation

as the authoritative project state.

If repository state conflicts with chat history, follow the repository.