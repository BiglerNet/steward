## MODIFIED Requirements

### Requirement: Type-adaptive asset create/edit form
The frontend SHALL provide an edit form (dialog) for assets whose visible type-specific fields adapt to the asset's `AssetCategory` as defined by the registry's `applicableFields`, submitting to `PUT /api/households/{householdId}/assets/{assetId}`. The category SHALL not be changeable on edit. Asset creation SHALL NOT use this dialog: create controls (the asset list's new-asset action and the empty state's prompt) SHALL navigate to the creation wizard at `/households/:householdId/assets/new`. The registry-driven type-specific field inputs SHALL be shared between the edit dialog and the wizard's Details step rather than duplicated.

#### Scenario: Editing a Car
- **WHEN** a Contributor/Owner edits a Car
- **THEN** the dialog renders exactly the type-specific inputs the registry lists for Car and submits an update request, with inapplicable fields omitted or null

#### Scenario: Create actions open the wizard
- **WHEN** a Contributor clicks the new-asset action on the asset list (or the empty state's prompt)
- **THEN** the app navigates to `/households/:householdId/assets/new` instead of opening a dialog

#### Scenario: Category not changeable on edit
- **WHEN** a user edits an existing asset
- **THEN** the category is displayed but cannot be changed

#### Scenario: Viewer cannot create or edit
- **WHEN** a Viewer-role user opens the asset list
- **THEN** the app hides create/edit controls
