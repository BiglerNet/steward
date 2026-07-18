## ADDED Requirements

### Requirement: Create household template
The system SHALL provide `POST /api/households/{householdId}/templates` (Contributor or Owner only) accepting `{ title, description?, applicableCategories? }`. `title` is required. The created template SHALL have `householdId` set to the route household. On success it SHALL return HTTP 201 with the created `TemplateResponse` (with an empty `steps` array).

#### Scenario: Contributor creates a household template
- **WHEN** a Contributor POSTs `{ title: "Spring commissioning", applicableCategories: ["PowerBoat", "Sailboat"] }`
- **THEN** HTTP 201 is returned with a household-owned template (non-null `householdId`)

#### Scenario: Viewer cannot create a template
- **WHEN** a user with `Role = Viewer` POSTs to the create endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: List household templates
The system SHALL provide `GET /api/households/{householdId}/templates` (any Active member or PlatformAdmin) returning the household's own templates, with an optional `?assetCategory=` filter returning only templates whose `applicableCategories` is empty/null or includes the given category.

#### Scenario: Member lists household templates
- **WHEN** any Active member calls the list endpoint
- **THEN** HTTP 200 is returned with the household's templates, not platform templates

#### Scenario: Filtering by asset category
- **WHEN** a member calls the list endpoint with `?assetCategory=Snowmobile` and the household has one template applicable to `["Snowmobile"]` and one applicable to `["PowerBoat"]`
- **THEN** only the snowmobile-applicable template is returned

---

### Requirement: Update and delete household template
The system SHALL provide `PATCH /api/households/{householdId}/templates/{id}` and `DELETE /api/households/{householdId}/templates/{id}` (Contributor or Owner only) for a household's own templates. Deleting a template SHALL NOT delete any `MaintenanceItem` previously created from it (its `templateId` reference is simply retained as a historical pointer to a now-deleted template).

#### Scenario: Owner edits a household template's title
- **WHEN** an Owner PATCHes `{ title: "Fall haul-out" }` on a household template
- **THEN** HTTP 200 is returned with the updated title

#### Scenario: Deleting a template preserves items created from it
- **WHEN** a Contributor deletes a household template that a `Done` maintenance item's `templateId` still points to
- **THEN** HTTP 204 is returned and the maintenance item remains fully intact

---

### Requirement: Household template step CRUD and reorder
The system SHALL provide, scoped under a household template, `POST .../steps`, `PATCH .../steps/{id}`, and `DELETE .../steps/{id}` (Contributor or Owner only) accepting `{ text, engineScoped?, recurrenceIntervalMonths?, recurrenceIntervalMiles?, recurrenceIntervalHours?, suggestedParts? }`, and `PUT .../steps/reorder` accepting a full ordered array of the template's existing step ids, reassigning `sortOrder` to match array order.

#### Scenario: Adding an engine-scoped step with an interval
- **WHEN** a Contributor POSTs `{ text: "Change oil", engineScoped: true, recurrenceIntervalMonths: 12, recurrenceIntervalHours: 100 }` to a template's steps endpoint
- **THEN** HTTP 201 is returned with the new step carrying both interval values

#### Scenario: Reordering steps
- **WHEN** a Contributor PUTs a reordered array of a template's existing step ids
- **THEN** subsequent reads return the steps in the new order

---

### Requirement: Platform template catalog is readable by any authenticated user
The system SHALL provide `GET /api/templates/platform` (any authenticated user, regardless of household membership) returning all templates with `householdId = null`, with an optional `?assetCategory=` filter matching the same semantics as the household template list.

#### Scenario: Any authenticated user can browse platform templates
- **WHEN** an authenticated user who is not a PlatformAdmin calls `GET /api/templates/platform`
- **THEN** HTTP 200 is returned with the full platform template catalog

---

### Requirement: Platform template management is PlatformAdmin-only
The system SHALL provide `POST /api/admin/templates`, `PATCH /api/admin/templates/{id}`, and `DELETE /api/admin/templates/{id}` (PlatformAdmin only) for creating, updating, and deleting platform templates (`householdId = null`), and `POST /api/admin/templates/{id}/steps`, `PATCH /api/admin/templates/{id}/steps/{stepId}`, `DELETE /api/admin/templates/{id}/steps/{stepId}`, and `PUT /api/admin/templates/{id}/steps/reorder` (PlatformAdmin only) for managing their steps, with the same request/response shapes as the household template endpoints.

#### Scenario: PlatformAdmin creates a platform template
- **WHEN** a PlatformAdmin POSTs `{ title: "Oil change" }` to `/api/admin/templates`
- **THEN** HTTP 201 is returned with a template at `householdId: null`

#### Scenario: Non-admin cannot create a platform template
- **WHEN** a household Owner (not a PlatformAdmin) POSTs to `/api/admin/templates`
- **THEN** HTTP 403 is returned

#### Scenario: Non-admin cannot edit or delete a platform template
- **WHEN** a household Owner PATCHes or DELETEs a platform template via the admin endpoints
- **THEN** HTTP 403 is returned

---

### Requirement: Duplicate a platform template into a household
The system SHALL provide `POST /api/households/{householdId}/templates/duplicate` (Contributor or Owner only) accepting `{ platformTemplateId }`. The system SHALL create a new household-owned template (`householdId` set to the route household) with the same `title`, `description`, and `applicableCategories`, and a deep copy of all steps (including their `suggestedParts`), fully independent of the source platform template. Subsequent edits to either copy SHALL NOT affect the other.

#### Scenario: Duplicating creates an independent copy
- **WHEN** a Contributor POSTs `{ platformTemplateId: <id of the seeded "Oil change" template> }`
- **THEN** HTTP 201 is returned with a new household-owned template containing the same steps, and editing the household copy afterward does not change the original platform template

#### Scenario: platformTemplateId must reference a platform template
- **WHEN** the `platformTemplateId` in a duplicate request refers to a household-owned template (non-null `householdId`) rather than a platform template
- **THEN** HTTP 400 is returned

---

### Requirement: Seeded built-in platform templates
The system SHALL seed a small built-in library of platform templates (e.g. "Oil change", "Tire rotation", "Winterize engine") on startup or via migration, idempotently (re-running the seed SHALL NOT create duplicates).

#### Scenario: Seed runs on a fresh database
- **WHEN** the application starts against a database with no templates
- **THEN** the built-in platform templates exist afterward, each with `householdId = null`

#### Scenario: Seed is idempotent
- **WHEN** the application restarts against a database that already has the seeded platform templates
- **THEN** no duplicate templates are created

---

### Requirement: Creating a maintenance item from a template expands its steps and suggested parts
When `POST .../maintenance-items` is called with a `templateId` (see the `maintenance-items` capability), the system SHALL, within the same transaction as the item's creation: for each `TemplateStep` ordered by its `sortOrder`, create one `ChecklistItem` per active `Engine` on the target asset if `engineScoped` is true (using each such engine's `EngineId`), or one asset-level `ChecklistItem` (`EngineId = null`) if `engineScoped` is false; set each created `ChecklistItem`'s `TemplateStepId` to the source step; and for each entry in the step's `suggestedParts`, create a `PartLine` at `status: "Needed"` with that entry's `name` and `quantity`, linked to the created `ChecklistItem` via `checklistItemId`. If the template's `applicableCategories` is non-empty and does not include the target asset's `Category`, the request SHALL be rejected with HTTP 400.

#### Scenario: Engine-scoped step expands per active engine
- **WHEN** a `MaintenanceItem` is created with a `templateId` whose template has one `engineScoped: true` step, for a boat asset with two `Active` engines
- **THEN** the created item has two `ChecklistItem`s for that step, one per engine, each with `TemplateStepId` set to the source step

#### Scenario: Asset-level step creates a single checklist item
- **WHEN** the same template also has an `engineScoped: false` step ("Clean and cover bilge")
- **THEN** the created item has exactly one `ChecklistItem` for that step, with `EngineId = null`

#### Scenario: Suggested parts are copied, not linked
- **WHEN** a template step has `suggestedParts: [{ "name": "Oil filter", "quantity": 1 }]`
- **THEN** the created item has a `PartLine` named "Oil filter" at `status: "Needed"`, `quantity: 1`, linked to that step's checklist item, with no reference back to the template

#### Scenario: Retired engines are not expanded into
- **WHEN** an engine-scoped step is applied to an asset with one `Active` and one `Retired` engine
- **THEN** only one `ChecklistItem` is created, for the `Active` engine

#### Scenario: Category mismatch is rejected
- **WHEN** a `MaintenanceItem` create request specifies a `templateId` whose template's `applicableCategories` is `["PowerBoat", "Sailboat"]`, for an asset with `Category = Car`
- **THEN** HTTP 400 is returned and no maintenance item is created
