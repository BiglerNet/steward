# frontend-tracking-records Specification

## Purpose
TBD - created by archiving change frontend-assets-and-tracking. Update Purpose after archive.
## Requirements
### Requirement: List tracking records for an asset
The frontend SHALL list an asset's records for each of the four tracking-log types (service records, mileage logs, engine hours logs, fuel logs), newest first, each on its own routed URL segment under the asset's detail page.

#### Scenario: Viewing service records
- **WHEN** any household member opens `/households/:householdId/assets/:assetId/service-records`
- **THEN** the app lists that asset's service records ordered by date descending

#### Scenario: Viewing each log type independently
- **WHEN** a user navigates between the service records, mileage, engine hours, and fuel log tabs on an asset
- **THEN** each tab loads and displays only its own record type at its own URL

#### Scenario: No entries yet
- **WHEN** a tracking log has zero entries for an asset
- **THEN** the app shows an empty state prompting the first entry

### Requirement: Create tracking record
The frontend SHALL provide a create form for each tracking-log type, submitting to that log's `POST` endpoint scoped to the asset, available to Contributors and Owners.

#### Scenario: Logging a service record
- **WHEN** a Contributor/Owner submits a service record (date, description, provider, cost, odometer/engine hours, optional engine)
- **THEN** the app creates it and it appears at the top of the service records list

#### Scenario: Logging a fuel entry
- **WHEN** a Contributor/Owner submits a fuel log entry
- **THEN** the app creates it and it appears at the top of the fuel log list

#### Scenario: Viewer cannot create
- **WHEN** a Viewer-role user opens any tracking log tab
- **THEN** the app hides the create-entry control

### Requirement: Edit tracking record
The frontend SHALL provide an edit form for an existing tracking-log entry, available to Contributors and Owners, submitting to that log's `PUT` endpoint.

#### Scenario: Correcting a mileage log entry
- **WHEN** a Contributor/Owner edits an existing mileage log entry's value
- **THEN** the app submits the update and the list reflects the corrected value without reordering unrelated entries

### Requirement: Delete tracking record
The frontend SHALL allow Contributors and Owners (not just Owners) to delete a tracking-log entry, consistent with the backend's `HouseholdOperations.Edit`-gated delete for tracking records.

#### Scenario: Contributor deletes a service record
- **WHEN** a Contributor confirms deletion of a service record
- **THEN** the app calls the delete endpoint and the entry is removed from the list without affecting other entries

### Requirement: Optional engine association for applicable log types
The frontend SHALL offer an optional engine selector on the create/edit form for tracking-log types that support an `engineId` (service records, engine hours logs), populated from the asset's engine list, and omit the selector for log types that don't (mileage logs, fuel logs).

#### Scenario: Associating a service record with an engine
- **WHEN** a Contributor/Owner selects one of the asset's engines while logging a service record
- **THEN** the submitted record includes that `engineId`

#### Scenario: Mileage log has no engine selector
- **WHEN** a Contributor/Owner opens the create-mileage-log form
- **THEN** no engine selector is shown

