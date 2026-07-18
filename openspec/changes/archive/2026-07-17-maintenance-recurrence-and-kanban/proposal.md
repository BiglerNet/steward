## Why

With `MaintenanceItem`, `ChecklistItem`, and `Template`/`TemplateStep` in place (see `maintenance-items-and-templates`), the pieces exist to answer "when did I last change the oil, and when's it due again" and to give a household an at-a-glance view of maintenance work currently in flight — but nothing computes or surfaces either yet. This change adds both, purely as a read/UI layer on top of the existing entities: no scheduler, no new mutable state.

## What Changes

- New read-only "maintenance schedule" computation: for each `(TemplateStep, Engine)` pair an asset has ever had a checklist item for, compute `lastDoneAt` (latest `Done` `ChecklistItem.ResolvedAt` for that pair — `Skipped` never counts) and a best-effort `lastDoneReading` (the nearest `EngineHoursLog`/`MileageLog` entry at or before that date), then derive a due status from the step's interval.
- Extend the dashboard's `DueSoon` widget with a third source (recurring maintenance due, alongside the existing Registration/Warranty expiry) using the same `Overdue`/`DueSoon`/`Upcoming` bucketing.
- New per-asset "maintenance schedule" panel on the asset detail page showing each tracked recurring step's last-done date/reading and due status.
- New household-wide kanban board (`Planned`/`InProgress` columns, `Done` as a drop target only — completed items don't linger on the board) with an asset filter, drag-and-drop status changes (reusing the existing `@dnd-kit` pattern), and the same open-checklist-items confirmation on any drag into `Done` that the full-page editor already enforces. `Cancelled` is reached via a card action, not a column.
- New household-wide maintenance item list endpoint (across all assets) to back the kanban board.

## Capabilities

### New Capabilities
- `maintenance-recurrence`: backend computation of last-done/next-due per `(TemplateStep, Engine)` pair and the per-asset schedule endpoint.
- `frontend-maintenance-schedule`: the per-asset maintenance schedule panel.
- `frontend-maintenance-kanban`: the household-wide kanban board.

### Modified Capabilities
- `maintenance-items`: add a household-wide (cross-asset) list endpoint to back the kanban board.
- `dashboard-widgets`: `DueSoon` gains a third source (recurring maintenance) alongside Registration/Warranty expiry.

## Impact

- **Backend**: new read-only query logic joining `ChecklistItem`/`TemplateStep` history against `EngineHoursLog`/`MileageLog`; no new mutable entities or migrations beyond what `maintenance-items-and-templates` already introduced.
- **Frontend**: new route for the kanban board (e.g. `/households/:householdId/maintenance`); new panel on the asset detail page; extends the existing `DueSoon` widget rendering to handle a third `recordType`.
- **Depends on** `maintenance-items-and-templates` being implemented first — this change's specs are written as deltas against that change's specs, not against the current `main` state.
