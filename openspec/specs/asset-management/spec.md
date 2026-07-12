# asset-management Specification

## Purpose
Defines asset CRUD endpoints and lifecycle (create, list, get, update, delete) across all household asset categories.

## Requirements
### Requirement: Create asset
The system SHALL provide `POST /api/households/{householdId}/assets` (Contributor or Owner only) accepting a `CreateAssetRequest` with a required `category` (an `AssetCategory` value), required `name`, optional `usageTrackingMode`, and optional shared/type-specific fields. The server SHALL derive the structural class (Vehicle | Boat | Trailer | Equipment) from the asset type registry entry for the category. On success it SHALL create the asset scoped to `householdId` and return HTTP 201 with the created `AssetResponse`. An unknown `category` value SHALL return HTTP 400.

A non-null value in a type-specific field that is not listed in the category's registry `applicableFields` SHALL return HTTP 400 with a validation error naming the field.

When `usageTrackingMode` is omitted or null, the created asset SHALL use the registry's `defaultUsageTrackingMode` for the category.

#### Scenario: Contributor creates a boat
- **WHEN** a Contributor POSTs to `/api/households/{householdId}/assets` with `category: "Boat"`, `name: "Sea Ray"`, and `hin: "ABC12345D404"`
- **THEN** HTTP 201 is returned with an `AssetResponse` including `category: "Boat"`, `structuralType: "Boat"`, the generated `id`, and `hin`

#### Scenario: Category maps to structural class
- **WHEN** a Contributor POSTs with `category: "Snowmobile"` and `trackLengthIn: 137`
- **THEN** HTTP 201 is returned with `structuralType: "Vehicle"` and the asset row persists with the Vehicle discriminator

#### Scenario: Inapplicable field rejected
- **WHEN** a POST request has `category: "Car"` and a non-null `maxPsi`
- **THEN** HTTP 400 is returned with a validation error identifying `maxPsi` as not applicable to the category

#### Scenario: Usage tracking mode defaults from registry
- **WHEN** a POST request with `category: "Car"` omits `usageTrackingMode`
- **THEN** the created asset has the registry default for Car (e.g. `Mileage`) rather than `None`

#### Scenario: Viewer cannot create an asset
- **WHEN** a user with `Role = Viewer` POSTs to `/api/households/{householdId}/assets`
- **THEN** HTTP 403 is returned

#### Scenario: Unknown category rejected
- **WHEN** a POST request has `category: "Spaceship"`
- **THEN** HTTP 400 is returned with a validation error

#### Scenario: Non-member cannot create an asset in a foreign household
- **WHEN** a user with no membership in `householdId` POSTs to that household's assets endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: List household assets
The system SHALL provide `GET /api/households/{householdId}/assets` (any Active member or PlatformAdmin) returning all assets belonging to the household, each including its `category`, `structuralType`, and only the fields relevant to that category populated. The endpoint SHALL support an optional `?category=` query parameter to filter results to a single category and an optional `?group=` query parameter to filter to a registry group (Road | Powersport | Water | Trailer | Equipment).

#### Scenario: Member lists all household assets
- **WHEN** a user with any Active role calls `GET /api/households/{householdId}/assets`
- **THEN** HTTP 200 is returned with an array of `AssetResponse` covering every asset category present in the household

#### Scenario: Filter by category
- **WHEN** a user calls `GET /api/households/{householdId}/assets?category=Boat`
- **THEN** only assets with `category: "Boat"` are returned

#### Scenario: Filter by group
- **WHEN** a household contains a Utv, a Snowmobile, and a Car, and a user calls `GET /api/households/{householdId}/assets?group=Powersport`
- **THEN** only the Utv and Snowmobile are returned

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
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}` (Contributor or Owner only) accepting an `UpdateAssetRequest` with the same shared/type-specific fields as create, excluding `category` (immutable after creation). Type-specific field applicability SHALL be validated against the asset's category registry entry, as on create. On success it SHALL return HTTP 200 with the updated `AssetResponse`.

#### Scenario: Contributor updates an asset's name
- **WHEN** a Contributor PUTs `/api/households/{householdId}/assets/{assetId}` with a new `name`
- **THEN** HTTP 200 is returned with the updated `AssetResponse`

#### Scenario: Viewer cannot update an asset
- **WHEN** a user with `Role = Viewer` PUTs to the update endpoint
- **THEN** HTTP 403 is returned

#### Scenario: category in update body is rejected
- **WHEN** an `UpdateAssetRequest` body includes a `category` field that differs from the asset's actual category
- **THEN** HTTP 400 is returned

#### Scenario: Inapplicable field rejected on update
- **WHEN** a PUT for an asset with `category: "EnclosedTrailer"` includes a non-null `vin`
- **THEN** HTTP 400 is returned with a validation error identifying `vin` as not applicable

---

### Requirement: Delete asset
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}` (Owner only). On success the asset and any Engines attached to it SHALL be deleted (cascade) and HTTP 204 returned.

#### Scenario: Owner deletes an asset
- **WHEN** an Owner calls `DELETE /api/households/{householdId}/assets/{assetId}`
- **THEN** HTTP 204 is returned and the asset and its engines no longer exist

#### Scenario: Contributor cannot delete an asset
- **WHEN** a Contributor calls the delete endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Derived powertrain summary on asset responses
`AssetResponse` (and any list/summary projection used for asset cards or dashboard display) SHALL include a derived, non-persisted `powertrain` field computed from the asset's `Active`-status engines only: `null`/absent when the asset has only `Ice` engines or no engines, `"Electric"` when it has only `Electric` engines, `"Hybrid"` when it has at least one `Active` `Ice` engine and at least one `Active` `Electric` engine with `IsExternallyChargeable = false`, and `"Plug-in Hybrid"` when it has at least one `Active` `Ice` engine and at least one `Active` `Electric` engine with `IsExternallyChargeable = true`. This value SHALL NOT be stored on the `Asset` entity; it is computed at read time from the asset's current engines.

#### Scenario: Ice-only asset has no powertrain badge
- **WHEN** an asset has a single `Active` `Ice` engine and its `AssetResponse` is fetched
- **THEN** `powertrain` is absent or `null`

#### Scenario: Pure EV asset shows Electric
- **WHEN** an asset has a single `Active` `Electric` engine and its `AssetResponse` is fetched
- **THEN** `powertrain` is `"Electric"`

#### Scenario: Conventional hybrid shows Hybrid
- **WHEN** an asset has an `Active` `Ice` engine and an `Active` `Electric` engine with `IsExternallyChargeable = false`, and its `AssetResponse` is fetched
- **THEN** `powertrain` is `"Hybrid"`

#### Scenario: Plug-in hybrid shows Plug-in Hybrid
- **WHEN** an asset has an `Active` `Ice` engine and an `Active` `Electric` engine with `IsExternallyChargeable = true`, and its `AssetResponse` is fetched
- **THEN** `powertrain` is `"Plug-in Hybrid"`

#### Scenario: Retired engine does not count toward the powertrain badge
- **WHEN** an asset has an `Active` `Ice` engine and a `Retired` `Electric` engine (e.g. a discontinued EV conversion), and its `AssetResponse` is fetched
- **THEN** `powertrain` is absent or `null` (the retired electric engine is excluded)
