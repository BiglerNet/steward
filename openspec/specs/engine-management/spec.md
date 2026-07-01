### Requirement: Engine carries extended specification fields
The `Engine` entity SHALL support six additional optional specification fields: `HorsepowerHp` (decimal?, SAE horsepower), `TorqueNm` (decimal?, Newton-metres), `OilCapacityL` (decimal?, litres), `RecommendedOilType` (string?), `CoolantCapacityL` (decimal?, litres), and `RecommendedOctane` (int?). All fields are optional and nullable. These fields SHALL be included in `EngineResponse`, `CreateEngineRequest`, and `UpdateEngineRequest`. The create and update endpoints SHALL accept and persist these values when provided.

#### Scenario: Contributor records oil capacity and type when creating an engine
- **WHEN** a Contributor POSTs to the create engine endpoint with `oilCapacityL: 4.7` and `recommendedOilType: "5W-30 Full Synthetic"`
- **THEN** HTTP 201 is returned with an `EngineResponse` reflecting those values

#### Scenario: Fields are optional — omitting them is valid
- **WHEN** a Contributor POSTs to the create engine endpoint without any of the six new fields
- **THEN** HTTP 201 is returned with all six new fields as `null` in the `EngineResponse`

#### Scenario: Contributor updates an existing engine to add horsepower and torque
- **WHEN** a Contributor PUTs the update engine endpoint with `horsepowerHp: 355` and `torqueNm: 475`
- **THEN** HTTP 200 is returned and the `EngineResponse` reflects the new values

#### Scenario: Recommended octane accepts standard values only
- **WHEN** a Contributor submits `recommendedOctane: 94` (not a standard value)
- **THEN** HTTP 400 is returned

---

### Requirement: Engine status includes a Broken state
The `EngineStatus` enum SHALL have three values: `Active`, `Retired`, and `Broken`. `Active` means the engine is installed and operational. `Broken` means the engine is installed but non-functional. `Retired` means the engine has been removed from service. The `EngineResponse` SHALL expose the `status` field reflecting the current state.

#### Scenario: A newly created engine defaults to Active
- **WHEN** a Contributor creates a new engine
- **THEN** the returned `EngineResponse` has `status: "Active"`

#### Scenario: An engine can be marked broken
- **WHEN** a Contributor POSTs to `POST .../engines/{engineId}/mark-broken` for an `Active` engine
- **THEN** HTTP 200 is returned and `status` becomes `Broken`

#### Scenario: A broken engine can be reactivated
- **WHEN** a Contributor POSTs to `.../engines/{engineId}/reactivate` for a `Broken` engine
- **THEN** HTTP 200 is returned and `status` becomes `Active`

#### Scenario: A broken engine can be retired
- **WHEN** a Contributor POSTs to `.../engines/{engineId}/retire` for a `Broken` engine
- **THEN** HTTP 200 is returned and `status` becomes `Retired`

#### Scenario: Marking an already-broken engine broken is rejected
- **WHEN** a Contributor POSTs to `.../engines/{engineId}/mark-broken` for a `Broken` engine
- **THEN** HTTP 400 is returned

#### Scenario: Marking a retired engine broken is rejected
- **WHEN** a Contributor POSTs to `.../engines/{engineId}/mark-broken` for a `Retired` engine
- **THEN** HTTP 400 is returned

---

### Requirement: Create engine
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/engines` (Contributor or Owner only) accepting `{ label, make, model, serialNumber, year, engineType, fuelType, cylinders, displacementCc, installedDate, installedAtAssetMiles, installedAtAssetHours, horsepowerHp, torqueNm, oilCapacityL, recommendedOilType, coolantCapacityL, recommendedOctane }` with `label` required. All six new spec fields are optional. New engines SHALL default to `Status = Active`. On success it SHALL return HTTP 201 with the created `EngineResponse` including all spec fields.

#### Scenario: Contributor adds an engine to a boat
- **WHEN** a Contributor POSTs to `/api/households/{householdId}/assets/{assetId}/engines` with `label: "Port"` for a twin-engine boat
- **THEN** HTTP 201 is returned with an `EngineResponse` having `status: "Active"` and null spec fields

#### Scenario: Contributor adds an engine with full spec data
- **WHEN** a Contributor POSTs with `label: "Main"`, `cylinders: 8`, `horsepowerHp: 355`, `torqueNm: 475`, `oilCapacityL: 5.7`, `recommendedOilType: "5W-30 Full Synthetic"`, `recommendedOctane: 91`
- **THEN** HTTP 201 is returned with all provided values reflected in the `EngineResponse`

#### Scenario: Viewer cannot add an engine
- **WHEN** a user with `Role = Viewer` POSTs to the create engine endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Missing label rejected
- **WHEN** a create engine request omits `label`
- **THEN** HTTP 400 is returned

#### Scenario: Asset belongs to a different household
- **WHEN** `assetId` does not belong to `householdId` in the route
- **THEN** HTTP 404 is returned

---

### Requirement: List engines for an asset
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/engines` (any Active member or PlatformAdmin) returning all engines (both Active and Retired) attached to the asset.

#### Scenario: Member lists engines including retired ones
- **WHEN** a user with any Active role calls the list engines endpoint for an asset that has one Active and one Retired engine
- **THEN** HTTP 200 is returned with both engines included

---

### Requirement: Update engine
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/engines/{engineId}` (Contributor or Owner only) accepting the same fields as create, excluding `status` (changed only via the dedicated status-transition endpoints). The six new spec fields are included and optional. On success it SHALL return HTTP 200 with the updated `EngineResponse`.

#### Scenario: Contributor updates engine serial number
- **WHEN** a Contributor PUTs the engine endpoint with a corrected `serialNumber`
- **THEN** HTTP 200 is returned with the updated `EngineResponse`

#### Scenario: Contributor adds spec data to an existing engine
- **WHEN** a Contributor PUTs with `horsepowerHp: 200` and `torqueNm: 320` on an engine that previously had null values for those fields
- **THEN** HTTP 200 is returned with the spec values updated in the `EngineResponse`

#### Scenario: Viewer cannot update an engine
- **WHEN** a user with `Role = Viewer` PUTs the update engine endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Retire and reactivate engine
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/retire` (Contributor or Owner only) to transition an engine to `Retired`. An engine in `Active` or `Broken` state can be retired. A `Retired` engine cannot be retired again (HTTP 400).

The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/reactivate` (Contributor or Owner only) to transition a `Retired` or `Broken` engine back to `Active`. An already-`Active` engine cannot be reactivated (HTTP 400).

#### Scenario: Contributor retires an Active engine
- **WHEN** a Contributor POSTs to the retire endpoint for an `Active` engine
- **THEN** HTTP 200 is returned and the engine's `status` becomes `Retired`

#### Scenario: Contributor retires a Broken engine
- **WHEN** a Contributor POSTs to the retire endpoint for a `Broken` engine
- **THEN** HTTP 200 is returned and the engine's `status` becomes `Retired`

#### Scenario: Retiring an already-retired engine rejected
- **WHEN** a Contributor POSTs to the retire endpoint for an engine that is already `Retired`
- **THEN** HTTP 400 is returned

#### Scenario: Reactivating a Retired engine
- **WHEN** an Owner POSTs to the reactivate endpoint for a `Retired` engine
- **THEN** HTTP 200 is returned and the engine's `status` becomes `Active`

#### Scenario: Reactivating a Broken engine
- **WHEN** a Contributor POSTs to the reactivate endpoint for a `Broken` engine
- **THEN** HTTP 200 is returned and the engine's `status` becomes `Active`

#### Scenario: Reactivating an already-Active engine rejected
- **WHEN** a Contributor POSTs to the reactivate endpoint for an `Active` engine
- **THEN** HTTP 400 is returned

---

### Requirement: Mark engine as broken
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/mark-broken` (Contributor or Owner only) to transition an `Active` engine to `Broken`. Calling this endpoint on a `Broken` or `Retired` engine SHALL return HTTP 400.

#### Scenario: Contributor marks an Active engine as Broken
- **WHEN** a Contributor POSTs to `.../mark-broken` for an `Active` engine
- **THEN** HTTP 200 is returned and the engine's `status` becomes `Broken`

#### Scenario: Marking a Broken engine broken again is rejected
- **WHEN** a Contributor POSTs to `.../mark-broken` for an engine already `Broken`
- **THEN** HTTP 400 is returned

#### Scenario: Marking a Retired engine broken is rejected
- **WHEN** a Contributor POSTs to `.../mark-broken` for a `Retired` engine
- **THEN** HTTP 400 is returned

#### Scenario: Viewer cannot mark an engine as broken
- **WHEN** a Viewer POSTs to the mark-broken endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Delete engine
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/engines/{engineId}` (Owner only). On success the engine record SHALL be permanently removed and HTTP 204 returned.

#### Scenario: Owner deletes an engine record added in error
- **WHEN** an Owner calls `DELETE` on the engine endpoint
- **THEN** HTTP 204 is returned and the engine no longer exists

#### Scenario: Contributor cannot delete an engine
- **WHEN** a Contributor calls the delete engine endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Garage Logic aggregate metrics computed from active engine specs
The system SHALL expose four household-level aggregate metrics as part of the dashboard snapshot: Cylinder Index, Total Displacement, Total Horsepower, and Total Torque. All four metrics are computed only from engines with `status = Active`. The Cylinder Index additionally filters to `engineType = Ice` (internal combustion only).

- **Cylinder Index**: `SUM(cylinders)` across Active ICE engines where `cylinders` is not null
- **Total Displacement**: `SUM(displacementCc)` across Active engines where `displacementCc` is not null
- **Total Horsepower**: `SUM(horsepowerHp)` across Active engines where `horsepowerHp` is not null
- **Total Torque**: `SUM(torqueNm)` across Active engines where `torqueNm` is not null

`Broken` and `Retired` engines do NOT contribute to any metric, regardless of their spec values.

#### Scenario: Cylinder Index counts only active ICE engines
- **WHEN** a household has a 4-cyl Active ICE engine, a 4-cyl Broken ICE engine, and a 2-cyl Active Electric engine, and the CylinderIndex widget is in the dashboard
- **THEN** the snapshot returns `totalCylinders: 4` (broken engine excluded; electric engine excluded)

#### Scenario: Total Horsepower includes active electric engines
- **WHEN** a household has an Active ICE engine with `horsepowerHp: 355` and an Active Electric engine with `horsepowerHp: 200`, and the TotalHorsepower widget is in the dashboard
- **THEN** the snapshot returns `totalHp: 555`

#### Scenario: A retired engine with specs does not contribute
- **WHEN** a household has one Active engine (`cylinders: 4`) and one Retired engine (`cylinders: 8`), and the CylinderIndex widget is in the dashboard
- **THEN** the snapshot returns `totalCylinders: 4`

#### Scenario: No active engines yields zero for all metrics
- **WHEN** a household has no Active engines
- **THEN** the snapshot returns `0` (or `null`) for all four Garage Logic metrics
