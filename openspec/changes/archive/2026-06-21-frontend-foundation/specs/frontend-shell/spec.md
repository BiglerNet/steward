## ADDED Requirements

### Requirement: Protected routing
The frontend SHALL gate all routes other than `/login`, `/register`, and `/auth/callback` behind an authentication check, redirecting unauthenticated visitors to `/login`.

#### Scenario: Unauthenticated visitor hits a protected route
- **WHEN** a visitor with no stored session navigates to `/households/{id}/...` (or any other protected route)
- **THEN** the app redirects them to `/login`

#### Scenario: Authenticated visitor hits a public-only route
- **WHEN** an already-authenticated user navigates to `/login` or `/register`
- **THEN** the app redirects them into the authenticated app instead of re-showing the form

### Requirement: URL-based household scoping
The frontend SHALL scope authenticated routes under `/households/:householdId/...`, matching the backend's household-scoped resource routes.

#### Scenario: Deep link to a household-scoped route
- **WHEN** a logged-in user opens a link to `/households/{id}/...` directly
- **THEN** the app loads that household's context without requiring the switcher to be used first

#### Scenario: Refreshing a household-scoped route
- **WHEN** a logged-in user refreshes a page under `/households/{id}/...`
- **THEN** the app remains on the same household-scoped route after the session is restored

### Requirement: Household switcher
The frontend SHALL provide a household switcher in the authenticated shell listing the user's households (via `GET /api/households`) and navigating to the equivalent route under the newly selected household when changed.

#### Scenario: Switching households
- **WHEN** a user with multiple households selects a different one from the switcher
- **THEN** the app navigates to the same relative route under the newly selected household's ID and remembers the selection as the default for next login

#### Scenario: User belongs to no households
- **WHEN** a logged-in user has zero households
- **THEN** the app shows a "Create your first household" prompt instead of the switcher/content outlet

### Requirement: Authenticated layout with navigation and user menu
The frontend SHALL render a consistent authenticated layout (top navigation, household switcher, user menu with display name/avatar/logout) wrapping all protected routes.

#### Scenario: Viewing any protected page
- **WHEN** a logged-in user is on any protected route
- **THEN** the layout shows the current household name, navigation, and a user menu offering logout

### Requirement: Global API error handling
The frontend SHALL display a toast notification for unhandled API errors (e.g. `403 Forbidden`, `404 Not Found`, `5xx`) that aren't already handled inline by a form.

#### Scenario: Forbidden action
- **WHEN** an API call returns `403 Forbidden` (e.g. a Viewer attempting an action requiring Contributor/Owner)
- **THEN** the app shows a toast explaining the action isn't permitted, without crashing or navigating away

#### Scenario: Resource not found
- **WHEN** an API call returns `404 Not Found` (e.g. a stale link to a deleted resource)
- **THEN** the app shows a toast and the user remains on a sensible fallback view
