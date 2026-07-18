## MODIFIED Requirements

### Requirement: Dashboard snapshot
The system SHALL provide `GET /api/v1/households/{householdId}/dashboards/{dashboardId}/snapshot` (any Active member or PlatformAdmin) returning computed data for every widget in the dashboard's current layout. The response is a JSON object keyed by widget type, containing only the widget types present in the layout. All values are computed via SQL-level aggregation — no full entity trees are loaded into application memory.

**Widget data shapes:**
- `AssetCount`: `{ "count": <int> }`
- `CylinderIndex`: `{ "totalCylinders": <int>, "engineCount": <int> }`
- `TotalDisplacement`: `{ "totalCc": <decimal>, "engineCount": <int> }`
- `TotalHorsepower`: `{ "totalHp": <decimal>, "engineCount": <int> }`
- `TotalTorque`: `{ "totalNm": <decimal>, "engineCount": <int> }`
- `DueSoon`: `{ "items": [{ "assetId", "assetName", "recordType": "Registration"|"Warranty"|"MaintenanceRecurrence", "expiresOn"?, "urgency": "Overdue"|"DueSoon"|"Upcoming", "stepText"?, "engineLabel"? }] }` — `MaintenanceRecurrence` entries come from the `maintenance-recurrence` capability's per-asset schedule computation, filtered to entries whose `dueStatus` is `Overdue`, `DueSoon`, or `Upcoming` (entries at `OK` or `Unknown` are excluded); for these entries `expiresOn` is omitted and `stepText`/`engineLabel?` are populated instead.
- `RecentActivity`: `{ "items": [{ "assetId", "assetName", "description", "performedOn", "cost" }] }` — sourced from `MaintenanceItem` rows with `Status = Done`, where `description` is the item's `Title` and `performedOn` is its `Date`.
- `FuelCostYtd`: `{ "totalCost": <decimal>, "logCount": <int> }`
- `MileageMtd`: `{ "totalMiles": <decimal>, "logCount": <int> }`

**DueSoon urgency thresholds (server-side, not configurable per widget instance):**
- For `Registration`/`Warranty`: `Overdue` if `expiresOn < today`; `DueSoon` if within 7 days; `Upcoming` if within `daysAhead` days (from widget config, default 30), excluding Overdue and DueSoon.
- For `MaintenanceRecurrence`: urgency is taken directly from the schedule entry's `dueStatus` (`Overdue`/`DueSoon`/`Upcoming`), computed per the `maintenance-recurrence` capability's calendar/usage-interval logic.

**DueSoon widget config schema:** `{ "daysAhead": <int, default 30> }`
**RecentActivity widget config schema:** `{ "limit": <int, default 5, max 20> }`

#### Scenario: Snapshot returns only widgets in the dashboard layout
- **WHEN** a dashboard contains only `AssetCount` and `DueSoon` widgets, and a member requests the snapshot
- **THEN** the response JSON contains exactly the keys `AssetCount` and `DueSoon`

#### Scenario: DueSoon widget returns items sorted by expiry date ascending
- **WHEN** the dashboard contains a DueSoon widget and the household has registrations and warranties with upcoming expiry dates
- **THEN** the snapshot's `DueSoon.items` for those record types are ordered by `expiresOn` ascending

#### Scenario: DueSoon urgency classification for registrations/warranties
- **WHEN** a household has a registration expired yesterday, a warranty expiring in 3 days, and a registration expiring in 20 days (with default 30-day window)
- **THEN** the snapshot DueSoon items have urgency `Overdue`, `DueSoon`, and `Upcoming` respectively

#### Scenario: DueSoon includes overdue recurring maintenance
- **WHEN** a household has an asset with a "Change oil" recurrence entry at `dueStatus: "Overdue"`
- **THEN** the snapshot's `DueSoon.items` includes an entry with `recordType: "MaintenanceRecurrence"`, `urgency: "Overdue"`, and the step's `stepText`

#### Scenario: DueSoon excludes maintenance recurrence entries that are not due
- **WHEN** a household's only recurrence entry is at `dueStatus: "OK"`
- **THEN** no `MaintenanceRecurrence` item appears in `DueSoon.items` for it

#### Scenario: RecentActivity respects the limit config
- **WHEN** a dashboard has a RecentActivity widget with `config: { "limit": 3 }` and the household has 10 `Done` maintenance items across its assets
- **THEN** the snapshot returns exactly 3 items in `RecentActivity.items`, ordered by `Date` descending

#### Scenario: RecentActivity excludes non-Done maintenance items
- **WHEN** a household has maintenance items in `Planned`, `InProgress`, and `Done` status
- **THEN** the snapshot's `RecentActivity.items` includes only the `Done` ones

#### Scenario: FuelCostYtd covers calendar year to date only
- **WHEN** the household has fuel logs from the current calendar year and from previous years
- **THEN** `FuelCostYtd.totalCost` includes only the current year's logs

#### Scenario: MileageMtd covers the current calendar month
- **WHEN** the household has mileage logs from the current month and from prior months
- **THEN** `MileageMtd.totalMiles` includes only the current calendar month's logs

#### Scenario: Snapshot for a dashboard with no widgets returns an empty object
- **WHEN** the dashboard has zero widgets (layout was cleared via PUT)
- **THEN** HTTP 200 is returned with an empty JSON object `{}`

#### Scenario: Viewer can access the snapshot
- **WHEN** a user with `Role = Viewer` requests the snapshot
- **THEN** HTTP 200 is returned with widget data

#### Scenario: Non-member cannot access the snapshot
- **WHEN** a user not belonging to the household requests the snapshot
- **THEN** HTTP 403 is returned
