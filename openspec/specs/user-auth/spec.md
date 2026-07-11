### Requirement: Email/password registration
The system SHALL provide `POST /api/auth/register` accepting `{ email, password, displayName }`. On success it SHALL create an `ApplicationUser`, issue a JWT access token and a refresh token, and return HTTP 201 with an `AuthResponse` containing both tokens and basic user info. Duplicate email SHALL return HTTP 409. Registration SHALL always issue a refresh token with the "remembered" (long) expiry — there is no remember-me choice at registration time.

#### Scenario: Successful registration
- **WHEN** a POST to `/api/auth/register` is made with a valid email, password meeting complexity rules, and a display name
- **THEN** HTTP 201 is returned with `{ token, refreshToken, expiresAt, user: { id, email, displayName } }`

#### Scenario: Duplicate email rejected
- **WHEN** a POST to `/api/auth/register` is made with an email already in use
- **THEN** HTTP 409 is returned with a problem details body

#### Scenario: Weak password rejected
- **WHEN** a POST to `/api/auth/register` is made with a password that fails complexity rules (min 8 chars, at least one non-alphanumeric)
- **THEN** HTTP 400 is returned with validation error details

---

### Requirement: Email/password login
The system SHALL provide `POST /api/auth/login` accepting `{ email, password, rememberMe }`. On success it SHALL return HTTP 200 with an `AuthResponse` containing a JWT access token and a refresh token. Invalid credentials SHALL return HTTP 401 without specifying which field was wrong. `rememberMe` SHALL default to `true` when omitted and determines which refresh-token expiry (`Jwt:RefreshToken:RememberMeExpiry` vs `Jwt:RefreshToken:DefaultExpiry`) is applied to the issued refresh token.

#### Scenario: Successful login
- **WHEN** a POST to `/api/auth/login` is made with correct credentials
- **THEN** HTTP 200 is returned with a valid signed JWT and a refresh token in the response body

#### Scenario: Login with rememberMe true
- **WHEN** a POST to `/api/auth/login` is made with correct credentials and `rememberMe: true`
- **THEN** the issued refresh token's expiry is set from `Jwt:RefreshToken:RememberMeExpiry`

#### Scenario: Login with rememberMe false
- **WHEN** a POST to `/api/auth/login` is made with correct credentials and `rememberMe: false`
- **THEN** the issued refresh token's expiry is set from `Jwt:RefreshToken:DefaultExpiry`

#### Scenario: Wrong password
- **WHEN** a POST to `/api/auth/login` is made with a correct email but wrong password
- **THEN** HTTP 401 is returned; the response body SHALL NOT indicate whether the email or password was wrong

#### Scenario: Unknown email
- **WHEN** a POST to `/api/auth/login` is made with an email not in the system
- **THEN** HTTP 401 is returned (same response as wrong password — no user enumeration)

---

### Requirement: JWT access token structure
Issued JWT tokens SHALL be signed with HS256 using the configured signing key. Tokens SHALL contain claims: `sub` (user ID), `email`, `name` (DisplayName), `role` (all Identity roles for the user, re-derived from the database at issuance/refresh time). Token expiry SHALL be configurable via `Jwt:ExpiryMinutes` (default: 30).

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
The system SHALL provide `POST /api/auth/oauth/exchange` accepting `{ code }`. It SHALL look up the exchange code in IMemoryCache, invalidate it immediately (single-use), and return an `AuthResponse` with a JWT access token and a refresh token (issued with the "remembered" long expiry, matching registration). An expired or unknown code SHALL return HTTP 400.

#### Scenario: Valid exchange code returns JWT
- **WHEN** `POST /api/auth/oauth/exchange` is called with a valid, unexpired code
- **THEN** HTTP 200 is returned with an `AuthResponse` containing a JWT and refresh token, and the code is invalidated

#### Scenario: Reused exchange code is rejected
- **WHEN** the same exchange code is submitted a second time
- **THEN** HTTP 400 is returned

---

### Requirement: Refresh token issuance and storage
Every successful login, registration, and OAuth exchange SHALL issue a refresh token: a cryptographically random opaque value returned to the client once, with only its hash (not the raw value) persisted server-side in a `RefreshTokens` record. Each record SHALL track the owning user, an expiry timestamp, whether it was issued under "remember me," and revocation state.

#### Scenario: Refresh token is never stored in plaintext
- **WHEN** a refresh token is issued
- **THEN** the persisted `RefreshTokens` record contains only a hash of the token value, not the raw value

---

### Requirement: Token refresh endpoint
The system SHALL provide `POST /api/auth/refresh` accepting `{ refreshToken }`. On a valid, non-expired, non-revoked refresh token, it SHALL: re-derive the user's current roles from the database, issue a new JWT access token, rotate the refresh token (revoke the presented token, issue and return a new one carrying forward the original "remember me" expiry policy), and return HTTP 200 with an `AuthResponse`. An invalid, expired, or already-revoked (outside the reuse grace period) refresh token SHALL return HTTP 401.

#### Scenario: Valid refresh rotates tokens
- **WHEN** `POST /api/auth/refresh` is called with a valid, unexpired, unrevoked refresh token
- **THEN** HTTP 200 is returned with a new access token and a new refresh token, and the presented refresh token is marked revoked

#### Scenario: Refresh re-derives roles
- **WHEN** a user's household role or `PlatformAdmin` status changed since their access token was issued, and they call `POST /api/auth/refresh`
- **THEN** the newly issued access token's `role` claims reflect the current state in the database, not the claims from the token being refreshed

#### Scenario: Expired refresh token rejected
- **WHEN** `POST /api/auth/refresh` is called with a refresh token past its `ExpiresAt`
- **THEN** HTTP 401 is returned

#### Scenario: Reuse within the grace window is tolerated
- **WHEN** a refresh token that was already rotated less than `Jwt:RefreshToken:ReuseGracePeriod` ago is presented again
- **THEN** the system returns the same access/refresh token pair produced by the original rotation, rather than an error or a second rotation

#### Scenario: Reuse outside the grace window is treated as theft
- **WHEN** a refresh token that was rotated more than `Jwt:RefreshToken:ReuseGracePeriod` ago is presented again
- **THEN** HTTP 401 is returned and every non-expired refresh token belonging to that user is revoked

---

### Requirement: Session logout endpoint
The system SHALL provide `POST /api/auth/logout` accepting `{ refreshToken }` (requires authentication). It SHALL revoke the presented refresh token and its full rotation chain, so it can no longer be used to obtain new access tokens.

#### Scenario: Logout revokes the refresh token
- **WHEN** an authenticated user calls `POST /api/auth/logout` with their current refresh token
- **THEN** HTTP 200 is returned and subsequent `POST /api/auth/refresh` calls with that token return HTTP 401

#### Scenario: Access token issued before logout still expires naturally
- **WHEN** a user logs out and their previously issued access token has not yet reached its `exp`
- **THEN** that access token remains valid for API requests until it naturally expires (logout revokes the refresh token, not already-issued access tokens)

---

### Requirement: Current user profile endpoint
The system SHALL provide `GET /api/auth/me` (requires authentication) returning the current user's `{ id, email, displayName, avatarUrl }` derived from the database (not just JWT claims).

#### Scenario: Authenticated user gets their profile
- **WHEN** `GET /api/auth/me` is called with a valid JWT
- **THEN** HTTP 200 is returned with the user's current profile data

#### Scenario: Unauthenticated request rejected
- **WHEN** `GET /api/auth/me` is called without a Bearer token
- **THEN** HTTP 401 is returned
