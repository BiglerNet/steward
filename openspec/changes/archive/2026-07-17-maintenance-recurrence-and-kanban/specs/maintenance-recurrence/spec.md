## ADDED Requirements

### Requirement: Per-asset maintenance schedule endpoint
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/maintenance-schedule` (any Active member or PlatformAdmin) returning one entry per `(TemplateStepId, EngineId)` pair the asset has ever had a `ChecklistItem` for (`EngineId` is omitted/null for asset-level steps), each containing: `templateId`, `templateTitle`, `templateStepId`, `stepText`, `engineId?`, `engineLabel?`, `lastDoneAt` (nullable date — null means never resolved `Done`), `lastDoneReading?` (nullable `{ value, unit }`), `intervalMonths?`, `intervalMiles?`, `intervalHours?`, `dueStatus` (`Overdue` | `DueSoon` | `Upcoming` | `OK` | `Unknown`).

A pair is included whenever at least one `ChecklistItem` with that `TemplateStepId`/`EngineId` combination exists for the asset, regardless of that checklist item's current or historical `Status`.

#### Scenario: Never-completed step shows as never done
- **WHEN** an asset has a `Skipped` checklist item for a given `(TemplateStep, Engine)` pair and no `Done` occurrence has ever existed for it
- **THEN** the schedule entry for that pair has `lastDoneAt: null` and a `dueStatus` reflecting that the step has never been completed

#### Scenario: Independent engines on the same asset have independent entries
- **WHEN** a boat asset has an engine-scoped "Change oil" step applied to both a main outboard and a kicker, with different completion histories
- **THEN** the schedule returns two separate entries for that step, one per engine, each with its own `lastDoneAt`

#### Scenario: Skipping does not advance lastDoneAt
- **WHEN** a `(TemplateStep, Engine)` pair's most recent occurrence was `Done` six months ago and a newer occurrence was just marked `Skipped`
- **THEN** `lastDoneAt` still reflects the six-month-old `Done` occurrence, not the more recent `Skipped` one

---

### Requirement: Last-done reading is derived from usage logs, not from MaintenanceItem fields
When computing `lastDoneReading` for a schedule entry, the system SHALL look up the nearest `EngineHoursLog` entry (for an engine-scoped step, matching `EngineId`) or `MileageLog` entry (for an asset-level step) with a `Date` on or before the checklist item's `ResolvedAt` date, and SHALL NOT read `MaintenanceItem.OdometerMiles`/`EngineHours` for this purpose. If no such log entry exists, `lastDoneReading` SHALL be null.

#### Scenario: Reading found via nearest log entry
- **WHEN** a checklist item was resolved `Done` on 2026-06-01 and the engine has an `EngineHoursLog` entry dated 2026-05-30 at 340 hours, with no closer entry
- **THEN** the schedule entry's `lastDoneReading` is `{ value: 340, unit: "Hours" }`

#### Scenario: No nearby log entry yields a null reading
- **WHEN** a checklist item was resolved `Done` but no `EngineHoursLog`/`MileageLog` entry exists on or before that date
- **THEN** the schedule entry's `lastDoneReading` is null, while `lastDoneAt` is still populated

---

### Requirement: Due status combines calendar and usage intervals
`dueStatus` SHALL be computed as follows: if a usage interval (`intervalMiles`/`intervalHours`) is set and the asset's/engine's current reading meets or exceeds `lastDoneReading + interval`, the status is `Overdue`. Otherwise, if a calendar interval (`intervalMonths`) is set, the status follows the same day-window bucketing already used by the `DueSoon` dashboard widget (`Overdue` if past due, `DueSoon` if due within 7 days, `Upcoming` if due within a 30-day default window, else `OK`). If neither interval yields a determinable status (e.g. a usage interval is set but no current reading is available), the status is `Unknown`. If the step has no interval configured at all, the status is `OK`.

#### Scenario: Usage interval exceeded marks Overdue regardless of calendar status
- **WHEN** a step's `intervalHours = 100`, `lastDoneReading = 340`, and the engine's current hours reading is 450
- **THEN** `dueStatus` is `Overdue`

#### Scenario: Calendar-only interval uses day-window bucketing
- **WHEN** a step has `intervalMonths = 12`, `lastDoneAt` is 13 months ago, and no usage interval is configured
- **THEN** `dueStatus` is `Overdue`

#### Scenario: Usage interval with no current reading is Unknown, not guessed
- **WHEN** a step has only `intervalHours` configured and the engine has no `EngineHoursLog` entries at all
- **THEN** `dueStatus` is `Unknown`
