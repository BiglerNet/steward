# frontend-dashboard-editing Specification

## Purpose
TBD - created by archiving change dashboard-widget-editing-enhancements. Update Purpose after archive.

## Requirements

### Requirement: Centered content in small stat widgets
Single-value stat widgets rendered via `StatWidget` (used for Asset Count, Cylinder Index, Total Displacement, Total Horsepower, Total Torque, and similar `Small`-sized widgets) SHALL center their label, value, and optional sub-label content horizontally and vertically within the widget card.

#### Scenario: Small stat widget renders centered content
- **WHEN** a `Small`-sized stat widget (e.g. Cylinder Index) is rendered on the dashboard
- **THEN** its label, value, and sub-label are horizontally and vertically centered within the card bounds

### Requirement: Dashboard edit mode toggle
The dashboard page SHALL provide an edit-mode toggle (replacing the prior "Edit Dashboard" dialog trigger) visible to users with `Edit` permission on the household (Owner or Contributor). Entering edit mode SHALL snapshot the current widget layout into local, unsaved state. Leaving edit mode without saving SHALL discard any staged changes and restore the last-saved layout.

#### Scenario: Contributor enters edit mode
- **WHEN** a Contributor clicks "Edit Dashboard" on a dashboard they can edit
- **THEN** the dashboard enters edit mode, showing drag handles and resize controls on each widget

#### Scenario: Viewer cannot enter edit mode
- **WHEN** a user with `Role = Viewer` views the dashboard
- **THEN** no edit-mode control is shown

#### Scenario: Canceling edit mode discards staged changes
- **WHEN** a user reorders widgets while in edit mode and then clicks "Cancel" (or exits edit mode without saving)
- **THEN** the dashboard reverts to the layout as last saved on the server, and no request is sent to the replace-layout endpoint

### Requirement: Drag-and-drop widget reordering
While in edit mode, the user SHALL be able to drag a widget to a new position among the dashboard's widgets, updating the staged (unsaved) layout order immediately. Widgets SHALL also be reorderable via keyboard (focus a widget's drag handle, then move it with arrow keys) as an accessible alternative to pointer-based dragging.

#### Scenario: Dragging a widget reorders the staged layout
- **WHEN** a user in edit mode drags a widget from one position to another
- **THEN** the staged widget order updates immediately to reflect the drop position, without a network request

#### Scenario: Keyboard reordering
- **WHEN** a user in edit mode focuses a widget's drag handle and presses an arrow key
- **THEN** the widget moves one position in the corresponding direction within the staged layout

### Requirement: Widget resize control
While in edit mode, each widget SHALL expose a resize control that cycles the widget's `widgetSize` through `Small` → `Wide` → `Full` → `Small` on activation, updating the staged (unsaved) layout immediately.

#### Scenario: Cycling a widget's size
- **WHEN** a user in edit mode activates the resize control on a `Small` widget
- **THEN** the widget's staged size becomes `Wide`; activating it again makes it `Full`; activating it again returns it to `Small`

### Requirement: Saving staged layout changes
While in edit mode, the user SHALL be able to save staged changes (reordering, resizing, and/or adding/removing widgets), which SHALL call the existing dashboard widget-layout replace endpoint with the full staged widget array and then exit edit mode on success.

#### Scenario: Saving a staged layout persists it
- **WHEN** a user in edit mode reorders and resizes widgets, then clicks "Save Layout"
- **THEN** a single request replaces the dashboard's widget layout with the staged array, edit mode exits, and the dashboard reflects the saved layout

#### Scenario: Save failure keeps edit mode active
- **WHEN** the save request fails
- **THEN** an error is shown, the dashboard remains in edit mode, and the staged (unsaved) changes are preserved

### Requirement: Adding and removing widgets in edit mode
While in edit mode, the user SHALL be able to add a widget from the available widget catalog to the staged layout, and remove any widget from the staged layout, without leaving edit mode.

#### Scenario: Adding a widget from the catalog
- **WHEN** a user in edit mode selects a widget type not already present in the layout from the widget catalog
- **THEN** the widget is appended to the staged layout with its default size

#### Scenario: Removing a widget
- **WHEN** a user in edit mode removes a widget from the staged layout
- **THEN** the widget no longer appears in the staged layout or the dashboard preview
