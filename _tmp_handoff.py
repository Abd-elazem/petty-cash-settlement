path = r'D:\petty-cash-settlement\AI_HANDOFF.md'
with open(path, 'r', encoding='utf-8-sig') as f:
    content = f.read()

# Update last-updated header
old_header = '_Last updated: 2026-07-21 (Migration 20260721092741 finalised \u2014 snapshot seed data corrected; model/migrations/snapshot/HasData fully consistent; awaiting client dotnet build + dotnet ef database update + dotnet test verification)._'
new_header = '_Last updated: 2026-07-21 (VS12 Update Settlement Header implemented \u2014 backend + frontend + integration tests; awaiting client verification)._'
assert old_header in content, 'Header not found'
content = content.replace(old_header, new_header, 1)

# Update roadmap to add VS12
old_roadmap_line = '8. SharePoint + Entra production adapters'
new_roadmap_line = '7l. **Vertical Slice 12 \u2014 Update Settlement Header** \u2014 implemented, awaiting client verification\n8. SharePoint + Entra production adapters'
assert old_roadmap_line in content, 'Roadmap anchor not found'
content = content.replace(old_roadmap_line, new_roadmap_line, 1)

# Update current task section
old_task = '## 7. Current Task\n\n**Migration 20260721092741 finalised (2026-07-21) \u2014 awaiting client verification.**'
new_task = (
    '## 7. Current Task\n\n'
    '**Vertical Slice 12 \u2014 Update Settlement Header (2026-07-21): IMPLEMENTATION COMPLETE. Awaiting client verification.**\n\n'
    '`PUT /api/v1/settlements/{settlementId}` \u2014 updates `SettlementDate` and `Purpose` on a Draft settlement. Reuses `Settlement.UpdateHeader()` (already present in Domain from a prior frozen-layer exception D-044). No Domain change this session.\n\n'
    '**Files created this session:**\n'
    '- `backend/src/PettyCash.Application/Settlements/Commands/UpdateSettlementHeaderCommand.cs` (command record + validator + handler)\n'
    '- `backend/src/PettyCash.Api/Endpoints/UpdateSettlementHeaderRequest.cs` (wire DTO)\n'
    '- `backend/tests/PettyCash.Api.Tests/Settlements/UpdateSettlementHeaderEndpointTests.cs` (6 integration tests)\n'
    '- `frontend/src/features/settlements/hooks/useUpdateSettlementHeader.ts` (mutation hook)\n'
    '- `frontend/src/features/settlements/components/EditSettlementHeaderForm.tsx` (inline edit form with client-side validation)\n\n'
    '**Files modified this session:**\n'
    '- `backend/src/PettyCash.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs` (registered `UpdateSettlementHeaderCommandHandler`)\n'
    '- `backend/src/PettyCash.Api/Endpoints/SettlementsEndpoints.cs` (added `MapPut("/{settlementId:guid}", UpdateHeaderAsync)` + private method)\n'
    '- `frontend/src/types/settlements.ts` (added `UpdateSettlementHeaderRequest` type)\n'
    '- `frontend/src/api/settlementsClient.ts` (added `updateHeader` method)\n'
    '- `frontend/src/pages/SettlementDetailPage.tsx` (wired hook + `EditSettlementHeaderForm`, "Edit Header" button gated on `isDraft`)\n\n'
    '**Not yet run:** `dotnet build`, `dotnet test`, `npm run build` \u2014 no execution access this session. Awaiting client verification checklist (\u00a79).\n\n'
    '**Prior task note:** Migration `20260721103939_AddCategoryDisplayNameAndNullableDimensions` is still pending client `dotnet ef database update` verification from the previous session \u2014 that must also pass before VS12 can be considered fully integrated.'
)
assert old_task in content, 'Task section not found'
content = content.replace(old_task, new_task, 1)

# Append to changelog section in AI_HANDOFF.md §13
old_log_line = '- 2026-07-21 \u2014 Snapshot seed data corrected: previous session\'s edit had incorrectly removed `DimensionDefaults` from `OFFICE_SUPPLIES` and `GOVERNMENT_FEES` snapshot seed rows based on a misreading of the constructor argument order. Re-verification against `ReferenceData.cs` and `CategoryMappingConfiguration.HasData` confirmed all three rows have non-null `DimensionDefaults`; snapshot restored to include them. No `UpdateData` needed in the migration (database rows written by `InitialCreate` are already correct). Model/migrations/snapshot/HasData now fully consistent. Awaiting client `dotnet restore && dotnet build && dotnet ef database update && dotnet test` verification.'
new_log_line = (
    old_log_line + '\n'
    '- 2026-07-21 \u2014 VS12 (Update Settlement Header) implemented: `UpdateSettlementHeaderCommand`/Validator/Handler (Application), DI registration, `UpdateSettlementHeaderRequest` DTO, `PUT /{settlementId:guid}` endpoint (Api), 6 integration tests (Api.Tests), `useUpdateSettlementHeader` hook, `EditSettlementHeaderForm` component, and `SettlementDetailPage` integration (frontend). No Domain change \u2014 `Settlement.UpdateHeader()` was already present. Implementation complete, not yet client-verified. \u00a74/\u00a76/\u00a77 updated; last-updated header updated.'
)
assert old_log_line in content, 'Log line not found'
content = content.replace(old_log_line, new_log_line, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('OK')
