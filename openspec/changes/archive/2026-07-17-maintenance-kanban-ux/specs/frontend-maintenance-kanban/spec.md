## MODIFIED Requirements

### Requirement: Household-wide kanban board
The frontend SHALL provide a household-scoped route (e.g. `/households/:householdId/maintenance`) showing a kanban board of `MaintenanceItem`s across every asset in the household, with columns `Planned` and `InProgress` plus a `Done` drop zone, and an asset filter persisted as a URL search param (e.g. `?asset=<assetId>`, absent/omitted meaning "all assets"). Each card SHALL show its title, asset name, and a "Blocked" badge when `isBlocked` is true. Cards in the `Done` drop zone SHALL additionally show a relative-time "Completed" label (e.g. "Completed 2 days ago") derived from `completedAt`. The board SHALL be visible to any Active household member; drag-and-drop status changes require Contributor or Owner. Navigating from a card's title to the item's full-page editor SHALL pass the board's current path (including the asset filter) as navigation state, so the editor's back action can return to the same filtered view.

#### Scenario: Viewing work across the fleet
- **WHEN** a member opens the household maintenance board with items on three different assets
- **THEN** cards for all three assets' `Planned`/`InProgress` items appear, grouped by status column

#### Scenario: Filtering to one asset
- **WHEN** a member selects one asset in the board's filter
- **THEN** only that asset's cards remain visible and the URL reflects the selected asset

#### Scenario: Filter survives a reload
- **WHEN** a member filters the board to one asset and reloads the page
- **THEN** the board still shows only that asset's cards, restored from the URL

#### Scenario: Done and Cancelled items are not shown as resting cards
- **WHEN** the board loads
- **THEN** no `Done` or `Cancelled` items appear as steady-state cards in any column

#### Scenario: Done-zone cards show when they were completed
- **WHEN** the board loads with an item completed two days ago in the `Done` drop zone
- **THEN** that card shows a relative label reading "Completed 2 days ago"

#### Scenario: Viewer cannot drag cards
- **WHEN** a Viewer-role user views the board
- **THEN** cards are not draggable

---

### Requirement: Drag-and-drop status changes
The board SHALL support dragging a card between the `Planned` and `InProgress` columns (updating `status` accordingly) and dragging a card onto the `Done` drop zone, using `@dnd-kit/core` with the entire card body as the draggable surface — not a single small handle. A grip icon remains on the card as a visual indicator only, with no distinct hit behavior of its own. Drag activation SHALL use a movement-distance constraint on pointer/mouse input (so a plain click on the title or the card's menu is never mistaken for a drag) and a press-and-hold constraint on touch input (so a tap or a scroll gesture is never mistaken for a drag). Dragging a card onto `Done` SHALL trigger the identical open-checklist-items confirmation (Go back / Mark remaining as Skipped, then complete / Complete anyway) that the full-page editor's status control triggers — it SHALL NOT be possible to bypass that confirmation by using drag instead of the editor.

#### Scenario: Dragging between active columns
- **WHEN** a Contributor drags a card from `Planned` to `InProgress` by grabbing anywhere on the card body
- **THEN** the item's `status` updates to `InProgress` and the card moves to that column

#### Scenario: A plain click still follows the title link
- **WHEN** a Contributor clicks the card's title without moving the pointer
- **THEN** the app navigates to the item's full-page editor instead of starting a drag

#### Scenario: A tap still opens the card's menu
- **WHEN** a Contributor taps the card's menu button on a touch device without holding
- **THEN** the menu opens instead of a drag starting

#### Scenario: Press-and-hold starts a drag on touch
- **WHEN** a Contributor presses and holds a card on a touch device past the activation delay without moving their finger, then drags
- **THEN** the card follows the drag and, on release over a valid target, its status updates accordingly

#### Scenario: Dragging to Done with open checklist items still prompts
- **WHEN** a Contributor drags a card with two `Open` checklist items onto the `Done` drop zone
- **THEN** the same three-option confirmation appears as it would from the full-page editor, before any status change is committed

#### Scenario: Card disappears from the board after completing
- **WHEN** a drag-to-Done is confirmed
- **THEN** the card is no longer present on the board after the next refetch

#### Scenario: Cancelling is a card action, not a column
- **WHEN** a Contributor wants to cancel a card
- **THEN** they use a per-card action (e.g. a context menu "Cancel" option) rather than dragging it to a column, since no `Cancelled` column exists on the board
