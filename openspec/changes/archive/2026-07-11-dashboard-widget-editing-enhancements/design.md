## Context

The dashboard renders widgets via `WidgetGrid.tsx`: a 12-column CSS grid where each widget's `WidgetSize` (`Small`=3 cols, `Wide`=6 cols, `Full`=12 cols) maps to a `col-span-*` class, and DOM order (driven by the widget's `Position` integer) determines grid flow. There is no explicit x/y placement — the browser's grid auto-flow wraps widgets left-to-right, row by row, based on remaining space.

Layout edits currently go through `WidgetCatalogSheet.tsx`, a modal dialog with a widget-toggle chip list and a linear "Layout Order" list with Up/Down buttons and a size `<select>`. Saving calls `PUT .../dashboards/{dashboardId}/widgets` with the full ordered widget array (`useReplaceWidgetLayout`), which the backend applies as an atomic delete-and-reinsert (`DashboardService.ReplaceWidgetLayoutAsync`).

The user asked for direct drag-and-drop reordering/repositioning and resizing "from" the dashboard itself, in an edit mode, rather than through the dialog. A prior clarification with the user settled the resize model: keep the existing three-tier `WidgetSize` enum (`Small`/`Wide`/`Full`) rather than moving to a free-form pixel/row grid — resize means cycling a widget through those presets, not arbitrary width/height.

## Goals / Non-Goals

**Goals:**
- Center content (label/value/sub-label) inside `StatWidget` cards.
- Change the seeded default dashboard's widget set/order (backend seed only).
- Replace the dialog-based layout editor with an in-place edit mode: toggle edit mode on the dashboard, drag widgets to reorder within the same 12-column flow grid, resize via a per-widget control that cycles `Small → Wide → Full → Small`, stage changes locally, save or cancel.
- Keep the existing `PUT .../dashboards/{dashboardId}/widgets` contract unchanged — this is a frontend interaction change, not a data model change.

**Non-Goals:**
- Free-form pixel/row-based grid layout (`x`, `y`, `width`, `height` per widget) — explicitly deferred; out of scope per user decision.
- Per-widget custom pixel sizing or independent row heights.
- Multi-column drag targets beyond the existing 12-col responsive breakpoints (`sm`/`lg`) already defined in `SIZE_COLS`.
- Real-time collaborative editing (two household members editing the same dashboard simultaneously) — last write wins, as today.

## Decisions

### Decision: Use `@dnd-kit/core` + `@dnd-kit/sortable` for drag-and-drop
No drag-and-drop library exists in the frontend today (confirmed via `package.json`). `@dnd-kit` is chosen over `react-beautiful-dnd` (unmaintained, no React 19 support) and `react-grid-layout` (brings its own absolute-positioned grid model, which conflicts with the decision to keep the simple CSS-grid/flow-order model). `@dnd-kit/sortable` treats the widget list as a sortable list — a natural fit since layout is still fundamentally an ordered array with a size enum, not 2D coordinates.

**Alternatives considered:**
- `react-grid-layout`: rejected — assumes free-form x/y/w/h grid, which was explicitly ruled out.
- Native HTML5 drag-and-drop (`draggable` attribute): rejected — poor touch support, more manual work for drop-indicator UX, no built-in accessibility (keyboard reordering) that `@dnd-kit` provides out of the box.

### Decision: Edit mode is a client-side staged-state mode, not per-drag persistence
Entering edit mode snapshots the current widget layout into local component state (same pattern `WidgetCatalogSheet` already uses for its `layout` state). Drag reorders and resize-cycle clicks mutate only this local state; nothing is persisted until the user clicks "Save Layout" (or discarded on "Cancel"), reusing the existing `useReplaceWidgetLayout` mutation unchanged. This avoids a flurry of PUT requests during drag and keeps the backend contract untouched.

**Alternatives considered:**
- Persist on every drop: rejected — noisy network traffic, and a half-finished drag sequence would leave the dashboard in an intermediate state if the user navigates away mid-edit.

### Decision: Resize control cycles through presets rather than a draggable resize handle
A visible per-widget button (e.g., an icon in a small toolbar shown on the widget only while in edit mode) cycles `widgetSize` through `Small → Wide → Full → Small` on click. This was chosen over a draggable corner/edge resize handle because the underlying model only has three discrete sizes — a drag gesture implies continuous resizing the data model doesn't support, and would need snapping logic that adds complexity without adding real flexibility.

**Alternatives considered:**
- Draggable edge handle with snap-to-3-sizes: rejected — more implementation complexity (drag distance thresholds, visual snapping) for the same three outcomes a single click already provides.

### Decision: Remove `WidgetCatalogSheet`'s reorder/resize UI, keep its add/remove-widget catalog
The chip-based "Available Widgets" toggle list (add/remove widget types from the layout) is retained — drag-and-drop only replaces ordering and sizing, not catalog selection. The Up/Down arrow buttons and size `<select>` are removed since edit mode supersedes them. The catalog picker becomes a control surfaced within edit mode (e.g. an "Add Widget" affordance) rather than a separate modal, so the whole editing experience (add, remove, reorder, resize) lives in one place.

### Decision: Backend seed change is additive to the widget catalog, not a new endpoint
`SeedDefaultDashboardAsync` in `DashboardService.cs` already hardcodes the seeded widget list; changing widget composition/order is a straightforward edit to that method's `Widgets` array (add `TotalDisplacement` and `TotalHorsepower`, reorder). No migration is needed since this only affects the auto-create-on-first-list path for households with zero dashboards — existing dashboards/rows are untouched.

## Risks / Trade-offs

- [Risk] `@dnd-kit` is a new dependency, adding bundle size and a learning-curve surface for future contributors → Mitigation: it's a widely-used, actively maintained, accessibility-conscious library (keyboard + screen reader support built in), and the sortable-list use case here is one of its primary documented patterns.
- [Risk] Dragging across responsive breakpoints (grid columns differ at `sm`/`lg`) could make the drop-target visualization confusing on resize while dragging → Mitigation: edit mode drag interactions are primarily a desktop/pointer affordance; the resize-cycle button and existing add/remove controls remain fully usable on touch/mobile even if free-drag reordering is less precise there.
- [Risk] Removing the dialog-based Up/Down/select controls removes a fallback for users who prefer discrete non-drag interactions → Mitigation: `@dnd-kit/sortable` supports keyboard-based reordering (arrow keys while a widget is focused) as a built-in accessible alternative, and the resize-cycle button remains a discrete click, not a drag.
- [Trade-off] Keeping the three-tier size enum instead of free-form grid is simpler to ship but caps how much layout flexibility users get → accepted per explicit user decision; can be revisited as a future change if requested.

## Migration Plan

- Frontend: add `@dnd-kit/core` and `@dnd-kit/sortable` to `src/Steward.Web/package.json`; no environment/config changes.
- Backend: edit `SeedDefaultDashboardAsync`'s widget array; no EF Core migration required (no schema change, only seed-time data composition for new households).
- No feature flag — ship directly; the seed change only affects households with zero dashboards at time of first list call, and the edit-mode UI fully replaces the old dialog in the same release.
- Rollback: revert the frontend edit-mode change and the seed-order edit independently if needed; neither depends on a database migration.

## Open Questions

None outstanding — resize granularity was resolved with the user (discrete `Small`/`Wide`/`Full` cycling, not free-form grid).
