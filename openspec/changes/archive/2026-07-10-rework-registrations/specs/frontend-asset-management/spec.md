## ADDED Requirements

### Requirement: License plate surfaced on asset detail
The frontend SHALL display an asset's `licensePlate`, when populated, prominently in the asset detail header area (not only within the type-specific field list), so the plate is readable without scanning detail fields. The plate SHALL be edited through the existing type-adaptive asset form, where it appears automatically for categories whose registry `applicableFields` include `licensePlate`.

#### Scenario: Plate visible at the top of asset detail
- **WHEN** a member opens the detail page of a Car with `licensePlate: "ABC-1234"`
- **THEN** the plate is shown in the header area of the page

#### Scenario: No plate, no placeholder
- **WHEN** a member opens the detail page of an asset with no `licensePlate` (unset, or a category where it does not apply)
- **THEN** the header shows no plate element

#### Scenario: Plate editable via the asset form
- **WHEN** a Contributor edits a UtilityTrailer and the registry lists `licensePlate` as applicable
- **THEN** the asset form renders a "License plate" input and submits its value like any other type-specific field
