## MODIFIED Requirements

### Requirement: Authenticated layout with navigation and user menu
The frontend SHALL render a consistent authenticated layout (top navigation with primary nav links, household switcher, avatar-based user menu with display name/avatar/logout) wrapping all protected routes.

#### Scenario: Viewing any protected page
- **WHEN** a logged-in user is on any protected route
- **THEN** the layout shows the current household name, navigation, and a user menu offering logout

#### Scenario: Primary navigation links
- **WHEN** a logged-in user views the top navigation
- **THEN** it shows persistent links to the asset list and household settings routes for the current household, in addition to the brand mark and household switcher

#### Scenario: Active nav link indicator
- **WHEN** a logged-in user is on a route matching one of the primary nav links
- **THEN** that link is visually indicated as active (e.g. underline/accent border)
