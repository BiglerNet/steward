## MODIFIED Requirements

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
