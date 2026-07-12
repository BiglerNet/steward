# frontend-auth Specification

## Purpose
Defines the frontend registration, login, and session UX.

## Requirements
### Requirement: Email/password registration
The frontend SHALL provide a registration form (email, password, password confirmation, display name) that calls `POST /api/auth/register`, stores the returned session, and navigates the user into the app on success.

The password field SHALL have a paired confirmation field that must match before submission is allowed. Both the password and confirmation fields SHALL support toggling between masked and plain-text display via a visibility toggle. As the user types a password, the form SHALL show a live, per-rule pass/fail indicator reflecting the enforced password policy (minimum 8 characters; at least one non-alphanumeric character), updating without requiring submit or blur.

#### Scenario: Successful registration
- **WHEN** a visitor submits valid email, password, matching password confirmation, and display name on `/register`
- **THEN** the app stores the returned JWT/user/pendingInvites and navigates to the default authenticated route

#### Scenario: Registration validation error
- **WHEN** the API returns a `400 ValidationProblem` (e.g. email already in use, weak password)
- **THEN** the form surfaces the field-level errors inline without losing the user's other entered values

#### Scenario: Password confirmation mismatch
- **WHEN** a visitor enters a password and a different value in the confirmation field
- **THEN** the form shows an inline error on the confirmation field and does not submit

#### Scenario: Toggling password visibility
- **WHEN** a visitor selects the show/hide toggle on the password or confirmation field
- **THEN** that field's input type switches between masked and plain text, without clearing the entered value

#### Scenario: Live password requirements feedback
- **WHEN** a visitor types into the password field
- **THEN** the form shows, in real time, whether the minimum-length and non-alphanumeric-character rules are currently satisfied, updating on every change

---

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

### Requirement: OAuth login and callback exchange
The frontend SHALL support starting an OAuth flow via redirect to `GET /api/auth/oauth/{provider}/login` and completing it on `/auth/callback` by exchanging the returned `code` via `POST /api/auth/oauth/exchange`.

The frontend SHALL query `GET /api/auth/oauth/providers` and render only providers reported as configured. When no providers are configured, the entire OAuth section — including the "or continue with" divider — SHALL be omitted from `/login` and `/register`. On both pages, the OAuth section (when rendered) SHALL appear above the email/password form. Each provider button SHALL show a distinct light/dark brand icon appropriate to the current color scheme. Clicking a provider button SHALL put that button into a pending/redirecting state and disable the other provider buttons until the browser navigates away.

#### Scenario: Initiating OAuth
- **WHEN** a visitor clicks "Continue with Google" (or Facebook/Apple) on `/login` or `/register`
- **THEN** the browser is redirected to the corresponding `GET /api/auth/oauth/{provider}/login` endpoint

#### Scenario: Completing OAuth
- **WHEN** the browser lands on `/auth/callback?code=...` after the provider redirect
- **THEN** the app exchanges the code via `POST /api/auth/oauth/exchange`, stores the returned session, and navigates into the app

#### Scenario: Failed OAuth exchange
- **WHEN** the code exchange fails (expired/invalid code)
- **THEN** the app redirects to `/login` with an error message, without storing a session

#### Scenario: Only configured providers are shown
- **WHEN** `GET /api/auth/oauth/providers` reports `google: true, facebook: false, apple: false`
- **THEN** `/login` and `/register` show only the Google button among the OAuth options

#### Scenario: No providers configured collapses the OAuth section
- **WHEN** `GET /api/auth/oauth/providers` reports all providers as `false`
- **THEN** `/login` and `/register` show neither any OAuth buttons nor the "or continue with" divider

#### Scenario: OAuth section renders above the password form
- **WHEN** at least one provider is configured
- **THEN** the OAuth buttons and divider render above the email/password fields on both `/login` and `/register`

#### Scenario: Clicking a provider shows a pending state
- **WHEN** a visitor clicks a configured provider's button
- **THEN** that button shows a loading/redirecting indicator and the remaining provider buttons become disabled until navigation occurs

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

---

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

---

### Requirement: Pending invite acceptance
The frontend SHALL surface any `pendingInvites` returned on login/registration and allow the user to accept one via `POST /api/auth/invites/{code}/accept`.

#### Scenario: User has pending invites
- **WHEN** a user logs in with one or more `pendingInvites`
- **THEN** the authenticated shell shows a banner indicating the count, linking to a list where each invite can be accepted

#### Scenario: Accepting an invite
- **WHEN** a user accepts a pending invite from the list
- **THEN** the app calls `POST /api/auth/invites/{code}/accept`, removes it from the pending list, and the newly joined household becomes available in the household switcher
