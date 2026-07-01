## ADDED Requirements

### Requirement: Create engine
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/engines` (Contributor or Owner only) accepting `{ label, make, model, serialNumber, year, engineType, fuelType, cylinders, displacementCc, installedDate, installedAtAssetMiles, installedAtAssetHours }` with `label` required. New engines SHALL default to `Status = Active`. On success it SHALL return HTTP 201 with the created `EngineResponse`.

#### Scenario: Contributor adds an engine to a boat
- **WHEN** a Contributor POSTs to `/api/households/{householdId}/assets/{assetId}/engines` with `label: "Port"` for a twin-engine boat
- **THEN** HTTP 201 is returned with an `EngineResponse` having `status: "Active"`

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
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/engines/{engineId}` (Contributor or Owner only) accepting the same fields as create, excluding `status` (changed only via the dedicated retire/reactivate endpoints). On success it SHALL return HTTP 200 with the updated `EngineResponse`.

#### Scenario: Contributor updates engine serial number
- **WHEN** a Contributor PUTs the engine endpoint with a corrected `serialNumber`
- **THEN** HTTP 200 is returned with the updated `EngineResponse`

#### Scenario: Viewer cannot update an engine
- **WHEN** a user with `Role = Viewer` PUTs the update engine endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Retire and reactivate engine
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/retire` and `POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/reactivate` (Contributor or Owner only) to toggle `Status` between `Active` and `Retired` without deleting the engine record. Retiring an Active engine that is already Retired (or reactivating one that is already Active) SHALL return HTTP 400.

#### Scenario: Contributor retires an engine being replaced
- **WHEN** a Contributor POSTs to the retire endpoint for an Active engine
- **THEN** HTTP 200 is returned and the engine's `status` becomes `Retired`

#### Scenario: Retiring an already-retired engine rejected
- **WHEN** a Contributor POSTs to the retire endpoint for an engine that is already `Retired`
- **THEN** HTTP 400 is returned

#### Scenario: Reactivating an engine
- **WHEN** an Owner POSTs to the reactivate endpoint for a `Retired` engine
- **THEN** HTTP 200 is returned and the engine's `status` becomes `Active`

---

### Requirement: Delete engine
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/engines/{engineId}` (Owner only). On success the engine record SHALL be permanently removed and HTTP 204 returned.

#### Scenario: Owner deletes an engine record added in error
- **WHEN** an Owner calls `DELETE` on the engine endpoint
- **THEN** HTTP 204 is returned and the engine no longer exists

#### Scenario: Contributor cannot delete an engine
- **WHEN** a Contributor calls the delete engine endpoint
- **THEN** HTTP 403 is returned
