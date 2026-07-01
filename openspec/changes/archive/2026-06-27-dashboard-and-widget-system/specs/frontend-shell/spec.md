## MODIFIED Requirements

### Requirement: Authenticated layout with navigation and user menu
The frontend SHALL render a consistent authenticated layout (top navigation with primary nav links, household switcher, avatar-based user menu with display name/avatar/logout) wrapping all protected routes. The primary navigation SHALL include a "Dashboard" link pointing to the household root route (`/households/:householdId`), in addition to "My Gear" and "Household Settings".

#### Scenario: Viewing any protected page
- **WHEN** a logged-in user is on any protected route
- **THEN** the layout shows the current household name, navigation, and a user menu offering logout

#### Scenario: Primary navigation links include Dashboard
- **WHEN** a logged-in user views the top navigation
- **THEN** it shows persistent links to Dashboard, the asset list, and household settings for the current household

#### Scenario: Active nav link indicator
- **WHEN** a logged-in user is on a route matching one of the primary nav links
- **THEN** that link is visually indicated as active (e.g. underline/accent border)

---

## ADDED Requirements

### Requirement: Dashboard page with widget grid
The frontend SHALL render a dashboard page at `/households/:householdId` that replaces the current stub `HouseholdOverviewPage`. The page SHALL fetch the household's dashboard list, display a selector (tab strip or dropdown) for switching between dashboards, fetch and render the selected dashboard's snapshot, and arrange widgets in a CSS grid respecting widget size: `Small` = 1/4 column width, `Wide` = 1/2 column width, `Full` = full row. An empty dashboard (no widgets) SHALL render an empty-state prompt.

#### Scenario: Member lands on the household root route
- **WHEN** a logged-in user navigates to `/households/{householdId}`
- **THEN** the dashboard page loads, shows the dashboard selector, and renders the widget grid with data from the snapshot

#### Scenario: Dashboard grid respects widget sizes
- **WHEN** the dashboard contains a mix of Small, Wide, and Full widgets
- **THEN** the grid renders Small widgets at 1/4 width, Wide at 1/2 width, and Full at full row width

#### Scenario: Empty dashboard state
- **WHEN** the active dashboard has no widgets
- **THEN** the page shows an empty-state prompt (e.g., "Add a widget to get started") with a call-to-action for Owner/Contributors

#### Scenario: Dashboard selector shows all household dashboards
- **WHEN** a household has three dashboards
- **THEN** the selector shows all three and the active one is indicated

---

### Requirement: Dashboard selector remembers last selection per household
The frontend SHALL persist the last-selected dashboard ID per household in `localStorage` under the key `dashboard:${householdId}`. On page load the selected dashboard SHALL be resolved in order: stored ID (if still valid) → the dashboard with `isDefault: true` → the first dashboard in the list.

#### Scenario: Returning user sees their last-selected dashboard
- **WHEN** a user selected "Fuel & Mileage" on a previous visit and returns to the household
- **THEN** the "Fuel & Mileage" dashboard is active (not the default)

#### Scenario: Stale stored ID falls back to default
- **WHEN** the stored dashboard ID no longer exists (e.g., was deleted)
- **THEN** the frontend falls back to the `isDefault` dashboard

---

### Requirement: Widget catalog for dashboard customization
The frontend SHALL provide a widget catalog UI accessible to Owner and Contributor roles for adding, removing, and reordering widgets. The catalog SHALL present all available widget types with their natural default size. Selecting a widget type adds it to the layout. Removing a widget removes it. Reordering is reflected in the persisted position. Changes are saved via `PUT .../widgets` with the full updated layout. Viewers do not see the catalog UI.

#### Scenario: Owner opens the widget catalog
- **WHEN** an Owner triggers the widget catalog (e.g., an "Edit Dashboard" button)
- **THEN** a UI appears listing all available widget types with options to add/remove/reorder

#### Scenario: Viewer does not see edit controls
- **WHEN** a user with `Role = Viewer` views the dashboard
- **THEN** no "Edit Dashboard" button or widget catalog controls are shown

#### Scenario: Adding a widget persists the layout
- **WHEN** an Owner adds "Total Horsepower" from the catalog and saves
- **THEN** the dashboard reloads and the TotalHorsepower widget is visible in the grid

---

### Requirement: Unit display conversion for engine spec fields
The frontend SHALL display engine spec values using contextual unit labels, converting stored SI values at display time. `TorqueNm` SHALL be displayed as ft-lbs (1 Nm = 0.7376 ft-lbs). `OilCapacityL` and `CoolantCapacityL` SHALL be displayed as US quarts (1 litre = 1.0567 qt). `HorsepowerHp` and `DisplacementCc` are displayed as-is. Engine forms SHALL accept input and display in the converted unit, then convert back to SI on submit.

#### Scenario: Engine detail shows torque in ft-lbs
- **WHEN** an engine has `torqueNm: 475` and the user views the engine detail
- **THEN** the UI displays "350 ft-lbs" (475 × 0.7376, rounded to a whole number)

#### Scenario: Engine form accepts oil capacity in quarts
- **WHEN** a Contributor enters "5 qt" in the oil capacity field and submits
- **THEN** the value sent to the API is `oilCapacityL: 4.73` (5 ÷ 1.0567, rounded to 2 decimal places)
