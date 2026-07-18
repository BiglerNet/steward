## ADDED Requirements

### Requirement: Household-wide kanban board
The frontend SHALL provide a household-scoped route (e.g. `/households/:householdId/maintenance`) showing a kanban board of `MaintenanceItem`s across every asset in the household, with columns `Planned` and `InProgress` plus a `Done` drop zone, and an asset filter. Each card SHALL show its title, asset name, and a "Blocked" badge when `isBlocked` is true. The board SHALL be visible to any Active household member; drag-and-drop status changes require Contributor or Owner.

#### Scenario: Viewing work across the fleet
- **WHEN** a member opens the household maintenance board with items on three different assets
- **THEN** cards for all three assets' `Planned`/`InProgress` items appear, grouped by status column

#### Scenario: Filtering to one asset
- **WHEN** a member selects one asset in the board's filter
- **THEN** only that asset's cards remain visible

#### Scenario: Done and Cancelled items are not shown as resting cards
- **WHEN** the board loads
- **THEN** no `Done` or `Cancelled` items appear as steady-state cards in any column

#### Scenario: Viewer cannot drag cards
- **WHEN** a Viewer-role user views the board
- **THEN** cards are not draggable

---

### Requirement: Drag-and-drop status changes
The board SHALL support dragging a card between the `Planned` and `InProgress` columns (updating `status` accordingly) and dragging a card onto the `Done` drop zone, using the same `@dnd-kit/core`/`@dnd-kit/sortable` pattern already used elsewhere in the app (`PointerSensor` + `KeyboardSensor`). Dragging a card onto `Done` SHALL trigger the identical open-checklist-items confirmation (Go back / Mark remaining as Skipped, then complete / Complete anyway) that the full-page editor's status control triggers — it SHALL NOT be possible to bypass that confirmation by using drag instead of the editor.

#### Scenario: Dragging between active columns
- **WHEN** a Contributor drags a card from `Planned` to `InProgress`
- **THEN** the item's `status` updates to `InProgress` and the card moves to that column

#### Scenario: Dragging to Done with open checklist items still prompts
- **WHEN** a Contributor drags a card with two `Open` checklist items onto the `Done` drop zone
- **THEN** the same three-option confirmation appears as it would from the full-page editor, before any status change is committed

#### Scenario: Card disappears from the board after completing
- **WHEN** a drag-to-Done is confirmed
- **THEN** the card is no longer present on the board after the next refetch

#### Scenario: Cancelling is a card action, not a column
- **WHEN** a Contributor wants to cancel a card
- **THEN** they use a per-card action (e.g. a context menu "Cancel" option) rather than dragging it to a column, since no `Cancelled` column exists on the board
