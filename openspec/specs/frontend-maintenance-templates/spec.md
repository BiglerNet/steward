# frontend-maintenance-templates Specification

## Purpose
Defines the frontend UI for managing household maintenance templates, browsing/duplicating platform templates, and picking a template when quick-creating a maintenance item.

## Requirements
### Requirement: Household template management screen
The frontend SHALL provide a household-scoped route (`/households/:householdId/templates`) listing the household's own templates, with create/edit/delete controls (including step management: add/edit/delete/reorder steps, each with optional engine-scoping and recurrence interval fields) available to Contributors and Owners, and read-only viewing for Viewers.

#### Scenario: Owner creates a household template with steps
- **WHEN** an Owner creates a new template and adds three steps, marking one as engine-scoped with a recurrence interval
- **THEN** the template persists with its steps in the entered order and the engine-scoped step's interval saved

#### Scenario: Viewer cannot edit
- **WHEN** a Viewer opens the household templates screen
- **THEN** no create/edit/delete controls are shown

---

### Requirement: Browsing and duplicating platform templates
The household templates screen SHALL also show the platform template catalog (read-only) alongside the household's own templates, with a "Duplicate to my household" action (Contributor/Owner only) on each platform template that creates an independent, fully editable household copy and navigates to it.

#### Scenario: Duplicating a platform template
- **WHEN** a Contributor selects "Duplicate to my household" on the seeded "Oil change" platform template
- **THEN** a new household-owned template is created with the same steps, and the app navigates to its edit view

#### Scenario: Platform templates are not directly editable from the household screen
- **WHEN** a Contributor views a platform template in the household templates screen
- **THEN** no edit or delete control is shown for it, only "Duplicate to my household"

---

### Requirement: Template picker in the quick-create dialog
The maintenance-item quick-create dialog SHALL include an optional template picker listing templates applicable to the target asset's category (its own household's templates plus the platform catalog, filtered by `applicableCategories`), searchable/filterable by title.

#### Scenario: Picker filters by asset category
- **WHEN** a user opens the quick-create dialog for a `Snowmobile` asset
- **THEN** the template picker lists only templates whose `applicableCategories` is empty or includes `Snowmobile`

#### Scenario: Picking a template previews its steps
- **WHEN** a user selects a template in the picker before submitting
- **THEN** the dialog shows a preview of that template's step count (or step titles) before the item is created
