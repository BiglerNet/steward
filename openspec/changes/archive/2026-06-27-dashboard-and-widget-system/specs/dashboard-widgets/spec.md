## ADDED Requirements

### Requirement: List household dashboards
The system SHALL provide `GET /api/v1/households/{householdId}/dashboards` (any Active member or PlatformAdmin) returning a lightweight list of all dashboards for the household. Each item includes `id`, `name`, `isDefault`, and `position`. If no dashboards exist, the system SHALL auto-create a default "Overview" dashboard (AssetCount Small, CylinderIndex Small, DueSoon Full with 30-day window, RecentActivity Full with 5-item limit) before returning it.

#### Scenario: Member lists dashboards for a household with existing dashboards
- **WHEN** an Active member calls `GET /api/v1/households/{householdId}/dashboards`
- **THEN** HTTP 200 is returned with an array of dashboard summaries ordered by `position`

#### Scenario: First-time call auto-creates the default Overview dashboard
- **WHEN** a member calls `GET .../dashboards` for a household that has no dashboards yet
- **THEN** HTTP 200 is returned with one dashboard named "Overview" with `isDefault: true`

#### Scenario: Viewer can list dashboards
- **WHEN** a user with `Role = Viewer` calls the list dashboards endpoint
- **THEN** HTTP 200 is returned

#### Scenario: Non-member cannot list dashboards
- **WHEN** a user who is not an Active member of the household calls the list dashboards endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Get dashboard layout
The system SHALL provide `GET /api/v1/households/{householdId}/dashboards/{dashboardId}` (any Active member or PlatformAdmin) returning the dashboard metadata plus its full ordered widget list. Each widget includes `id`, `widgetType`, `widgetSize`, `position`, and `config` (widget-specific JSON).

#### Scenario: Member retrieves a dashboard with widgets
- **WHEN** an Active member calls `GET .../dashboards/{dashboardId}`
- **THEN** HTTP 200 is returned with the dashboard metadata and its widgets ordered by `position`

#### Scenario: Dashboard belonging to a different household is not found
- **WHEN** the `dashboardId` does not belong to `householdId` in the route
- **THEN** HTTP 404 is returned

---

### Requirement: Create dashboard
The system SHALL provide `POST /api/v1/households/{householdId}/dashboards` (Owner or Contributor only) accepting `{ name, isDefault?, position? }`. `name` is required and must be unique within the household (case-insensitive). If `isDefault` is `true`, any existing default dashboard SHALL have its `isDefault` set to `false`. A newly created dashboard has no widgets; widgets are added via the layout endpoint.

#### Scenario: Contributor creates a new dashboard
- **WHEN** a Contributor POSTs `{ "name": "Fuel & Mileage" }` to the create dashboards endpoint
- **THEN** HTTP 201 is returned with the new dashboard (no widgets, `isDefault: false`)

#### Scenario: Setting isDefault demotes the prior default
- **WHEN** a Contributor creates a dashboard with `isDefault: true`
- **THEN** HTTP 201 is returned, the new dashboard has `isDefault: true`, and the previously default dashboard now has `isDefault: false`

#### Scenario: Duplicate dashboard name rejected
- **WHEN** a Contributor POSTs a dashboard with a `name` already used in this household
- **THEN** HTTP 400 is returned

#### Scenario: Viewer cannot create a dashboard
- **WHEN** a user with `Role = Viewer` POSTs to the create dashboard endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Update dashboard metadata
The system SHALL provide `PUT /api/v1/households/{householdId}/dashboards/{dashboardId}` (Owner or Contributor only) accepting `{ name, isDefault, position }`. If `isDefault` is set to `true`, any existing default dashboard is demoted. `name` must remain unique within the household.

#### Scenario: Owner renames a dashboard
- **WHEN** an Owner PUTs `{ "name": "My Overview", "isDefault": true, "position": 0 }` to the update endpoint
- **THEN** HTTP 200 is returned with the updated dashboard

#### Scenario: Viewer cannot update a dashboard
- **WHEN** a Viewer PUTs to the update dashboard endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Delete dashboard
The system SHALL provide `DELETE /api/v1/households/{householdId}/dashboards/{dashboardId}` (Owner only). On success the dashboard and all its widgets SHALL be permanently deleted and HTTP 204 returned. The last remaining dashboard in a household SHALL NOT be deletable (HTTP 400).

#### Scenario: Owner deletes a non-default dashboard
- **WHEN** an Owner calls `DELETE` on a dashboard that is not the only one in the household
- **THEN** HTTP 204 is returned and the dashboard no longer exists

#### Scenario: Deleting the only dashboard is rejected
- **WHEN** an Owner calls `DELETE` on the only dashboard remaining in the household
- **THEN** HTTP 400 is returned

#### Scenario: Contributor cannot delete a dashboard
- **WHEN** a Contributor calls the delete dashboard endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Replace dashboard widget layout
The system SHALL provide `PUT /api/v1/households/{householdId}/dashboards/{dashboardId}/widgets` (Owner or Contributor only) accepting a full ordered array of widget definitions `[{ widgetType, widgetSize, config }]`. The server SHALL atomically replace the existing widget layout with the new one (delete all existing widgets, insert new ones) within a single database transaction. `position` is derived from array order (index 0 = position 0). Duplicate `widgetType` values within a single layout are permitted (e.g., two DueSoon widgets with different configs).

Valid `widgetType` values: `AssetCount`, `CylinderIndex`, `TotalDisplacement`, `TotalHorsepower`, `TotalTorque`, `DueSoon`, `RecentActivity`, `FuelCostYtd`, `MileageMtd`.

Valid `widgetSize` values: `Small` (1/4 grid column), `Wide` (1/2 grid column), `Full` (full row).

#### Scenario: Contributor replaces the widget layout
- **WHEN** a Contributor PUTs an array of three widget definitions to the widgets endpoint
- **THEN** HTTP 200 is returned and the dashboard now has exactly those three widgets in the specified order

#### Scenario: Empty widget array clears the dashboard
- **WHEN** a Contributor PUTs an empty array `[]`
- **THEN** HTTP 200 is returned and the dashboard has no widgets

#### Scenario: Invalid widgetType is rejected
- **WHEN** a Contributor PUTs a widget with `widgetType: "WeatherForecast"` (not in catalog)
- **THEN** HTTP 400 is returned

#### Scenario: Viewer cannot modify the widget layout
- **WHEN** a Viewer PUTs to the widgets endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Dashboard snapshot
The system SHALL provide `GET /api/v1/households/{householdId}/dashboards/{dashboardId}/snapshot` (any Active member or PlatformAdmin) returning computed data for every widget in the dashboard's current layout. The response is a JSON object keyed by widget type, containing only the widget types present in the layout. All values are computed via SQL-level aggregation — no full entity trees are loaded into application memory.

**Widget data shapes:**
- `AssetCount`: `{ "count": <int> }`
- `CylinderIndex`: `{ "totalCylinders": <int>, "engineCount": <int> }`
- `TotalDisplacement`: `{ "totalCc": <decimal>, "engineCount": <int> }`
- `TotalHorsepower`: `{ "totalHp": <decimal>, "engineCount": <int> }`
- `TotalTorque`: `{ "totalNm": <decimal>, "engineCount": <int> }`
- `DueSoon`: `{ "items": [{ "assetId", "assetName", "recordType": "Registration"|"Warranty", "expiresOn", "urgency": "Overdue"|"DueSoon"|"Upcoming" }] }`
- `RecentActivity`: `{ "items": [{ "assetId", "assetName", "description", "performedOn", "cost" }] }`
- `FuelCostYtd`: `{ "totalCost": <decimal>, "logCount": <int> }`
- `MileageMtd`: `{ "totalMiles": <decimal>, "logCount": <int> }`

**DueSoon urgency thresholds (server-side, not configurable per widget instance):**
- `Overdue`: `expiresOn < today`
- `DueSoon`: `expiresOn` is within 7 days from today
- `Upcoming`: `expiresOn` is within `daysAhead` days from today (from widget config, default 30), excluding Overdue and DueSoon

**DueSoon widget config schema:** `{ "daysAhead": <int, default 30> }`  
**RecentActivity widget config schema:** `{ "limit": <int, default 5, max 20> }`

#### Scenario: Snapshot returns only widgets in the dashboard layout
- **WHEN** a dashboard contains only `AssetCount` and `DueSoon` widgets, and a member requests the snapshot
- **THEN** the response JSON contains exactly the keys `AssetCount` and `DueSoon`

#### Scenario: DueSoon widget returns items sorted by expiry date ascending
- **WHEN** the dashboard contains a DueSoon widget and the household has registrations and warranties with upcoming expiry dates
- **THEN** the snapshot's `DueSoon.items` are ordered by `expiresOn` ascending

#### Scenario: DueSoon urgency classification
- **WHEN** a household has a registration expired yesterday, a warranty expiring in 3 days, and a registration expiring in 20 days (with default 30-day window)
- **THEN** the snapshot DueSoon items have urgency `Overdue`, `DueSoon`, and `Upcoming` respectively

#### Scenario: RecentActivity respects the limit config
- **WHEN** a dashboard has a RecentActivity widget with `config: { "limit": 3 }` and the household has 10 service records
- **THEN** the snapshot returns exactly 3 items in `RecentActivity.items`

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
