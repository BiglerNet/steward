## MODIFIED Requirements

### Requirement: Email/password login
The frontend SHALL provide a login form that calls `POST /api/auth/login` and stores the returned session on success. The form SHALL include a "Remember me" checkbox, checked by default, whose value is sent as `rememberMe` in the login request.

#### Scenario: Successful login
- **WHEN** a visitor submits a valid email/password on `/login`
- **THEN** the app stores the session (including the refresh token) and navigates to the default authenticated route (the last-selected household, or household creation if the user has none)

#### Scenario: Invalid credentials
- **WHEN** the API rejects the login attempt
- **THEN** the form shows an inline error and does not store any session

#### Scenario: Remember me defaults to checked
- **WHEN** a visitor loads `/login`
- **THEN** the "Remember me" checkbox is checked by default

#### Scenario: Unchecking remember me requests a shorter session
- **WHEN** a visitor unchecks "Remember me" before submitting valid credentials
- **THEN** the login request is sent with `rememberMe: false`

---

### Requirement: Session persistence across page refresh
The frontend SHALL persist the authenticated session (access token, refresh token, user, expiry) to `localStorage` and restore it on app load, without requiring re-login on every refresh. On restoring a session, the frontend SHALL schedule the proactive refresh timer (see "Proactive silent token refresh") against the restored `expiresAt`.

#### Scenario: Refreshing while logged in
- **WHEN** a logged-in user refreshes the page
- **THEN** the app restores their session, including the refresh token, from `localStorage` and renders the authenticated shell without redirecting to `/login`

---

### Requirement: Automatic logout on token expiry or unauthorized response
The frontend SHALL clear the stored session and redirect to `/login` when a `401 Unauthorized` response cannot be resolved by refreshing the session — specifically, when `POST /api/auth/refresh` itself fails (expired, revoked, or invalid refresh token). A `401` on an ordinary API request SHALL first attempt one silent refresh-and-retry before falling back to logout.

#### Scenario: Refresh token invalid or expired
- **WHEN** the proactive refresh timer fires, or a request-triggered refresh attempt occurs, and `POST /api/auth/refresh` returns `401`
- **THEN** the app clears the stored session and redirects to `/login`

#### Scenario: Ordinary request hits a stale access token
- **WHEN** an API request returns `401` because the access token expired before the proactive refresh timer fired
- **THEN** the app attempts a refresh, and on success retries the original request once with the new access token

---

### Requirement: Logout
The frontend SHALL provide a logout action that calls `POST /api/auth/logout` with the stored refresh token, then clears the stored session and returns the user to `/login` regardless of whether the backend call succeeds.

#### Scenario: User logs out
- **WHEN** a logged-in user selects "Log out" from the user menu
- **THEN** the app calls `POST /api/auth/logout`, clears the stored session, and navigates to `/login`

## ADDED Requirements

### Requirement: Proactive silent token refresh
The frontend SHALL schedule a timer, relative to the current session's `expiresAt`, that calls `POST /api/auth/refresh` before the access token expires, and updates the stored session with the resulting tokens and new `expiresAt` without interrupting the user. The timer SHALL be rescheduled whenever the session changes (login, refresh, or a cross-tab session update).

#### Scenario: Silent refresh before expiry
- **WHEN** the scheduled refresh timer fires
- **THEN** the app calls `POST /api/auth/refresh` with the stored refresh token and, on success, updates the stored session and reschedules the timer against the new `expiresAt`, with no visible interruption to the user

---

### Requirement: Cross-tab session synchronization
The frontend SHALL listen for the browser `storage` event and, when the session entry in `localStorage` changes, update its in-memory auth state to match — adopting a rotated session's tokens/`expiresAt` (and rescheduling its own refresh timer accordingly) or clearing its state and redirecting to `/login` if the session was removed.

#### Scenario: Another tab rotates the session
- **WHEN** one tab's proactive refresh rotates the stored session
- **THEN** other open tabs detect the `storage` event, adopt the new tokens into their in-memory state, and reschedule their own refresh timers instead of independently calling `/api/auth/refresh`

#### Scenario: Another tab logs out
- **WHEN** one tab's user logs out and the session is removed from `localStorage`
- **THEN** other open tabs detect the `storage` event, clear their in-memory auth state, and redirect to `/login`
