## Why

The dashboard's default layout and small stat widgets were shipped with a minimal first cut: single-value widgets (Cylinder Index, Total Displacement, etc.) left-align their content awkwardly in the available space, the seeded "Overview" dashboard omits Total Displacement and Total Horsepower and orders widgets inconsistently, and reordering/resizing widgets today requires opening a dialog and clicking Up/Down arrows plus a size dropdown rather than directly manipulating the layout. This change tightens the visual presentation of small widgets and replaces the dialog-based layout editor with direct drag-and-drop/resize on the dashboard itself.

## What Changes

- Small, single-value stat widgets (`StatWidget` used by Cylinder Index, Total Displacement, Total Horsepower, Total Torque, Asset Count, etc.) center their label/value/sub-label content within the card instead of left-aligning it.
- The seeded default "Overview" dashboard's widget set and order changes to: row 1 — Cylinder Index, Total Displacement, Total Horsepower, Asset Count (all `Small`); row 2 — Recent Activity, then Due Soon (both `Full`). This only affects newly seeded dashboards (first-time auto-create for a household with none); existing dashboards are not migrated.
- The dashboard gains an in-place **edit mode** (toggled from the existing "Edit Dashboard" control) that replaces the current dialog-based layout editor: widgets can be dragged directly on the grid to reorder/reposition, and each widget exposes a resize control that cycles its `WidgetSize` through `Small` → `Wide` → `Full`. Changes are staged client-side and persisted via the existing `PUT .../dashboards/{dashboardId}/widgets` replace-layout endpoint on save; adding/removing widgets from the catalog remains available in edit mode.
- The old `WidgetCatalogSheet` dialog (Up/Down reorder buttons, size `<select>`) is removed in favor of the in-place editor.

## Capabilities

### New Capabilities
- `frontend-dashboard-editing`: In-place drag-and-drop dashboard edit mode — dragging widgets to reorder/reposition, resize-cycling widget size, staging changes, and saving/canceling — plus centered layout for small single-value stat widgets.

### Modified Capabilities
- `dashboard-widgets`: The default seeded "Overview" dashboard's widget list and order changes (adds `TotalDisplacement` and `TotalHorsepower`, reorders to Cylinder Index, Total Displacement, Total Horsepower, Asset Count, Recent Activity, Due Soon).

## Impact

- Frontend: `src/Steward.Web/src/components/dashboard/StatWidget.tsx` (centering), `WidgetGrid.tsx` (drag/resize-aware rendering), `WidgetCatalogSheet.tsx` (removed/replaced), `DashboardPage.tsx` (edit-mode state), `useDashboardMutations.ts` (unchanged endpoint, staged local state before save). New dependency: a drag-and-drop library (e.g. `@dnd-kit/core` + `@dnd-kit/sortable`) — none currently in `package.json`.
- Backend: `src/Steward.Infrastructure/Dashboards/DashboardService.cs` (`SeedDefaultDashboardAsync` widget list/order). No API contract change — the existing replace-layout endpoint already accepts arbitrary ordered widget arrays.
- Spec: `openspec/specs/dashboard-widgets/spec.md` (default seed requirement), new `openspec/specs/frontend-dashboard-editing/spec.md`.
