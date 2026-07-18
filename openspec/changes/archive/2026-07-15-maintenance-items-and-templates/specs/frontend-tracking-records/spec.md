## MODIFIED Requirements

### Requirement: List tracking records for an asset
The frontend SHALL list an asset's records for each of the three tracking-log types (mileage logs, engine hours logs, fuel logs), newest first, each on its own routed URL segment under the asset's detail page. Maintenance work is listed separately via the `frontend-maintenance-items` capability, not through this generic tracking-log UI.

#### Scenario: Viewing each log type independently
- **WHEN** a user navigates between the mileage, engine hours, and fuel log tabs on an asset
- **THEN** each tab loads and displays only its own record type at its own URL

#### Scenario: No entries yet
- **WHEN** a tracking log has zero entries for an asset
- **THEN** the app shows an empty state prompting the first entry

---

### Requirement: Create tracking record
The frontend SHALL provide a create form for each of the three tracking-log types (mileage logs, engine hours logs, fuel logs), submitting to that log's `POST` endpoint scoped to the asset, available to Contributors and Owners.

#### Scenario: Logging a fuel entry
- **WHEN** a Contributor/Owner submits a fuel log entry
- **THEN** the app creates it and it appears at the top of the fuel log list

#### Scenario: Viewer cannot create
- **WHEN** a Viewer-role user opens any tracking log tab
- **THEN** the app hides the create-entry control

---

### Requirement: Delete tracking record
The frontend SHALL allow Contributors and Owners (not just Owners) to delete a tracking-log entry, consistent with the backend's `HouseholdOperations.Edit`-gated delete for tracking records.

#### Scenario: Contributor deletes a fuel log entry
- **WHEN** a Contributor confirms deletion of a fuel log entry
- **THEN** the app calls the delete endpoint and the entry is removed from the list without affecting other entries

---

### Requirement: Optional engine association for applicable log types
The frontend SHALL offer an optional engine selector on the create/edit form for tracking-log types that support an `engineId` (engine hours logs), populated from the asset's engine list, and omit the selector for log types that don't (mileage logs). Fuel log entries SHALL follow the auto-select/require-selection behavior described in the "Fuel log engine and unit selection" requirement instead of this generic optional-selector behavior.

#### Scenario: Associating an engine hours log with an engine
- **WHEN** a Contributor/Owner selects one of the asset's engines while logging an engine hours entry
- **THEN** the submitted record includes that `engineId`

#### Scenario: Mileage log has no engine selector
- **WHEN** a Contributor/Owner opens the create-mileage-log form
- **THEN** no engine selector is shown
