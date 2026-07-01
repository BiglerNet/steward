## ADDED Requirements

### Requirement: Email/password registration
The frontend SHALL provide a registration form (email, password, display name) that calls `POST /api/auth/register`, stores the returned session, and navigates the user into the app on success.

#### Scenario: Successful registration
- **WHEN** a visitor submits valid email, password, and display name on `/register`
- **THEN** the app stores the returned JWT/user/pendingInvites and navigates to the default authenticated route

#### Scenario: Registration validation error
- **WHEN** the API returns a `400 ValidationProblem` (e.g. email already in use, weak password)
- **THEN** the form surfaces the field-level errors inline without losing the user's other entered values

### Requirement: Email/password login
The frontend SHALL provide a login form that calls `POST /api/auth/login` and stores the returned session on success.

#### Scenario: Successful login
- **WHEN** a visitor submits a valid email/password on `/login`
- **THEN** the app stores the session and navigates to the default authenticated route (the last-selected household, or household creation if the user has none)

#### Scenario: Invalid credentials
- **WHEN** the API rejects the login attempt
- **THEN** the form shows an inline error and does not store any session

### Requirement: OAuth login and callback exchange
The frontend SHALL support starting an OAuth flow via redirect to `GET /api/auth/oauth/{provider}/login` and completing it on `/auth/callback` by exchanging the returned `code` via `POST /api/auth/oauth/exchange`.

#### Scenario: Initiating OAuth
- **WHEN** a visitor clicks "Continue with Google" (or Facebook/Apple) on `/login` or `/register`
- **THEN** the browser is redirected to the corresponding `GET /api/auth/oauth/{provider}/login` endpoint

#### Scenario: Completing OAuth
- **WHEN** the browser lands on `/auth/callback?code=...` after the provider redirect
- **THEN** the app exchanges the code via `POST /api/auth/oauth/exchange`, stores the returned session, and navigates into the app

#### Scenario: Failed OAuth exchange
- **WHEN** the code exchange fails (expired/invalid code)
- **THEN** the app redirects to `/login` with an error message, without storing a session

### Requirement: Session persistence across page refresh
The frontend SHALL persist the authenticated session (JWT, user, expiry) to `localStorage` and restore it on app load, without requiring re-login on every refresh.

#### Scenario: Refreshing while logged in
- **WHEN** a logged-in user refreshes the page
- **THEN** the app restores their session from `localStorage` and renders the authenticated shell without redirecting to `/login`

### Requirement: Automatic logout on token expiry or unauthorized response
The frontend SHALL clear the stored session and redirect to `/login` whenever any API request returns `401 Unauthorized`.

#### Scenario: Token expires mid-session
- **WHEN** an API request returns `401` (expired or invalid JWT)
- **THEN** the app clears the stored session and redirects to `/login`

### Requirement: Logout
The frontend SHALL provide a logout action that clears the stored session and returns the user to `/login`.

#### Scenario: User logs out
- **WHEN** a logged-in user selects "Log out" from the user menu
- **THEN** the stored session is cleared and the app navigates to `/login`

### Requirement: Pending invite acceptance
The frontend SHALL surface any `pendingInvites` returned on login/registration and allow the user to accept one via `POST /api/auth/invites/{code}/accept`.

#### Scenario: User has pending invites
- **WHEN** a user logs in with one or more `pendingInvites`
- **THEN** the authenticated shell shows a banner indicating the count, linking to a list where each invite can be accepted

#### Scenario: Accepting an invite
- **WHEN** a user accepts a pending invite from the list
- **THEN** the app calls `POST /api/auth/invites/{code}/accept`, removes it from the pending list, and the newly joined household becomes available in the household switcher
