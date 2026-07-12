# theme-preference Specification

## Purpose
Defines the Light/Dark/System theme preference model.

## Requirements
### Requirement: Three-state theme preference
The system SHALL support a `Light` / `Dark` / `System` theme preference per user, where `System` means the theme follows the OS `prefers-color-scheme` setting rather than a fixed choice.

#### Scenario: User selects an explicit theme
- **WHEN** an authenticated user selects "Dark" from the theme control
- **THEN** the app immediately switches to the dark theme and the choice persists across reloads and devices

#### Scenario: User returns to following the OS
- **WHEN** a user who previously selected "Dark" selects "System" instead
- **THEN** the app switches to match the current OS `prefers-color-scheme` value and continues to track it live if the OS setting changes thereafter

---

### Requirement: Pre-authentication and first-paint theme resolution
The frontend SHALL resolve the active theme, in order, from: (1) an authenticated user's stored server-side preference, (2) a `localStorage` value if no authenticated user is loaded yet, (3) the OS `prefers-color-scheme` media query if neither is present. A resolved `System` preference (from either source) SHALL always defer to the live OS media query value.

#### Scenario: Anonymous visitor on the login page
- **WHEN** a visitor with no session opens `/login` and their OS is set to dark mode, with no prior saved preference on this device
- **THEN** the login page renders in dark mode without a flash of the light theme first

#### Scenario: Returning device with a local override, no session
- **WHEN** a visitor previously set "Light" via the theme control on this device while logged out, and now opens `/login` with an OS dark-mode setting
- **THEN** the login page renders in light mode, honoring the local override over the OS setting

#### Scenario: Authenticated user's stored preference wins
- **WHEN** a user logs in on a new device that has never had a local theme choice
- **THEN** the app renders using their account's stored `themePreference`, not this device's OS setting or lack of local storage

---

### Requirement: Theme preference persisted server-side per user
The system SHALL persist an authenticated user's theme preference on their account so it is consistent across devices, updatable via a dedicated API endpoint.

#### Scenario: Changing theme while authenticated updates the account
- **WHEN** an authenticated user changes their theme selection
- **THEN** the app calls the theme-update endpoint with the new value, and the account's stored preference reflects the change on the next fetch of `/api/auth/me`

#### Scenario: Theme update is scoped to the calling user
- **WHEN** a user calls the theme-update endpoint
- **THEN** only that user's own `ApplicationUser` record is modified, regardless of any user identifier that might be supplied

#### Scenario: Unauthenticated request is rejected
- **WHEN** a request to the theme-update endpoint has no valid `Authorization` bearer token
- **THEN** the API returns HTTP 401 and no record is modified

---

### Requirement: Theme control in the user menu
The frontend SHALL provide the Light/Dark/System control inside the existing `UserMenu` dropdown, positioned above the "Log out" item.

#### Scenario: Opening the user menu shows theme options
- **WHEN** a logged-in user opens the `UserMenu` dropdown
- **THEN** it shows Light, Dark, and System options, with the currently active one indicated, above the "Log out" item
