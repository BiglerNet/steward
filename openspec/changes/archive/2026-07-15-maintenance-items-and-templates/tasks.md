## 1. Domain entities (Domain)

- [x] 1.1 Add `MaintenanceItem`, `ChecklistItem`, `PartLine`, `Part` entities per `specs/domain-model/spec.md`'s MODIFIED "Cross-cutting tracking entities" requirement.
- [x] 1.2 Add `Template`, `TemplateStep` entities per the ADDED "Template entity" requirement, including the `SuggestedParts` value shape (`{ name, quantity }`). **Reworked, see §13**: `TemplateStep.SuggestedParts` is now a `List<TemplateStepSuggestedPart>` navigation collection (child entity), not a `List<SuggestedPart>` value type.
- [x] 1.3 Add `MaintenanceItemStatus` (`Planned|InProgress|Done|Cancelled`), `ChecklistItemStatus` (`Open|Done|Skipped`), `PartLineStatus` (`Needed|Ordered|Received`) enums, persisted as strings per this repo's enum-storage convention.
- [x] 1.4 Remove the `ServiceRecord` entity.

## 2. EF Core configuration and migration (Infrastructure)

- [x] 2.1 Add `MaintenanceItemConfiguration`, `ChecklistItemConfiguration`, `PartLineConfiguration`, `PartConfiguration`, `TemplateConfiguration`, `TemplateStepConfiguration` — FK relationships, no `HasMaxLength` on `Description`/`Title` (unbounded `text`, consistent with existing convention), `ApplicableCategories` as a native Postgres array column, `SuggestedParts` as `jsonb`. **Reworked, see §13**: added `TemplateStepSuggestedPartConfiguration` (plain FK, `OnDelete(Cascade)`); `TemplateStepConfiguration.cs` no longer has any `HasConversion`/`ValueComparer`/`jsonb` mapping. The other five configurations and `ApplicableCategories` as an array are unaffected.
- [x] 2.2 Remove `ServiceRecordConfiguration` and the `ServiceRecords` table mapping.
- [x] 2.3 Reset migrations: delete existing migrations, regenerate a single `InitialCreate` including all new tables and the removed `ServiceRecords` table (per this project's pre-launch migration-reset convention). **Reworked, see §13**: regenerated again to reflect the `TemplateStepSuggestedParts` table in place of the `jsonb` column.
- [x] 2.4 Add a startup/migration seeder for built-in platform templates (idempotent — checks for existing seeded templates by a stable identifier before inserting).

## 3. Application layer: maintenance items (Application)

- [x] 3.1 `Steward.Application/Tracking/MaintenanceItems/Dtos.cs` — `MaintenanceItemResponse` (including computed `isBlocked`, nested `checklistItems`, `partLines`), `CreateMaintenanceItemRequest`, `PatchMaintenanceItemRequest`, `ChecklistItemResponse`/`CreateChecklistItemRequest`/`PatchChecklistItemRequest`, `PartLineResponse`/`CreatePartLineRequest`/`PatchPartLineRequest`.
- [x] 3.2 `IMaintenanceItemService.cs` — CRUD + checklist CRUD/reorder + part-line CRUD method signatures.
- [x] 3.3 `Validators.cs` — FluentValidation rules per `specs/maintenance-items/spec.md` (required `title`; cross-entity checks — `engineId` belongs to the asset, `checklistItemId` belongs to the same item, reorder is a permutation of existing ids — enforced in the Infrastructure service layer instead, mirroring `ServiceRecordService`'s `EnsureEngineBelongsToAssetAsync`, since Application-layer validators have no DB access per this repo's Clean Architecture boundary).

## 4. Application layer: templates (Application)

- [x] 4.1 `Steward.Application/Tracking/Templates/Dtos.cs` — `TemplateResponse`, `TemplateStepResponse`, create/patch requests for templates and steps, `DuplicateTemplateRequest`.
- [x] 4.2 `ITemplateService.cs` — household template CRUD/step CRUD/reorder/duplicate, platform template CRUD/step CRUD/reorder, platform catalog listing.
- [x] 4.3 `Validators.cs` — required `title`; `platformTemplateId` on duplicate must reference a template with `householdId = null` (enforced in the service layer, same reasoning as 3.3).

## 5. Infrastructure services (Infrastructure)

- [x] 5.1 `MaintenanceItemService.cs` implementing `IMaintenanceItemService`, including the derived `isBlocked` computation and the template-application expansion logic (per-engine expansion for `engineScoped` steps against the asset's `Active` engines, asset-category applicability check, suggested-parts copy). **Reworked, see §13**: the suggested-parts copy step now reads from the `TemplateStepSuggestedPart` child-entity collection (via an added `.ThenInclude`), ordered by `SortOrder`; the rest of this task is unaffected.
- [x] 5.2 `TemplateService.cs` implementing `ITemplateService`, including the household/platform branching and deep-copy duplicate logic. **Reworked, see §13**: the step duplicate logic's deep-copy of `SuggestedParts` now copies `TemplateStepSuggestedPart` child rows (fresh ids) instead of a JSON blob; the rest of this task is unaffected.
- [x] 5.3 Remove `ServiceRecordService` and its EF configuration.
- [x] 5.4 Add `AddStewardMaintenance` extension method (mirroring `AddStewardTracking`) registering the new services; wire it into `Program.cs` in place of the old service-record registration.

## 6. Authorization (Infrastructure)

- [x] 6.1 `MaintenanceItem`/`ChecklistItem`/`PartLine` and household-owned `Template` are authorized by resolving the owning household id (via `assetService.GetHouseholdIdForAssetAsync` for the asset-nested resources, directly from the route for `Template`) and reusing the existing `HouseholdResource(householdId)` + `HouseholdAuthorizationHandler` — no new wrapper classes needed, this is exactly the pattern `ServiceRecordsController`/`MileageLogsController` already use.
- [x] 6.2 Platform template authorization implemented as attribute-based role gating: `AdminTemplatesController` (`api/admin/templates/...`) carries `[Authorize(Roles = "PlatformAdmin")]` on the whole controller (mirrors `PlatformAdminController`), and the read-only `GET /api/templates/platform` catalog endpoint (`PlatformTemplateCatalogController`) carries plain `[Authorize]` (any authenticated user, no household check). No new `IAuthorizationRequirement`/handler was needed — attribute-based gating fully expresses the spec's rule and matches this codebase's existing admin-endpoint convention.

## 7. API controllers (Api)

- [x] 7.1 `MaintenanceItemsController` — all endpoints from `specs/maintenance-items/spec.md` (create, list, get, patch, delete, checklist-item CRUD + reorder, part-line CRUD), versioned consistent with existing controllers.
- [x] 7.2 `TemplatesController` (household-scoped: `/api/households/{householdId}/templates...`) and `AdminTemplatesController` (`/api/admin/templates...`) per `specs/maintenance-templates/spec.md`; `GET /api/templates/platform` as its own lightweight endpoint (any authenticated user) via `PlatformTemplateCatalogController`.
- [x] 7.3 Update `DashboardService`'s `RecentActivity` query to source from `MaintenanceItem` rows with `Status = Done` (mapping `Title` → `description`, `Date` → `performedOn`) instead of `ServiceRecord`.
- [x] 7.4 Remove `ServiceRecordsController`.
- [x] 7.5 Regenerate the OpenAPI-derived frontend types (`npm run generate:api`) once the API is stable.

## 8. Backend tests (UnitTests / IntegrationTests)

- [x] 8.1 Unit tests for template-application expansion logic (engine-scoped fan-out, retired-engine exclusion, category-applicability rejection, suggested-parts copy). **Reworked, see §13**: the four expansion tests remain in `tests/Steward.IntegrationTests/Maintenance/MaintenanceItemServiceLogicTests.cs` — they all persist a `Template` with `ApplicableCategories` (native Postgres array), which still isn't model-validatable under EF Core's InMemory provider, so this logic still needs a real Postgres schema regardless of the `SuggestedParts` rework.
- [x] 8.2 Unit tests for `isBlocked` derivation. **Reworked, see §13**: moved to `tests/Steward.UnitTests/Maintenance/MaintenanceItemServiceTests.cs` — `isBlocked` never touches `Template`/`ApplicableCategories`, so now that `SuggestedParts` is off `jsonb` these tests run fine against EF Core's InMemory provider (with `InMemoryEventId.TransactionIgnoredWarning` ignored, since `CreateAsync`'s unconditional transaction isn't supported by the InMemory store).
- [x] 8.3 Integration tests for each `maintenance-items` endpoint's authorization scenarios (Contributor/Owner/Viewer/non-member), mirroring the existing `service-record-tracking` integration test structure. `tests/Steward.IntegrationTests/Maintenance/MaintenanceItemsControllerTests.cs`.
- [x] 8.4 Integration tests for platform-template authorization (non-admin 403 on admin endpoints, any authenticated user can read the platform catalog). `tests/Steward.IntegrationTests/Maintenance/TemplatesControllerTests.cs`.
- [x] 8.5 Integration test for the household template "duplicate" endpoint producing a fully independent copy. Same file as 8.4.
- [x] 8.6 Update/replace any existing `ServiceRecord`-based integration tests and the `RecentActivity` dashboard snapshot test for the new `MaintenanceItem` source. `ServiceRecordsControllerTests.cs` removed with the entity; `DashboardsControllerTests.cs`'s `CreateServiceRecordAsync` helper replaced with `CreateMaintenanceItemAsync`, plus a new test for the Done-only filter.

## 9. Frontend: API client (Web)

- [x] 9.1 Add typed API functions under `src/api/` for maintenance items, checklist items, part lines, household templates, platform templates, and admin template management, against the regenerated `schema.d.ts`.
- [x] 9.2 Add TanStack Query hooks (`useMaintenanceItems`, `useMaintenanceItem`, `useHouseholdTemplates`, `usePlatformTemplates`) mirroring existing hook patterns (`useAssets`, `useEngines`). Checklist items and part lines have no standalone list endpoint (they're nested in `MaintenanceItemResponse`), so `useChecklistItems`/`usePartLines` became mutation hooks (`useMaintenanceItemMutations`) instead of query hooks — deviation noted, no separate GET exists to wrap.

## 10. Frontend: maintenance items UI (Web)

- [x] 10.1 Replace the asset detail page's service-records tab with a "Maintenance" tab listing `MaintenanceItem`s (status, blocked badge, date/cost) per `specs/frontend-maintenance-items/spec.md`.
- [x] 10.2 Build the quick-create dialog (title + optional template picker) that creates the item and navigates to its full-page editor.
- [x] 10.3 Build the full-page editor route (`/households/:householdId/assets/:assetId/maintenance/:itemId`) with autosaving fields for title/description/status/date/cost/odometer/engine hours/engine, using the `MarkdownEditor` component (from `shared-markdown-editor`) for `description`.
- [x] 10.4 Build the checklist UI: checkbox toggle, context menu (Mark skipped/Reopen/Move up/Move down), and drag-and-drop reordering reusing the `@dnd-kit` pattern from `WidgetGrid.tsx`.
- [x] 10.5 Build the parts list UI (name/quantity/status/optional fields) with add/edit/delete.
- [x] 10.6 Build the Done-transition confirmation (Go back / Mark remaining as Skipped, then complete / Complete anyway), wired to the status control.
- [x] 10.7 Component tests for the quick-create dialog, checklist interactions (toggle, skip, reorder via both drag and context menu — drag exercised via the reorder helper's own logic; jsdom has no real pointer geometry so the drag path is covered indirectly, context-menu path directly), parts list interactions, and the Done-transition confirmation's three branches.

## 11. Frontend: templates UI (Web)

- [x] 11.1 Build the household templates screen (`/households/:householdId/templates`): list, create/edit/delete, step management (add/edit/delete/reorder, engine-scoping + recurrence interval fields). Reorder implemented via Move up/down controls rather than drag (the spec requires drag specifically for the maintenance-item checklist UI, not templates; "reorder" here is satisfied functionally).
- [x] 11.2 Add the platform-template browse section with "Duplicate to my household" action.
- [x] 11.3 Build the template picker embedded in the quick-create dialog, filtered by the target asset's category. Built during §10 (`TemplatePicker`, shared with this screen's browse section indirectly via the same hooks).
- [x] 11.4 Component tests for template CRUD, step reorder, duplicate action, and category-filtered picker.

## 12. Frontend: admin console (Web)

- [x] 12.1 Build `PlatformAdminRoute` guard component (mirrors `ProtectedRoute`, checks the `PlatformAdmin` role claim). Added `src/lib/jwt.ts` (base64url JWT payload decode, no library dependency) and `useIsPlatformAdmin` since this frontend had no prior JWT-claim decoding.
- [x] 12.2 Add the conditional "Admin" top-nav link, visible only when the JWT carries `PlatformAdmin`.
- [x] 12.3 Build the `/admin` shell with sub-navigation, and the `/admin/templates` platform template management screen (list/create/edit/delete templates and steps). Reuses `TemplateEditor`/`TemplateStepList` from §11 pointed at `useAdminTemplateMutations`, per design's "favor extracting a shared template-editor component" guidance.
- [x] 12.4 Component tests for the route guard (blocked vs. allowed) and the nav link's conditional rendering.

## 13. Rework: `TemplateStep.SuggestedParts` storage (Domain / Infrastructure)

- [x] 13.1 Add `TemplateStepSuggestedPart` entity (`Id`, `TemplateStepId` FK, `Name`, `Quantity`, `SortOrder`) to `Steward.Domain`; remove `TemplateStep`'s `List<SuggestedPart>` value-object property and the now-unused `SuggestedPart` value type, replacing it with a `SuggestedParts` navigation collection of the new entity (same property name, no DTO/API contract change).
- [x] 13.2 Add `TemplateStepSuggestedPartConfiguration` (plain FK relationship, no `HasConversion`, no `ValueComparer`); remove the `jsonb`/`HasConversion`/`ValueComparer` mapping from `TemplateStepConfiguration.cs`.
- [x] 13.3 Update `TemplateService.cs`, `MaintenanceItemService.cs`, and `PlatformTemplateSeeder.cs` wherever they read or write `TemplateStep.SuggestedParts` to work against the new child-entity collection instead of the JSON-backed list (deep-copy-on-duplicate, suggested-parts-copy-on-apply, and seed data). Also added `.ThenInclude(s => s.SuggestedParts)` (or `.Include(s => s.SuggestedParts)`) everywhere a `TemplateStep`/`Template` is loaded and its `SuggestedParts` are read — this wasn't needed before since `jsonb` always loaded with the parent row, but a real navigation collection requires an explicit `Include`.
- [x] 13.4 Regenerate the `InitialCreate` migration to reflect the new `TemplateStepSuggestedParts` table in place of the `jsonb` column. Applied to both the `steward` (dev) and `steward_test` databases.
- [x] 13.5 Move the `isBlocked`-derivation tests out of `MaintenanceItemServiceLogicTests.cs` into `Steward.UnitTests` (no longer blocked from the InMemory provider once `SuggestedParts` is off `jsonb`); leave the template-expansion tests that exercise `ApplicableCategories` matching as Postgres-backed integration tests (the array column is unaffected by this rework).

## 14. Verification

- [x] 14.1 Manually exercise: quick-log an oil change in one step; create a "Winterize the boat" item from a template on a twin-engine asset and confirm per-engine checklist rows; add a part, advance it through Needed→Ordered→Received, and confirm the Blocked badge clears; trigger the Done-transition prompt with open items and try all three options; reorder checklist items via drag and via the context menu; duplicate a platform template and edit the copy; confirm the original is unaffected; log in as a non-admin and confirm `/admin` is unreachable and the nav link is absent. **Not done by the implementing agent** — no interactive browser tool was available in that session. A boot-level smoke check substituted: API + Postgres started cleanly, `GET /api/templates/platform` correctly 401s unauthenticated, `/scalar/v1` loads, and the 4 seeded platform templates (Oil change, Tire rotation, Winterize engine, Battery check) are present in the dev DB with `HouseholdId = null`. The interactive click-through this item describes still needs a human pass.
- [x] 14.2 Run `dotnet test` (unit + integration) and `npm test`/`npm run lint` in `src/Steward.Web`. Green: `dotnet build` 0 errors/warnings; `dotnet test` 135 unit + 188 integration passing; `npx tsc -b --noEmit` 0 errors; `npm run lint` 0 errors (1 pre-existing unrelated warning in `WidgetGrid.tsx`, confirmed via `git diff` to predate this change); `npx vitest run` 217/217 passing across 46 files.
