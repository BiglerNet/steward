# frontend-maintenance-items Specification

## Purpose
Defines the frontend UI for creating, viewing, and managing maintenance items (planning, checklists, and parts) on an asset, replacing the former service-records UI.

## Requirements
### Requirement: Maintenance list on the asset detail page
The frontend SHALL list an asset's maintenance items on a "Maintenance" tab of the asset detail page (replacing the former service-records tab), newest first by `date`, showing `title`, `status`, a derived "Blocked" badge when `isBlocked` is true, `date`/`cost` when present, and a "Completed" column showing the absolute `completedAt` date (or a placeholder when unset). Navigating from a row to the item's full-page editor SHALL pass the asset's name and this tab's path as navigation state, so the editor's breadcrumb and back action can reflect this asset without an extra fetch.

#### Scenario: Viewing maintenance items
- **WHEN** any household member opens an asset's Maintenance tab
- **THEN** the app lists that asset's maintenance items ordered by `date` descending, with status, blocked-badge, and completed date (when set) visible per row

#### Scenario: Completed column is blank for items not yet done
- **WHEN** a listed item has never had a `completedAt` value
- **THEN** its row's Completed column shows a placeholder ("—") rather than a date

#### Scenario: No entries yet
- **WHEN** an asset has zero maintenance items
- **THEN** the app shows an empty state prompting the first entry

---

### Requirement: Quick-create dialog
The frontend SHALL provide a small dialog (not a full-page form) for creating a new maintenance item, containing only `title` (required) and an optional template picker (see `frontend-maintenance-templates`). Submitting the dialog SHALL create the item immediately via the API and navigate to its full-page editor. The dialog SHALL be available to Contributors and Owners only.

#### Scenario: Quick-creating without a template
- **WHEN** a Contributor/Owner opens the quick-create dialog, enters a title, and submits without picking a template
- **THEN** a new `Planned` maintenance item is created and the app navigates to its full-page editor

#### Scenario: Quick-creating from a template
- **WHEN** a Contributor/Owner picks a template in the quick-create dialog before submitting
- **THEN** the created item's checklist and part lines are pre-populated per the chosen template, and the app navigates to its full-page editor already showing them

#### Scenario: Viewer cannot open the quick-create dialog
- **WHEN** a Viewer-role user views the asset's Maintenance tab
- **THEN** no "New" control is shown

---

### Requirement: Full-page maintenance item editor
The frontend SHALL provide a dedicated routed page (`/households/:householdId/assets/:assetId/maintenance/:itemId`), not a dialog, serving as both the creation continuation and the ongoing edit surface for a maintenance item across its lifecycle. The page SHALL autosave every field as it is edited (no explicit global "Save" action): text fields save on blur, the status selector and checklist/part-line interactions save immediately on change.

The page SHALL display a breadcrumb reflecting how the user arrived: when navigation state identifying the origin is present (e.g. from the household kanban board or the asset's Maintenance tab), the breadcrumb SHALL use that origin's label; when absent (a direct URL load or a page refresh), the breadcrumb SHALL fall back to the asset's name and its Maintenance tab, derived from the item's own data. The breadcrumb SHALL include a back affordance that navigates to the passed-through origin path when available, or to the asset's Maintenance tab otherwise. When the item has a `completedAt` value, the page SHALL display it as a read-only, absolute-date field.

#### Scenario: Editing the title autosaves
- **WHEN** a Contributor/Owner edits the title field on the full-page editor and moves focus away
- **THEN** the change is persisted without the user clicking a Save button

#### Scenario: Same page serves create-continuation and later edits
- **WHEN** a user returns to a maintenance item's URL weeks after creating it, after its status has progressed to `InProgress`
- **THEN** the same full-page editor loads showing its current state, not a different view

#### Scenario: Viewer sees a read-only editor
- **WHEN** a Viewer-role user opens a maintenance item's full-page editor
- **THEN** all fields, the checklist, and the parts list are rendered read-only with no autosave controls

#### Scenario: Breadcrumb reflects arriving from the kanban board
- **WHEN** a user clicks a card's title on the household kanban board to open an item
- **THEN** the editor's breadcrumb reads back to the board, and using it returns the user to the board with the filter they had selected still applied

#### Scenario: Breadcrumb reflects arriving from the asset's Maintenance tab
- **WHEN** a user clicks a row on an asset's Maintenance tab to open an item
- **THEN** the editor's breadcrumb reads back to that asset's Maintenance tab, and using it returns the user there

#### Scenario: Breadcrumb falls back on a direct link or refresh
- **WHEN** a user opens a maintenance item's URL directly (no navigation state available), such as via a bookmark or page refresh
- **THEN** the editor's breadcrumb shows the item's asset name and Maintenance tab, and using it navigates to that asset's Maintenance tab

#### Scenario: Completed date is shown once an item is done
- **WHEN** an item's status was set to `Done` and `completedAt` was recorded
- **THEN** the full-page editor displays that date as a read-only field

#### Scenario: No completed date field for items never completed
- **WHEN** an item has never been `Done` and has no `completedAt` value
- **THEN** the full-page editor does not display a completed-date field

---

### Requirement: Checklist UI
The full-page editor SHALL render each `ChecklistItem` with a checkbox that toggles between `Open` and `Done`, and a context menu offering "Mark skipped," "Reopen" (shown only when applicable to the item's current status), "Move up," and "Move down." Checklist items SHALL additionally support drag-and-drop reordering via a dedicated drag-handle icon, using the same `@dnd-kit/core`/`@dnd-kit/sortable` pattern (PointerSensor + KeyboardSensor) already used for dashboard widget reordering.

#### Scenario: Checking off an item via the checkbox
- **WHEN** a Contributor/Owner clicks the checkbox on an `Open` checklist item
- **THEN** the item's status becomes `Done` and the checkbox reflects it, with no other UI interaction required

#### Scenario: Skipping via the context menu
- **WHEN** a Contributor/Owner selects "Mark skipped" from a checklist item's context menu
- **THEN** the item's status becomes `Skipped`, visually distinct from `Done`

#### Scenario: Reordering via drag handle
- **WHEN** a Contributor/Owner drags a checklist item's grip handle to a new position in the list
- **THEN** the checklist's order updates and persists via the reorder endpoint

#### Scenario: Reordering via context menu fallback
- **WHEN** a Contributor/Owner selects "Move down" from a checklist item's context menu
- **THEN** that item swaps position with the next item and the new order persists

---

### Requirement: Adding and removing checklist items and part lines
The full-page editor SHALL provide controls to add a new ad hoc checklist item (plain text, no template association) and to add a new part line, both available to Contributors and Owners, alongside delete controls for each.

#### Scenario: Adding an ad hoc checklist item
- **WHEN** a Contributor/Owner types a new checklist item's text and confirms
- **THEN** it appears at the end of the checklist as an `Open` item with no template association

#### Scenario: Adding a part line
- **WHEN** a Contributor/Owner adds a part line with a name and quantity
- **THEN** it appears in the parts list at `status: "Needed"`

---

### Requirement: Parts list UI
The full-page editor SHALL render each `PartLine` with its name, quantity, and a status control offering `Needed`/`Ordered`/`Received`, plus optional fields for part number, vendor, tracking number, order URL, and cost.

#### Scenario: Advancing a part's status
- **WHEN** a Contributor/Owner changes a part line's status from `Needed` to `Ordered`
- **THEN** the change persists immediately and the item's "Blocked" badge (if this was its only unresolved part) remains shown until the part reaches `Received`

---

### Requirement: Done-transition confirmation for open checklist items
When a user sets a maintenance item's status to `Done` while one or more of its checklist items are still `Open`, the frontend SHALL present a confirmation with three options: "Go back" (cancels the status change), "Mark remaining as Skipped, then complete" (sets every `Open` checklist item to `Skipped`, then sets the item's status to `Done`), and "Complete anyway" (sets the item's status to `Done`, leaving `Open` checklist items unchanged). This confirmation SHALL be presented identically whether the `Done` transition is initiated via the status control on the full-page editor or by dragging a card to the "Done" column on the household kanban board (see `maintenance-recurrence-and-kanban`).

#### Scenario: Choosing to skip remaining items
- **WHEN** a user sets status to `Done` on an item with two `Open` checklist items and chooses "Mark remaining as Skipped, then complete"
- **THEN** both items become `Skipped` and the maintenance item's status becomes `Done`

#### Scenario: Choosing to complete anyway
- **WHEN** a user makes the same choice but selects "Complete anyway"
- **THEN** the two checklist items remain `Open` and the maintenance item's status becomes `Done`

#### Scenario: Going back cancels the transition
- **WHEN** a user selects "Go back"
- **THEN** the maintenance item's status is not changed and no checklist items are modified

#### Scenario: No prompt when nothing is open
- **WHEN** a user sets status to `Done` on an item whose checklist items are all `Done` or `Skipped` (or has no checklist)
- **THEN** the status changes immediately with no confirmation prompt
