# frontend-maintenance-schedule Specification

## Purpose
The per-asset "Maintenance schedule" panel on the asset detail page, surfacing the `maintenance-recurrence` capability's per-asset schedule computation (last-done date/reading and due status for each tracked recurring step).

## Requirements
### Requirement: Per-asset maintenance schedule panel
The asset detail page SHALL show a "Maintenance schedule" panel listing each entry returned by the maintenance-schedule endpoint, showing the step's text, its engine label when engine-scoped, "Last done" (date and, when available, reading — e.g. "Jun 1, 2026 · 340 hrs"), and a due-status badge (`Overdue`/`DueSoon`/`Upcoming`/`OK`/`Unknown`), visible to any Active household member.

#### Scenario: Viewing a fresh oil-change status
- **WHEN** a member views an asset whose "Change oil" step was last done 2 months ago at 4,200 miles, with a 3,000-mile/3-month interval
- **THEN** the panel shows "Last done: [date] · 4,200 mi" with an appropriate due-status badge

#### Scenario: Divergent engines shown independently
- **WHEN** a boat's main outboard and kicker both have entries for the same "Change oil" step with different last-done dates
- **THEN** the panel lists them as two separate rows, each labeled with its engine

#### Scenario: Never-done step is shown plainly
- **WHEN** a tracked step has never been completed for this asset
- **THEN** the panel shows "Last done: Never" rather than omitting the row
