# frontend-asset-management Specification

## Purpose
TBD - created by archiving change frontend-assets-and-tracking. Update Purpose after archive.
## Requirements
### Requirement: Asset list
The frontend SHALL list a household's assets via `GET /api/households/{householdId}/assets`, optionally filtered by asset type, showing name, type, year, and photo when present.

#### Scenario: Viewing assets
- **WHEN** a household member opens `/households/:householdId/assets`
- **THEN** the app lists all assets in that household

#### Scenario: Filtering by type
- **WHEN** a user selects an asset type filter
- **THEN** the list shows only assets of that type

#### Scenario: No assets yet
- **WHEN** a household has zero assets
- **THEN** the app shows an empty state prompting asset creation

### Requirement: Type-adaptive asset create/edit form
The frontend SHALL provide a create/edit form for assets whose visible fields adapt to the selected `AssetType`, submitting to `POST /api/households/{householdId}/assets` or `PUT /api/households/{householdId}/assets/{assetId}`.

#### Scenario: Creating a Car
- **WHEN** a Contributor/Owner selects `Car` as the asset type and fills in the vehicle-specific fields (VIN, make, model, color)
- **THEN** the app submits a `CreateAssetRequest` with those fields populated and boat/trailer/mower-specific fields omitted

#### Scenario: Creating a Boat
- **WHEN** a Contributor/Owner selects `Boat` and fills in HIN, hull material, length, and beam
- **THEN** the app submits a `CreateAssetRequest` with those fields populated

#### Scenario: Changing asset type mid-edit
- **WHEN** a user changes the selected `AssetType` while editing an existing asset
- **THEN** the form clears field values that don't belong to the newly selected type's field group before submission

#### Scenario: Viewer cannot create or edit
- **WHEN** a Viewer-role user opens the asset list
- **THEN** the app hides create/edit controls

### Requirement: Asset detail view
The frontend SHALL show an asset's full details, including type-specific fields, via `GET /api/households/{householdId}/assets/{assetId}`.

#### Scenario: Viewing an asset
- **WHEN** any household member opens an asset's detail page
- **THEN** the app shows its name, type, and all populated type-specific fields

### Requirement: Asset deletion restricted to Owners
The frontend SHALL only show the delete-asset control to Owner-role users, calling `DELETE /api/households/{householdId}/assets/{assetId}`.

#### Scenario: Owner deletes an asset
- **WHEN** an Owner confirms deletion of an asset
- **THEN** the app calls the delete endpoint and navigates back to the asset list

#### Scenario: Contributor cannot see delete control
- **WHEN** a Contributor (non-Owner) views an asset's detail page
- **THEN** the app does not show a delete control

### Requirement: Engine management nested under an asset
The frontend SHALL provide list/create/edit UI for an asset's engines via `GET/POST/PUT /api/households/{householdId}/assets/{assetId}/engines`, with delete restricted to Owners.

#### Scenario: Listing engines
- **WHEN** any household member views an asset's detail page
- **THEN** the app lists its engines with label, type, fuel type, and status

#### Scenario: Adding an engine
- **WHEN** a Contributor/Owner submits a new engine's details
- **THEN** the app creates it and it appears in the asset's engine list

#### Scenario: Deleting an engine restricted to Owners
- **WHEN** a Contributor (non-Owner) views the engine list
- **THEN** the app does not show a delete control for any engine

