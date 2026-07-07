## MODIFIED Requirements

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
