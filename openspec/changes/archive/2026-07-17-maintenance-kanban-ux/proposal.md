## Why

The maintenance kanban board shipped in `maintenance-recurrence-and-kanban` is hard to use day-to-day: the drag handle is a ~16px icon instead of the card itself, the item editor page is a dead end with no way back to where you came from and no indication of which asset an item belongs to, and the `completedAt` timestamp that already drives the board's "done in the last 7 days" window is never shown to the user anywhere.

## What Changes

- Kanban cards become draggable by their whole body (not just the grip icon), using dnd-kit activation constraints (`distance` on desktop pointer input, `delay`/`tolerance` on touch) so a plain click/tap still hits the title link or the card menu instead of starting a drag. The grip icon remains as a visual affordance only.
- Add a mobile-appropriate touch sensor (press-and-hold to arm a drag) alongside the existing pointer sensor, so touch users get the same whole-card drag without it fighting page scroll.
- The full-page maintenance item editor gains a breadcrumb that shows where the item sits (`Maintenance` for kanban-origin visits, `[Asset name] › Maintenance` otherwise) and a working "back" action that returns to the actual place the user came from when that's known, falling back to the item's asset Maintenance tab when it isn't (direct link, refresh).
- The kanban board's asset filter moves from component state into a URL search param, so returning to the board (via the new back action or otherwise) restores the filter instead of resetting to "All assets."
- The existing `completedAt` field is surfaced for the first time: a relative-time label ("Completed 2 days ago") on kanban cards in the Done zone, the full absolute date on the full-page editor, and a new "Completed" column on the asset's Maintenance tab table.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `frontend-maintenance-kanban`: whole-card drag with activation constraints and a touch sensor; asset filter lives in the URL; Done-zone cards show a relative completed-at label; cards pass navigation origin state when linking to the item editor.
- `frontend-maintenance-items`: full-page editor gains a breadcrumb/back affordance and displays the absolute completion date; the asset Maintenance tab table gains a Completed column and passes navigation origin state when linking to the item editor.

## Impact

- Frontend only. No API, DTO, or database changes — `completedAt` is already returned by the maintenance item endpoints.
- Files: `KanbanCard.tsx`, `KanbanColumn.tsx`, `KanbanDoneDropZone.tsx`, `MaintenanceKanbanPage.tsx`, `MaintenanceItemEditorPage.tsx`, `MaintenanceItemsPage.tsx`, plus a new small breadcrumb component and a relative-time formatting helper (native `Intl.RelativeTimeFormat`, no new dependency).
