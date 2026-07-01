## MODIFIED Requirements

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

## ADDED Requirements

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
