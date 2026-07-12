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
The frontend SHALL offer an optional engine selector on the create/edit form for tracking-log types that support an `engineId` (service records, engine hours logs), populated from the asset's engine list, and omit the selector for log types that don't (mileage logs). Fuel log entries SHALL follow the auto-select/require-selection behavior described in the "Fuel log engine and unit selection" requirement instead of this generic optional-selector behavior.

#### Scenario: Associating a service record with an engine
- **WHEN** a Contributor/Owner selects one of the asset's engines while logging a service record
- **THEN** the submitted record includes that `engineId`

#### Scenario: Mileage log has no engine selector
- **WHEN** a Contributor/Owner opens the create-mileage-log form
- **THEN** no engine selector is shown

### Requirement: Fuel log engine and unit selection
The fuel log create/edit form SHALL submit `quantity` and `unit` (`Gallons` | `Liters` | `Kwh`) instead of `volume`/`volumeUnit`. The form SHALL compute the asset's "loggable engines" as its `Active` `Ice` engines plus its `Active` `Electric` engines where `isExternallyChargeable = true`. When the asset has zero loggable engines, the form SHALL behave as today: no engine selector, `unit` freely chosen. When the asset has exactly one loggable engine, the form SHALL submit that engine's id as `engineId` automatically without showing an engine selector, and SHALL constrain `unit` to the units that engine supports (`Gallons`/`Liters` for `Ice`, `Kwh` for `Electric`). When the asset has two or more loggable engines, the form SHALL require the user to pick which engine the entry is for before allowing submission, and SHALL constrain `unit` to the selected engine's supported units.

#### Scenario: Single-engine asset auto-selects with no prompt
- **WHEN** a Contributor/Owner opens the create-fuel-log form for an asset with exactly one `Active` `Ice` engine
- **THEN** no engine selector is shown, `unit` offers `Gallons`/`Liters` only, and the submitted entry includes that engine's `engineId`

#### Scenario: Conventional hybrid never prompts for engine choice
- **WHEN** a Contributor/Owner opens the create-fuel-log form for an asset with an `Active` `Ice` engine and an `Active` `Electric` engine where `isExternallyChargeable = false`
- **THEN** no engine selector is shown, `unit` offers `Gallons`/`Liters` only, and the submitted entry includes the `Ice` engine's `engineId`

#### Scenario: Plug-in hybrid requires picking gas or electric
- **WHEN** a Contributor/Owner opens the create-fuel-log form for an asset with an `Active` `Ice` engine and an `Active` `Electric` engine where `isExternallyChargeable = true`
- **THEN** an engine selector is shown offering both engines, and `unit`'s available options follow whichever engine is selected (`Gallons`/`Liters` for the Ice engine, `Kwh` for the Electric engine)

#### Scenario: Asset with no modeled engines keeps free unit choice
- **WHEN** a Contributor/Owner opens the create-fuel-log form for an asset with no `Engine` records
- **THEN** no engine selector is shown, all three units are selectable, and `engineId` is submitted as `null`

### Requirement: Engine status transition controls
The engine list on an asset's detail page SHALL provide Retire, Reactivate, and Mark Broken actions (calling the corresponding existing backend endpoints), available to Contributors and Owners, alongside the existing Edit/Delete actions. Each action SHALL only be offered when valid for the engine's current `Status` (e.g. Retire and Mark Broken are not offered on an already-`Retired` engine; Reactivate is only offered on `Retired` or `Broken` engines).

#### Scenario: Contributor retires an engine
- **WHEN** a Contributor clicks Retire on an `Active` engine row
- **THEN** the app calls the retire endpoint and the row's status updates to `Retired`

#### Scenario: Contributor reactivates a retired engine
- **WHEN** a Contributor clicks Reactivate on a `Retired` engine row
- **THEN** the app calls the reactivate endpoint and the row's status updates to `Active`

#### Scenario: Retire action hidden on an already-retired engine
- **WHEN** a Contributor views a `Retired` engine's row
- **THEN** no Retire action is shown, only Reactivate (and Delete, if permitted)

