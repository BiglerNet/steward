## 1. Backend: default dashboard seed (Infrastructure)

- [x] 1.1 Update `SeedDefaultDashboardAsync` in `src/Steward.Infrastructure/Dashboards/DashboardService.cs` to seed widgets in order: `CylinderIndex` (Small), `TotalDisplacement` (Small), `TotalHorsepower` (Small), `AssetCount` (Small), `RecentActivity` (Full, `{"limit":5}`), `DueSoon` (Full, `{"daysAhead":30}`), with `Position` 0–5 matching that order.
- [x] 1.2 Update/add an integration test asserting the seeded default dashboard's widget order and sizes match the new composition (`tests/Steward.IntegrationTests`).

## 2. Frontend: dependencies and small-widget centering (Web)

- [x] 2.1 Add `@dnd-kit/core` and `@dnd-kit/sortable` to `src/Steward.Web/package.json`; run install.
- [x] 2.2 Update `StatWidget.tsx` so `CardContent` centers its contents (label/value/sub-label) horizontally and vertically within the card.
- [x] 2.3 Add/update a component test for `StatWidget` verifying the centering classes/structure.

## 3. Frontend: edit-mode state and staged layout (Web)

- [x] 3.1 In `DashboardPage.tsx`, add edit-mode state (`isEditing`) and a staged widget-layout array, initialized from `detail.widgets` when entering edit mode.
- [x] 3.2 Add "Edit Dashboard" / "Save Layout" / "Cancel" controls driving edit-mode entry, save (via `useReplaceWidgetLayout`), and cancel/discard (restore last-saved layout, exit edit mode).
- [x] 3.3 On save failure, keep edit mode active, preserve staged changes, and surface an error via `toast`.

## 4. Frontend: drag-and-drop reordering (Web)

- [x] 4.1 Wrap `WidgetGrid` (or a new edit-mode variant) with `@dnd-kit`'s `DndContext` and `SortableContext` over the staged widget array, keyed by widget id.
- [x] 4.2 Add a per-widget drag handle (visible only in edit mode) and wire `onDragEnd` to reorder the staged array.
- [x] 4.3 Verify keyboard-based reordering works via `@dnd-kit`'s built-in keyboard sensor (focus handle, arrow keys move the widget).

## 5. Frontend: resize control (Web)

- [x] 5.1 Add a per-widget resize control (visible only in edit mode) that cycles the widget's staged `widgetSize` through `Small → Wide → Full → Small` on click.
- [x] 5.2 Ensure `WidgetGrid`'s `SIZE_COLS` rendering reflects the staged size immediately (no save required to preview).

## 6. Frontend: replace catalog dialog with in-place editor (Web)

- [x] 6.1 Move the "Available Widgets" add-widget affordance from `WidgetCatalogSheet`'s dialog into an edit-mode control on the dashboard (e.g. a small "Add Widget" menu/popover), preserving add-widget-with-default-size behavior.
- [x] 6.2 Add a per-widget remove control (visible only in edit mode) that removes the widget from the staged layout.
- [x] 6.3 Delete `WidgetCatalogSheet.tsx` and its Up/Down-button, size-`<select>` reorder UI; remove its export from `src/Steward.Web/src/components/dashboard/index.ts` and its usage in `DashboardPage.tsx`.
- [x] 6.4 Update/add component tests covering: entering edit mode, drag reorder, resize cycling, add widget, remove widget, save, and cancel/discard.

## 7. Verification

- [x] 7.1 Run `dotnet test` (backend) and confirm the updated seed test passes.
- [x] 7.2 Run `npm run lint` and `npm test` in `src/Steward.Web`.
- [x] 7.3 Manually verify in the browser: new household's default dashboard shows the correct row 1/row 2 order; small widgets render centered; edit mode drag/resize/add/remove/save/cancel all work as expected.
