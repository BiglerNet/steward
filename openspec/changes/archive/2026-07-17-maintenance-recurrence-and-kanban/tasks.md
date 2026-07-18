## 1. Recurrence computation (Application / Infrastructure)

- [x] 1.1 `Steward.Application/Tracking/MaintenanceRecurrence/Dtos.cs` — `MaintenanceScheduleEntryResponse` per `specs/maintenance-recurrence/spec.md`.
- [x] 1.2 `IMaintenanceScheduleService.cs` — signature for computing an asset's schedule entries.
- [x] 1.3 Implement the service: enumerate distinct `(TemplateStepId, EngineId)` pairs with at least one `ChecklistItem` for the asset; compute `lastDoneAt` from the latest `Done` `ChecklistItem.ResolvedAt`; compute `lastDoneReading` via the nearest `EngineHoursLog`/`MileageLog` entry at or before that date; compute `dueStatus` per the calendar/usage combination rule.
- [x] 1.4 Unit tests: never-done pair, independent per-engine entries, skip-does-not-advance-lastDoneAt, nearest-log-entry lookup (found and not-found cases), each `dueStatus` branch (Overdue via usage, Overdue/DueSoon/Upcoming via calendar, Unknown when no reading available).

## 2. Recurrence endpoint (Api)

- [x] 2.1 `GET /api/households/{householdId}/assets/{assetId}/maintenance-schedule` on (a new or existing) maintenance controller, per `specs/maintenance-recurrence/spec.md`.
- [x] 2.2 Integration tests for authorization (any Active member/PlatformAdmin can read) and the never-done/independent-engines scenarios end-to-end.

## 3. Household-wide maintenance item list (Application / Infrastructure / Api)

- [x] 3.1 Add `GET /api/households/{householdId}/maintenance-items` to `MaintenanceItemsController` (or a household-scoped controller), with `status`/`assetId` filters, per the `maintenance-items` delta spec in this change.
- [x] 3.2 Integration tests: cross-asset listing, combined status+asset filtering, non-member 403.

## 4. Dashboard DueSoon extension (Infrastructure / Api)

- [x] 4.1 Extend `DashboardService`'s `DueSoon` computation to also query each household asset's maintenance schedule and include entries at `Overdue`/`DueSoon`/`Upcoming` (excluding `OK`/`Unknown`), mapped to the `MaintenanceRecurrence` `recordType` shape.
- [x] 4.2 Update/extend the existing `DueSoon` snapshot tests for the new `recordType` and its inclusion/exclusion rules.

## 5. Frontend: schedule panel (Web)

- [x] 5.1 API function + TanStack Query hook for the maintenance-schedule endpoint.
- [x] 5.2 Build the "Maintenance schedule" panel on the asset detail page per `specs/frontend-maintenance-schedule/spec.md` (last-done date/reading, due-status badge, per-engine rows, "Never" state).
- [x] 5.3 Component tests: divergent-engine rows, never-done row, each due-status badge rendering.

## 6. Frontend: kanban board (Web)

- [x] 6.1 API function + TanStack Query hook for the household-wide maintenance item list.
- [x] 6.2 Build the kanban board route (`/households/:householdId/maintenance`): `Planned`/`InProgress` columns, `Done` drop zone, asset filter, card content (title, asset name, Blocked badge), using the `@dnd-kit` pattern from `WidgetGrid.tsx`.
- [x] 6.3 Wire drag-to-`InProgress`/`Planned` to a direct status `PATCH`, and drag-to-`Done` to the same open-checklist-items confirmation flow used by the full-page editor (reusing that confirmation component rather than duplicating it).
- [x] 6.4 Add the per-card "Cancel" action (sets `status: "Cancelled"`, no confirmation needed since it doesn't touch checklist completion semantics).
- [x] 6.5 Add a link to the kanban board from household navigation.
- [x] 6.6 Component tests: drag between active columns, drag-to-Done confirmation (all three branches), card removal from the board after completing, asset filter, Viewer cannot drag.

## 7. Verification

- [x] 7.1 Manually exercise: view an asset's maintenance schedule showing a "Never done" kicker-winterization row alongside a recently-done main-engine row; confirm a DueSoon dashboard widget surfaces an overdue recurring step; open the household kanban board, drag a card between Planned/InProgress, then drag a card with open checklist items onto Done and confirm the same three-option prompt appears and the card disappears from the board afterward; cancel a card via its context menu.
- [x] 7.2 Run `dotnet test` and `npm test`/`npm run lint` in `src/Steward.Web`.
