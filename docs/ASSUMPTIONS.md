# Assumptions Register

Every assumption below unblocks a design decision. **[CONFIRM]** items should go to the business owner (Awab Al-Habal) before Phase 2 build starts; they don't block Phase 1 domain/API work.

| ID | Assumption | Risk | Confirm? |
|---|---|---|---|
| A-001 | Drafts are persisted incrementally (partial save allowed) under a single RequestId. | Low | No |
| A-002 | Settlements are locked (no line edits) once Status=Submitted, until/unless Rejected. | Medium | **[CONFIRM]** |
| A-003 | On Reject, settlement returns to Draft with same RequestId, Version increments; on resubmit, a new approval cycle starts. | Medium | **[CONFIRM]** |
| A-004 | Journal creation is idempotent, keyed on RequestId; a retried flow run must not create a duplicate journal. | Low | Confirm with Phase 2/flow owner during build |
| A-005 | Single currency (EGP) only; no multi-currency support in MVP. | Medium | **[CONFIRM]** |
| A-006 | No budget/over-settlement check against the original cash handout voucher amount — out of scope per Guide's phase table (control gap, not a bug). | **High** | **[CONFIRM]** |
| A-007 | No approver delegation/escalation/OOO handling in MVP; single ApproverEmail, no failover. | **High** | **[CONFIRM]** |
| A-008 | No approval SLA/timeout/reminder logic beyond native Teams/Outlook approval notifications. | Medium | **[CONFIRM]** |
| A-009 | No sequential/plausibility validation on odometer readings in MVP (deferred to Guide's "Future" phase). | Medium | **[CONFIRM]** |
| A-010 | AI receipt-parsing vendor not yet selected; feature flagged off by default, doesn't block MVP either way. | Low | No, revisit before enabling |
| A-011 | Finance Content Owner and App Admin are distinct roles even if the same person holds both at CANEX today. | Low | No |
| A-012 | PostgreSQL is dev-only; production storage is SharePoint (confirmed 2026-07-13, see DECISIONS D-002/D-011). | — | Confirmed, no longer open |
| A-013 | Audit trail (`AuditLogEntry`) is a new SharePoint list not specified in the original Guide schema — additive, no change to existing lists. | Low | No |
| A-014 | Auth is JWT-based initially, migrating to Entra External ID later (per original CONTEXT.md, not contested during D-011 reconciliation). | Medium | **[CONFIRM]** — worth revisiting now that .NET is confirmed, since Entra-from-day-one avoids a later migration of the identity layer |
| A-015 | Single legal entity / no cross-company or multi-branch settlement routing. | Medium | **[CONFIRM]** |
| A-016 | Receipt photos are tracked outside the Settlement aggregate's consistency boundary rather than as a ReceiptPhoto child entity on SettlementLine. Discovered as a gap at Milestone 0.3: `IPhotoStore` exists but no `UploadReceiptPhotoCommand` does yet, since Domain (frozen after 0.2) has nowhere to persist a StorageRef against a line. Needs an explicit decision — extend Domain in a dedicated change, or keep photo association fully outside the aggregate — before Milestone 0.4. | Medium | **[CONFIRM]** — needed before photo upload can be implemented |
| A-017 | Running `PettyCash.Infrastructure.Tests` requires Docker (or a Testcontainers-compatible runtime) available on the machine, since these tests spin up a real disposable Postgres container rather than using mocks (explicit Sprint 4 instruction). | Medium | **[CONFIRM]** — if the dev/CI machine can't run Docker, this test project needs an alternative (e.g. a pre-provisioned test database) before it can run there. |
| A-018 | EF Core migrations are not yet generated on disk — this session has no command-execution access to the user's machine, so `dotnet ef migrations add InitialCreate` could not be run. `PostgresContainerFixture` fails with an explicit, actionable message (not a raw Postgres error) if migrations are missing when tests run. | Medium | **Resolved** — InitialCreate migration hand-authored and reviewed (no execution path exists anywhere in this environment); pending client's `dotnet build`/`dotnet test` for final confirmation. |

**High-risk items requiring sign-off before Phase 2 build begins:** A-006, A-007. Both are financial-control gaps, not engineering shortcuts.
**Worth a second look given confirmed .NET stack:** A-014 — building JWT auth now and migrating to Entra later means writing and later discarding an identity layer; going straight to Entra External ID may be cheaper overall. Flagging, not deciding unilaterally.
**Blocks a specific upcoming feature:** A-016 — photo upload (Guide §5.3/5.6) cannot be fully implemented until this is resolved. Not a blocker for the rest of Milestone 0.3 or 0.4, since every other use case is independent of it.
**Blocks Infrastructure test execution specifically:** A-017 (Docker), A-018 (migration generation) — both are one-time environment/setup steps, not architectural gaps.
