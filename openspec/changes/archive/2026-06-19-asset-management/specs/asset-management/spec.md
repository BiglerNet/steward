## ADDED Requirements

### Requirement: Create asset
The system SHALL provide `POST /api/households/{householdId}/assets` (Contributor or Owner only) accepting a `CreateAssetRequest` with a required `assetType` discriminator (one of: `Snowmobile`, `Utv`, `Boat`, `Car`, `Truck`, `SnowmobileTrailer`, `EnclosedTrailer`, `RidingMower`, `PowerWasher`, `SmallEngine`), required `name`, and optional shared/type-specific fields. On success it SHALL create the asset scoped to `householdId` and return HTTP 201 with the created `AssetResponse`. An unknown `assetType` value SHALL return HTTP 400.

#### Scenario: Contributor creates a boat
- **WHEN** a Contributor POSTs to `/api/households/{householdId}/assets` with `assetType: "Boat"`, `name: "Sea Ray"`, and `hin: "ABC12345D404"`
- **THEN** HTTP 201 is returned with an `AssetResponse` including `assetType: "Boat"`, the generated `id`, and `hin`

#### Scenario: Viewer cannot create an asset
- **WHEN** a user with `Role = Viewer` POSTs to `/api/households/{householdId}/assets`
- **THEN** HTTP 403 is returned

#### Scenario: Unknown assetType rejected
- **WHEN** a POST request has `assetType: "Spaceship"`
- **THEN** HTTP 400 is returned with a validation error

#### Scenario: Non-member cannot create an asset in a foreign household
- **WHEN** a user with no membership in `householdId` POSTs to that household's assets endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: List household assets
The system SHALL provide `GET /api/households/{householdId}/assets` (any Active member or PlatformAdmin) returning all assets belonging to the household, each including its `assetType` and only the fields relevant to that type populated. The endpoint SHALL support an optional `?assetType=` query parameter to filter results to a single type.

#### Scenario: Member lists all household assets
- **WHEN** a user with any Active role calls `GET /api/households/{householdId}/assets`
- **THEN** HTTP 200 is returned with an array of `AssetResponse` covering every asset type present in the household

#### Scenario: Filter by asset type
- **WHEN** a user calls `GET /api/households/{householdId}/assets?assetType=Boat`
- **THEN** only assets with `assetType: "Boat"` are returned

#### Scenario: Non-member cannot list a foreign household's assets
- **WHEN** a user with no membership in `householdId` calls the list endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Get asset by ID
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}` (any Active member or PlatformAdmin) returning the full `AssetResponse` for that asset. If the asset does not belong to `householdId`, HTTP 404 SHALL be returned.

#### Scenario: Member views an asset
- **WHEN** a user with any Active role calls `GET /api/households/{householdId}/assets/{assetId}` for an asset in that household
- **THEN** HTTP 200 is returned with the asset's full details

#### Scenario: Asset belongs to a different household
- **WHEN** `assetId` exists but belongs to a different household than `householdId` in the route
- **THEN** HTTP 404 is returned

---

### Requirement: Update asset
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}` (Contributor or Owner only) accepting an `UpdateAssetRequest` with the same shared/type-specific fields as create, excluding `assetType` (immutable after creation). On success it SHALL return HTTP 200 with the updated `AssetResponse`.

#### Scenario: Contributor updates an asset's name
- **WHEN** a Contributor PUTs `/api/households/{householdId}/assets/{assetId}` with a new `name`
- **THEN** HTTP 200 is returned with the updated `AssetResponse`

#### Scenario: Viewer cannot update an asset
- **WHEN** a user with `Role = Viewer` PUTs to the update endpoint
- **THEN** HTTP 403 is returned

#### Scenario: assetType in update body is ignored or rejected
- **WHEN** an `UpdateAssetRequest` body includes an `assetType` field that differs from the asset's actual type
- **THEN** HTTP 400 is returned

---

### Requirement: Delete asset
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}` (Owner only). On success the asset and any Engines attached to it SHALL be deleted (cascade) and HTTP 204 returned.

#### Scenario: Owner deletes an asset
- **WHEN** an Owner calls `DELETE /api/households/{householdId}/assets/{assetId}`
- **THEN** HTTP 204 is returned and the asset and its engines no longer exist

#### Scenario: Contributor cannot delete an asset
- **WHEN** a Contributor calls the delete endpoint
- **THEN** HTTP 403 is returned
