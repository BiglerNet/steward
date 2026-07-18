## ADDED Requirements

### Requirement: Create maintenance item
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/maintenance-items` (Contributor or Owner only) accepting `{ title, description?, providerName?, status?, date?, cost?, odometerMiles?, engineHours?, engineId?, templateId? }`. `title` is required. `status` defaults to `Planned` when omitted; any valid status (including `Done`) may be supplied directly on create, so logging a completed entry remains a single request. If `engineId` is provided it SHALL belong to the same asset. If `templateId` is provided, the created item's checklist and part lines SHALL be populated per the `maintenance-templates` capability's template-application behavior. On success it SHALL return HTTP 201 with the created `MaintenanceItemResponse`, including its (possibly template-populated) checklist items and part lines.

#### Scenario: Quick-logging a completed entry in one request
- **WHEN** a Contributor POSTs `{ title: "Oil change", status: "Done", date: "2026-06-01", cost: 85.00 }` with no `templateId`
- **THEN** HTTP 201 is returned with a `MaintenanceItemResponse` at `status: "Done"` and no checklist items or part lines

#### Scenario: Creating a planned item defaults to Planned status
- **WHEN** a Contributor POSTs `{ title: "Engine rebuild" }` with no `status`
- **THEN** HTTP 201 is returned with `status: "Planned"`

#### Scenario: Viewer cannot create a maintenance item
- **WHEN** a user with `Role = Viewer` POSTs to the create endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Missing title rejected
- **WHEN** a create request omits `title`
- **THEN** HTTP 400 is returned

#### Scenario: engineId from a different asset rejected
- **WHEN** a create request's `engineId` belongs to an engine on a different asset
- **THEN** HTTP 400 is returned

---

### Requirement: List maintenance items for an asset
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/maintenance-items` (any Active member or PlatformAdmin) returning all maintenance items for the asset, ordered by `date` descending (items with a null `date` ordered by `createdAt` descending, appearing first), with optional `?status=` filter accepting one or more status values.

#### Scenario: Member lists maintenance history
- **WHEN** a user with any Active role calls the list endpoint for an asset with items in multiple statuses
- **THEN** HTTP 200 is returned with all of them, dated items ordered newest first

#### Scenario: Filtering by status
- **WHEN** a user calls the list endpoint with `?status=Planned&status=InProgress`
- **THEN** only items in those two statuses are returned

---

### Requirement: Get a single maintenance item
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/maintenance-items/{id}` (any Active member or PlatformAdmin) returning the full `MaintenanceItemResponse` including its ordered `checklistItems` and `partLines`.

#### Scenario: Full detail includes children
- **WHEN** a member requests a maintenance item that has three checklist items and two part lines
- **THEN** HTTP 200 is returned with all three checklist items (in `sortOrder`) and both part lines included

---

### Requirement: Update maintenance item fields
The system SHALL provide `PATCH /api/households/{householdId}/assets/{assetId}/maintenance-items/{id}` (Contributor or Owner only) accepting any subset of `{ title, description, providerName, status, date, cost, odometerMiles, engineHours, engineId }`. `templateId` is immutable after creation and is not accepted by this endpoint. There is no server-side validation requiring `ChecklistItem`s to be resolved before `status` may become `Done`. On success it SHALL return HTTP 200 with the updated `MaintenanceItemResponse`.

#### Scenario: Autosaving a single field
- **WHEN** a Contributor PATCHes `{ title: "Winterize the boat (both engines)" }`
- **THEN** HTTP 200 is returned with only `title` changed and every other field unchanged

#### Scenario: Completing an item with open checklist items is allowed
- **WHEN** a Contributor PATCHes `{ status: "Done" }` on an item whose checklist has two `Open` items
- **THEN** HTTP 200 is returned with `status: "Done"`, and the open checklist items remain `Open` and untouched

#### Scenario: templateId cannot be changed
- **WHEN** a PATCH request includes a `templateId` field
- **THEN** the field is ignored (or HTTP 400 is returned, at implementer's discretion) and the item's original `templateId` is unchanged

#### Scenario: Viewer cannot update
- **WHEN** a user with `Role = Viewer` PATCHes the endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Delete maintenance item
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/maintenance-items/{id}` (Contributor or Owner). On success the item and all its `ChecklistItem`s and `PartLine`s SHALL be permanently deleted and HTTP 204 returned.

#### Scenario: Deleting cascades to children
- **WHEN** a Contributor deletes a maintenance item that has checklist items and part lines
- **THEN** HTTP 204 is returned and none of its former checklist items or part lines remain queryable

---

### Requirement: Checklist item CRUD
The system SHALL provide, scoped under a maintenance item, `POST .../checklist-items` (Contributor or Owner) accepting `{ text, engineId? }` (defaults `status: "Open"`, appends to the end of the current order); `PATCH .../checklist-items/{id}` (Contributor or Owner) accepting any subset of `{ text, status, engineId }`, where setting `status` to `Done` or `Skipped` sets `resolvedAt` to the current time and setting it to `Open` clears `resolvedAt`; and `DELETE .../checklist-items/{id}` (Contributor or Owner). `engineId`, if provided, SHALL belong to the same asset as the parent maintenance item.

#### Scenario: Adding an ad hoc checklist item
- **WHEN** a Contributor POSTs `{ text: "Check trailer lights" }` to a maintenance item's checklist-items endpoint
- **THEN** HTTP 201 is returned with a new `Open` checklist item with no `templateStepId`

#### Scenario: Checking off an item sets resolvedAt
- **WHEN** a Contributor PATCHes `{ status: "Done" }` on an `Open` checklist item
- **THEN** HTTP 200 is returned with `status: "Done"` and `resolvedAt` set to the current time

#### Scenario: Reopening clears resolvedAt
- **WHEN** a Contributor PATCHes `{ status: "Open" }` on a `Done` or `Skipped` checklist item
- **THEN** HTTP 200 is returned with `status: "Open"` and `resolvedAt` set to null

#### Scenario: Marking an item skipped does not mark it done
- **WHEN** a Contributor PATCHes `{ status: "Skipped" }` on an `Open` checklist item
- **THEN** HTTP 200 is returned with `status: "Skipped"` (not `"Done"`) and `resolvedAt` set

---

### Requirement: Checklist item reorder
The system SHALL provide `PUT .../maintenance-items/{id}/checklist-items/reorder` (Contributor or Owner) accepting a full ordered array of the maintenance item's existing checklist item ids. The server SHALL atomically reassign `sortOrder` to match array order (index 0 = `sortOrder` 0) within a single transaction. Omitting an existing checklist item id from the array is rejected.

#### Scenario: Reordering via drag-and-drop
- **WHEN** a Contributor PUTs a reordered array of all three of a checklist's item ids
- **THEN** HTTP 200 is returned and subsequent reads return the checklist items in the new order

#### Scenario: Missing an existing item id is rejected
- **WHEN** a PUT to the reorder endpoint omits one of the maintenance item's existing checklist item ids
- **THEN** HTTP 400 is returned and no reordering occurs

---

### Requirement: Part line CRUD
The system SHALL provide, scoped under a maintenance item, `POST .../part-lines` (Contributor or Owner) accepting `{ name, partNumber?, vendor?, trackingNumber?, orderUrl?, quantity?, cost?, checklistItemId? }` (`quantity` defaults to 1, `status` defaults to `Needed`); `PATCH .../part-lines/{id}` (Contributor or Owner) accepting any subset of those fields plus `status`; and `DELETE .../part-lines/{id}` (Contributor or Owner). `checklistItemId`, if provided, SHALL belong to the same maintenance item.

#### Scenario: Adding a part line
- **WHEN** a Contributor POSTs `{ name: "Oil filter", quantity: 1 }` to a maintenance item's part-lines endpoint
- **THEN** HTTP 201 is returned with a new part line at `status: "Needed"`

#### Scenario: Marking a part received
- **WHEN** a Contributor PATCHes `{ status: "Received" }` on a part line at `status: "Ordered"`
- **THEN** HTTP 200 is returned with `status: "Received"`

#### Scenario: checklistItemId from a different maintenance item rejected
- **WHEN** a part-line create or update request's `checklistItemId` belongs to a checklist item on a different maintenance item
- **THEN** HTTP 400 is returned

---

### Requirement: Blocked status is derived, not stored
The system SHALL NOT expose a stored "Blocked" value for `MaintenanceItem.status`. The `MaintenanceItemResponse` SHALL include a computed `isBlocked` boolean, true when the item has at least one `PartLine` with `status` of `Needed` or `Ordered`, false otherwise.

#### Scenario: Item with an unreceived part is flagged blocked
- **WHEN** a `Planned` maintenance item has one part line at `status: "Ordered"`
- **THEN** its `MaintenanceItemResponse.isBlocked` is `true`

#### Scenario: Item with all parts received is not blocked
- **WHEN** a maintenance item's only part line is at `status: "Received"`
- **THEN** its `MaintenanceItemResponse.isBlocked` is `false`
