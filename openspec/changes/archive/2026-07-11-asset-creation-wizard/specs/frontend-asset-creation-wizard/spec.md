## ADDED Requirements

### Requirement: Full-page creation wizard with registry-gated steps
The frontend SHALL provide asset creation as a full-page wizard at `/households/:householdId/assets/new` with steps **Type → VIN → Details → Engine → Photos**, where the VIN step appears only when the selected category's registry `vinDecodeSupport` is not `None` and the Engine step appears only when the registry's `typicallyHasEngine` is true. The wizard SHALL be available to Contributor/Owner members only; Viewers navigating to the route SHALL be redirected to the asset list. Leaving the wizard before the asset is created SHALL persist nothing.

#### Scenario: Car gets the full flow
- **WHEN** a Contributor selects `Car` in the Type step
- **THEN** the wizard offers VIN, Details, Engine, and Photos steps in order

#### Scenario: Trailer skips VIN and Engine steps
- **WHEN** a Contributor selects a trailer category whose registry entry has `vinDecodeSupport: None` and `typicallyHasEngine: false`
- **THEN** the wizard goes Type → Details → Photos with no VIN or Engine step shown

#### Scenario: Viewer cannot reach the wizard
- **WHEN** a Viewer navigates directly to `/households/:householdId/assets/new`
- **THEN** the app redirects to the asset list

#### Scenario: Abandoning before creation persists nothing
- **WHEN** a user fills the Type and Details steps and then navigates away before the creation call fires
- **THEN** no asset exists in the household

### Requirement: Type step with grouped category cards
The Type step SHALL present one selectable card per registry category, grouped under the registry `group` headings, using registry display labels and icon colors. Selecting a category SHALL prefill `usageTrackingMode` with the registry default (editable later in Details). Returning to the Type step and changing the category SHALL clear entered values for fields not applicable to the new category.

#### Scenario: Categories grouped by registry group
- **WHEN** a user views the Type step
- **THEN** category cards appear under their registry group headings (e.g. Utv and Snowmobile under Powersport)

#### Scenario: Changing type clears inapplicable fields
- **WHEN** a user who entered a VIN for `Car` goes back and selects a category without `vin` in its `applicableFields`
- **THEN** the VIN value is cleared and the VIN step no longer appears

### Requirement: Optional VIN decode with prefill
The VIN step SHALL accept a VIN and offer a decode action calling `GET /api/vin-decode/{vin}`, enabled only when the input matches the 17-character VIN format. A successful decode SHALL prefill Details (`make`, `model`, `year`) and Engine (`cylinders`, `displacement`, and `fuelType` when the decoded string maps to a known `FuelType`) as ordinary editable values. The step SHALL always be skippable, decode failures (including HTTP 502) SHALL show a notice and let the user continue manually, and categories with `vinDecodeSupport: BestEffort` SHALL show a caveat that decoded data may be sparse. When the decoded body class or vehicle type does not plausibly match the selected category, the wizard SHALL show an informational hint only — never block or clear input.

#### Scenario: Decode prefills details and engine
- **WHEN** a user decodes a valid VIN that returns make, model, year, and engine data
- **THEN** the Details and Engine steps show those values prefilled and editable

#### Scenario: Decode failure never blocks
- **WHEN** the decode call returns HTTP 502
- **THEN** the wizard shows a "couldn't decode, continue manually" notice and the user can proceed to Details

#### Scenario: Body-class mismatch is a soft hint
- **WHEN** the user selected `Car` and the decode returns a motorcycle body class
- **THEN** an informational hint appears suggesting the type be double-checked, with no enforcement

### Requirement: Creation on advancing past the last data step
The wizard SHALL create the asset via the existing `POST /api/households/{householdId}/assets` when the user advances past the Engine step (or the Details step when the Engine step is not shown or is skipped), followed by `POST .../assets/{assetId}/engines` when engine data was entered. If the engine call fails after the asset is created, the wizard SHALL surface the error on the Engine step with retry and skip options while keeping the created asset.

#### Scenario: Asset and engine created together
- **WHEN** a user completes Details and Engine and advances
- **THEN** the asset is created first and the engine is created against the new asset id

#### Scenario: Engine failure keeps the asset
- **WHEN** the asset creation succeeds but the engine creation fails
- **THEN** the wizard shows the engine error with retry/skip options and the asset remains

### Requirement: Photos step and completion
The Photos step SHALL upload against the created asset using the existing asset-photos endpoints and upload UI patterns, and SHALL be skippable. Finishing the wizard — with or without photos — SHALL navigate to the new asset's detail page.

#### Scenario: Photos uploaded during creation
- **WHEN** a user uploads two photos in the Photos step and finishes
- **THEN** the photos exist on the new asset and the app lands on its detail page

#### Scenario: Skipping photos
- **WHEN** a user skips the Photos step
- **THEN** the app navigates to the new asset's detail page with no photos
