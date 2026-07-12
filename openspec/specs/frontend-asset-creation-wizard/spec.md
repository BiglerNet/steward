# frontend-asset-creation-wizard Specification

## Purpose
TBD - created by archiving change asset-creation-wizard. Update Purpose after archive.

## Requirements
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
The Type step SHALL present one selectable compact row per registry category (the category's `AssetTypeIcon` chip plus display label, radio-group semantics), grouped under the registry `group` headings and laid out in two columns on desktop so that all categories fit a typical desktop viewport without scrolling. Selecting a category SHALL prefill `usageTrackingMode` with the registry default (editable later in Details). Returning to the Type step and changing the category SHALL clear entered values for fields not applicable to the new category.

#### Scenario: Categories grouped by registry group
- **WHEN** a user views the Type step
- **THEN** category rows appear under their registry group headings (e.g. Utv and Snowmobile under Powersport), each with its lucide icon chip

#### Scenario: Desktop picker fits without scrolling
- **WHEN** a user views the Type step at a typical desktop viewport height
- **THEN** every category row is visible without scrolling the page

#### Scenario: Changing type clears inapplicable fields
- **WHEN** a user who entered a VIN for `Car` goes back and selects a category without `vin` in its `applicableFields`
- **THEN** the VIN value is cleared and the VIN step no longer appears

### Requirement: Optional VIN decode with prefill
The VIN step SHALL explain what decoding provides (prefilled year, make, model, and engine specs, all editable) and SHALL accept a VIN with no separate decode action: advancing with a well-formed 17-character VIN SHALL run the decode (`GET /api/vin-decode/{vin}`) with an inline pending state and then advance regardless of outcome. A successful decode SHALL prefill Details (`make`, `model`, `year`) and Engine (`cylinders`, `displacement`, `horsepower`, `torque`, and `fuelType` when the decoded values map) as ordinary editable values, and the Details step SHALL show a confirmation of what was found (e.g. "Found: 2015 Ford F-150" from the decoded year/make/model). A failed or empty decode SHALL carry a "couldn't decode — enter details manually" notice into Details instead. A non-empty malformed VIN SHALL block advancing with inline validation; an explicit Skip SHALL always be available; the step SHALL never hard-block on decode outcome. Categories with `vinDecodeSupport: BestEffort` SHALL show a caveat that decoded data may be sparse. When the decoded body class or vehicle type does not plausibly match the selected category, the wizard SHALL show an informational hint only — never block or clear input.

#### Scenario: Decode runs on Continue and confirms its result
- **WHEN** a user enters a valid VIN and clicks Continue
- **THEN** the decode runs with a pending state, the wizard advances to Details with prefilled values, and a confirmation like "Found: 2015 Ford F-150" is shown

#### Scenario: Decode failure never blocks
- **WHEN** the decode call returns HTTP 502 or decodes to nothing
- **THEN** the wizard still advances to Details, showing a "couldn't decode — enter details manually" notice

#### Scenario: Malformed VIN gets inline validation
- **WHEN** a user enters a 12-character value and clicks Continue
- **THEN** the step shows inline VIN-format validation and does not advance until the value is fixed, cleared, or the step is skipped

#### Scenario: User is told why the VIN is worth entering
- **WHEN** a user reaches the VIN step
- **THEN** copy explains that the VIN prefills year, make, model, and engine specs

#### Scenario: Body-class mismatch is a soft hint
- **WHEN** the user selected `Car` and the decode returns a motorcycle body class
- **THEN** an informational hint appears suggesting the type be double-checked, with no enforcement

### Requirement: Creation on advancing past the last data step
The wizard SHALL create the asset via the existing `POST /api/households/{householdId}/assets` when the user advances past the Engine step (or the Details step when the Engine step is not shown or is skipped), followed by one `POST .../assets/{assetId}/engines` call when a single engine's data was entered, or two sequential `POST .../assets/{assetId}/engines` calls when the Engine step's engine-type choice was `Hybrid` or `Plug-in Hybrid`. If any engine call fails after the asset is created, the wizard SHALL surface the error on the Engine step with retry and skip options while keeping the created asset and any engine(s) already successfully created.

#### Scenario: Asset and engine created together
- **WHEN** a user completes Details and Engine and advances with engine type `Ice`
- **THEN** the asset is created first and one engine is created against the new asset id

#### Scenario: Hybrid selection creates two engines
- **WHEN** a user completes Details and Engine with engine type `Hybrid` and advances
- **THEN** the asset is created first, followed by two engine records against the new asset id — one `EngineType: "Ice"`, one `EngineType: "Electric"` with `isExternallyChargeable: false`

#### Scenario: Engine failure keeps the asset
- **WHEN** the asset creation succeeds but an engine creation fails
- **THEN** the wizard shows the engine error with retry/skip options and the asset (and any already-created engine) remains

### Requirement: Engine step engine-type selection
The Engine step SHALL offer four engine-type choices — `Ice`, `Electric`, `Hybrid`, `Plug-in Hybrid` — even though only `Ice` and `Electric` are ever persisted as an `Engine.EngineType` value. Selecting `Ice` or `Electric` SHALL show a single set of engine fields (label, make, model, mechanism/fuel type fields when `Ice`, etc.) resulting in one engine created. Selecting `Hybrid` or `Plug-in Hybrid` SHALL show two labeled field groups — one for the gas engine, one for the electric motor — resulting in two engines created together: an `Ice` engine from the first group, and an `Electric` engine from the second group with `isExternallyChargeable` set to `false` for `Hybrid` or `true` for `Plug-in Hybrid`.

#### Scenario: Ice selection shows one field group
- **WHEN** a user selects engine type `Ice` in the Engine step
- **THEN** a single group of engine fields is shown, including `mechanism` and `fuelType` options

#### Scenario: Hybrid selection shows two field groups
- **WHEN** a user selects engine type `Hybrid` in the Engine step
- **THEN** two labeled field groups appear — a gas engine group and an electric motor group — and no `isExternallyChargeable` control is shown to the user (it is implied `false` by the `Hybrid` choice)

#### Scenario: Plug-in Hybrid selection implies external chargeability
- **WHEN** a user selects engine type `Plug-in Hybrid` in the Engine step and completes the wizard
- **THEN** the created electric engine has `isExternallyChargeable: true`

#### Scenario: Switching away from Hybrid clears the second field group
- **WHEN** a user who selected `Hybrid` and filled both field groups switches the engine-type choice to `Ice`
- **THEN** the electric motor field group is hidden and its entered values are discarded

### Requirement: Engine step captures performance specs
The wizard's Engine step SHALL include optional horsepower and torque inputs alongside the existing fuel type, cylinders, and displacement fields, following the same unit conventions as the asset detail engine form, and SHALL submit them on the existing create-engine request.

#### Scenario: Horsepower and torque entered during creation
- **WHEN** a user fills horsepower and torque in the Engine step and completes the wizard
- **THEN** the created engine carries those values and household dashboard widgets that aggregate horsepower/torque include them

### Requirement: Photos step and completion
The Photos step SHALL upload against the created asset using the existing asset-photos endpoints and upload UI patterns, and SHALL be skippable. Finishing the wizard — with or without photos — SHALL navigate to the new asset's detail page.

#### Scenario: Photos uploaded during creation
- **WHEN** a user uploads two photos in the Photos step and finishes
- **THEN** the photos exist on the new asset and the app lands on its detail page

#### Scenario: Skipping photos
- **WHEN** a user skips the Photos step
- **THEN** the app navigates to the new asset's detail page with no photos
