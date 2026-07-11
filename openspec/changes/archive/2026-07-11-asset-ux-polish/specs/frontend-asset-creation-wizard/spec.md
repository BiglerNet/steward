## MODIFIED Requirements

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

---

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

## ADDED Requirements

### Requirement: Engine step captures performance specs
The wizard's Engine step SHALL include optional horsepower and torque inputs alongside the existing fuel type, cylinders, and displacement fields, following the same unit conventions as the asset detail engine form, and SHALL submit them on the existing create-engine request.

#### Scenario: Horsepower and torque entered during creation
- **WHEN** a user fills horsepower and torque in the Engine step and completes the wizard
- **THEN** the created engine carries those values and household dashboard widgets that aggregate horsepower/torque include them
