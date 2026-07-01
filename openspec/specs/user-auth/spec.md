### Requirement: Email/password registration
The system SHALL provide `POST /api/auth/register` accepting `{ email, password, displayName }`. On success it SHALL create an `ApplicationUser`, issue a JWT access token, and return HTTP 201 with an `AuthResponse` containing the token and basic user info. Duplicate email SHALL return HTTP 409.

#### Scenario: Successful registration
- **WHEN** a POST to `/api/auth/register` is made with a valid email, password meeting complexity rules, and a display name
- **THEN** HTTP 201 is returned with `{ token, expiresAt, user: { id, email, displayName } }`

#### Scenario: Duplicate email rejected
- **WHEN** a POST to `/api/auth/register` is made with an email already in use
- **THEN** HTTP 409 is returned with a problem details body

#### Scenario: Weak password rejected
- **WHEN** a POST to `/api/auth/register` is made with a password that fails complexity rules (min 8 chars, at least one non-alphanumeric)
- **THEN** HTTP 400 is returned with validation error details

---

### Requirement: Email/password login
The system SHALL provide `POST /api/auth/login` accepting `{ email, password }`. On success it SHALL return HTTP 200 with an `AuthResponse`. Invalid credentials SHALL return HTTP 401 without specifying which field was wrong.

#### Scenario: Successful login
- **WHEN** a POST to `/api/auth/login` is made with correct credentials
- **THEN** HTTP 200 is returned with a valid signed JWT in the response body

#### Scenario: Wrong password
- **WHEN** a POST to `/api/auth/login` is made with a correct email but wrong password
- **THEN** HTTP 401 is returned; the response body SHALL NOT indicate whether the email or password was wrong

#### Scenario: Unknown email
- **WHEN** a POST to `/api/auth/login` is made with an email not in the system
- **THEN** HTTP 401 is returned (same response as wrong password — no user enumeration)

---

### Requirement: JWT access token structure
Issued JWT tokens SHALL be signed with HS256 using the configured signing key. Tokens SHALL contain claims: `sub` (user ID), `email`, `name` (DisplayName), `role` (all Identity roles for the user). Token expiry SHALL be configurable via `Jwt:ExpiryMinutes` (default: 15).

#### Scenario: Token contains expected claims
- **WHEN** a user logs in and the returned JWT is decoded
- **THEN** it contains `sub`, `email`, `name`, and `exp` claims, and the signature validates against the configured key

#### Scenario: Expired token rejected
- **WHEN** a request is made with a JWT whose `exp` is in the past
- **THEN** HTTP 401 is returned

---

### Requirement: OAuth social login initiation
The system SHALL provide `GET /api/auth/oauth/{provider}/login` (where `provider` is `google`, `facebook`, or `apple`) that redirects the browser to the provider's authorization endpoint with the correct client ID, redirect URI, and scopes.

#### Scenario: Google login redirects to Google
- **WHEN** a browser navigates to `GET /api/auth/oauth/google/login`
- **THEN** the response is a 302 redirect to `accounts.google.com/o/oauth2/v2/auth` with `client_id`, `redirect_uri`, and `scope=openid email profile` in the query string

#### Scenario: Unknown provider returns 400
- **WHEN** `GET /api/auth/oauth/unknown/login` is requested
- **THEN** HTTP 400 is returned

---

### Requirement: OAuth callback and exchange code
After the provider redirects back, the system SHALL handle `GET /api/auth/oauth/{provider}/callback`, validate the authorization code with the provider, find or create the corresponding `ApplicationUser`, generate a single-use exchange code (random Guid, 60-second TTL stored in IMemoryCache), and redirect to the frontend at `{FrontendBaseUrl}/auth/callback?code={exchangeCode}`.

#### Scenario: New OAuth user is created
- **WHEN** a Google OAuth callback arrives for an email not yet in the system
- **THEN** a new `ApplicationUser` is created, an external login record is linked, and the browser is redirected to the frontend callback URL with an exchange code

#### Scenario: Returning OAuth user is matched
- **WHEN** a Google OAuth callback arrives for an email already linked to an existing user
- **THEN** no duplicate user is created and the browser is redirected with a fresh exchange code for the existing user

---

### Requirement: OAuth exchange code redemption
The system SHALL provide `POST /api/auth/oauth/exchange` accepting `{ code }`. It SHALL look up the exchange code in IMemoryCache, invalidate it immediately (single-use), and return an `AuthResponse` with a JWT. An expired or unknown code SHALL return HTTP 400.

#### Scenario: Valid exchange code returns JWT
- **WHEN** `POST /api/auth/oauth/exchange` is called with a valid, unexpired code
- **THEN** HTTP 200 is returned with an `AuthResponse` and the code is invalidated

#### Scenario: Reused exchange code is rejected
- **WHEN** the same exchange code is submitted a second time
- **THEN** HTTP 400 is returned

---

### Requirement: Current user profile endpoint
The system SHALL provide `GET /api/auth/me` (requires authentication) returning the current user's `{ id, email, displayName, avatarUrl }` derived from the database (not just JWT claims).

#### Scenario: Authenticated user gets their profile
- **WHEN** `GET /api/auth/me` is called with a valid JWT
- **THEN** HTTP 200 is returned with the user's current profile data

#### Scenario: Unauthenticated request rejected
- **WHEN** `GET /api/auth/me` is called without a Bearer token
- **THEN** HTTP 401 is returned
