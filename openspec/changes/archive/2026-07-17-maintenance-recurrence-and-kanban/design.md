## Context

`maintenance-items-and-templates` establishes `ChecklistItem.TemplateStepId` and `ChecklistItem.EngineId` as the stable identity for recurring work, and `ChecklistItem.ResolvedAt`/`Status` as the record of whether a given occurrence was actually done or just skipped. This change is purely additive on top of that: it computes derived views (schedule, kanban) and adds zero new mutable state.

## Goals / Non-Goals

**Goals:**
- Compute "last done" / "next due" per `(TemplateStep, Engine)` pair entirely from existing data — no new fields on `ChecklistItem` or `MaintenanceItem`.
- Give a household one place to see everything currently in flight across its whole fleet.
- Keep the kanban board honest as an "active work" view — it should never become a graveyard of old `Done` cards.

**Non-Goals:**
- No background job or scheduler — everything here is computed at read time.
- No auto-created future `MaintenanceItem`s.
- No inventory/consumption logic (still deferred, per `maintenance-items-and-templates`).

## Decisions

### Decision: "last done reading" comes from `EngineHoursLog`/`MileageLog`, not from `MaintenanceItem`'s own reading fields
`MaintenanceItem.OdometerMiles`/`EngineHours` are single values on the parent item, which works fine when an item touches one engine — but a "Winterize the boat" item can contain checklist rows for two different engines, and a single top-level `EngineHours` field can't represent both engines' readings at once. Rather than adding a reading field to `ChecklistItem` (which would reopen the "keep checklist items lightweight" decision from the previous change), the schedule computation instead looks up the nearest `EngineHoursLog` entry (for an engine-scoped step) or `MileageLog` entry (for an asset-level step) at or before the checklist item's `ResolvedAt` date. This reuses data users are already recording for other reasons and correctly disambiguates per-engine readings within a single multi-engine item. If no such log entry exists near that date, the reading is simply omitted — only the date-based "last done" is shown, calendar-interval due status still works, and usage-interval due status is skipped for that occurrence rather than guessed at.

Alternative considered: add a `Reading`/`ReadingUnit` field directly to `ChecklistItem`, filled in at resolution time. Rejected — duplicates data that already has a canonical home (`EngineHoursLog`/`MileageLog`), and risks drifting out of sync with it.

### Decision: due status combines a calendar interval and a usage interval independently, taking the more urgent
A `TemplateStep` may define a calendar interval (months), a usage interval (miles or hours), both, or neither. When both are present, "due" is whichever threshold is reached first — but calendar and usage thresholds aren't directly comparable in the way two calendar dates are, so the computation treats them as two independent checks and reports whichever yields the more urgent classification: usage-based intervals can only ever contribute `Overdue` (the reading either has or hasn't exceeded `lastDoneReading + interval` — there's no "days out" equivalent for `DueSoon`/`Upcoming`) or `OK`/omitted if the current reading is unknown; calendar-based intervals use the same day-window bucketing (`Overdue` / within 7 days / within `daysAhead`) already established for the `DueSoon` widget's Registration/Warranty entries. A step with only a usage interval and no recent log entry to compare against simply doesn't surface a due status — better silent than wrong.

### Decision: a `(TemplateStep, Engine)` pair is "tracked" once any `ChecklistItem` exists for it, regardless of status
A step that's been applied but never actually completed (e.g. the kicker's winterization, always `Skipped`) should still show up in the schedule as "Last done: Never" (or the date of the last real `Done`, if one ever happened), not disappear because nothing currently resolves to `Done`. Trackedness is derived from "at least one `ChecklistItem` with this `TemplateStepId`/`EngineId` combination exists," independent of what its current or historical `Status` values are.

### Decision: kanban shows `Done` as a drop target, not a resting column
The board's columns are `Planned` and `InProgress`, with `Done` rendered as a third drop zone a card can be dragged into — but a card dropped there triggers the status-change API call (with the same open-checklist confirmation the full-page editor uses) and then disappears from the board on the next refetch, rather than accumulating there. This avoids inventing a card-level "complete" button that duplicates what dragging already does, while keeping the board's steady-state view limited to genuinely active work. `Cancelled` has no column at all — it's reached via a per-card action (e.g. a context menu), since a board doesn't need a resting place for abandoned work any more than for finished work.

### Decision: kanban needs a new household-wide list endpoint
Every existing `maintenance-items` list endpoint is scoped to one asset (`GET .../assets/{assetId}/maintenance-items`). The kanban board needs to see across an entire household's assets at once, so this change adds `GET /api/households/{householdId}/maintenance-items` (household-wide, filterable by `status` and `assetId`) as an addition to the `maintenance-items` capability, rather than having the frontend fan out N per-asset requests.

## Risks / Trade-offs

- **[Risk] Reading-by-nearest-log-entry is a best-effort join, not an exact record.** → Mitigation: accepted — it's still more accurate than an unfilled manual field would be, and the UI should present it as "as of [log date]" rather than implying precision it doesn't have.
- **[Risk] Mixing calendar and usage interval semantics into one due-status value could be confusing.** → Mitigation: the per-asset schedule panel can show both raw signals (last-done date and last-done reading) alongside the derived status, so a user can always see why something is flagged.

## Migration Plan

No schema changes. Backend: new query/service code only. Frontend: new routes and one widget extension. Rollback is a plain deploy revert.
