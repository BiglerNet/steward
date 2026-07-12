# identity-and-auth Specification

## Purpose
Defines ASP.NET Core Identity setup and the extended application user.

## Requirements
### Requirement: ASP.NET Core Identity with extended user
The system SHALL use ASP.NET Core Identity for user management. The `ApplicationUser` class SHALL extend `IdentityUser` and add: `DisplayName` (string, nullable), `AvatarUrl` (string, nullable), and `ThemePreference` (nullable, one of `Light`/`Dark`/`System`; `null` means no explicit preference has been set and the client SHALL fall back to the OS `prefers-color-scheme` setting). Identity tables SHALL be created by the initial EF Core migration; `ThemePreference` SHALL be added by a subsequent migration with a `NULL` default for existing rows.

#### Scenario: User record created via Identity
- **WHEN** a new user registers via email/password or completes an OAuth flow
- **THEN** an `ApplicationUsers` row is created with a valid `Id` (Guid), `Email`, `NormalizedEmail`, and a `NULL` `ThemePreference`

#### Scenario: Existing users unaffected by the new column
- **WHEN** the `ThemePreference` migration runs against a database with existing `ApplicationUsers` rows
- **THEN** every existing row gets a `NULL` `ThemePreference` and no other column is affected

---

### Requirement: JWT Bearer authentication
The system SHALL issue signed JWT access tokens upon successful authentication, paired with a server-tracked refresh token. All protected API endpoints SHALL require a valid `Authorization: Bearer <token>` header. Unauthenticated requests SHALL receive HTTP 401.

Token claims SHALL include: `sub` (user ID), `email`, `name` (DisplayName), and `role` (ASP.NET Identity roles).

Access tokens SHALL have a configurable expiry defaulting to 30 minutes (`Jwt:ExpiryMinutes`).

#### Scenario: Valid token grants access
- **WHEN** a request is sent to a protected endpoint with a valid, non-expired JWT
- **THEN** the endpoint returns HTTP 200 (or appropriate success code)

#### Scenario: Missing token is rejected
- **WHEN** a request is sent to a protected endpoint with no `Authorization` header
- **THEN** the API returns HTTP 401 with no response body leaking internal details

#### Scenario: Expired token is rejected
- **WHEN** a request is sent with a JWT whose `exp` claim is in the past
- **THEN** the API returns HTTP 401

---

### Requirement: OAuth social login providers
The system SHALL support OAuth 2.0 login via Google, Facebook, and Apple. Upon successful OAuth callback, the system SHALL either link to an existing `ApplicationUser` (matched by email) or create a new user, then issue a JWT access token.

Provider client ID and secret SHALL be loaded from environment variables / configuration (not hardcoded).

#### Scenario: Google OAuth creates new user
- **WHEN** a user completes Google OAuth for the first time with an email not in the system
- **THEN** a new `ApplicationUser` is created, linked to the Google external login, and a JWT is returned

#### Scenario: Google OAuth links returning user
- **WHEN** a user completes Google OAuth with an email already associated with an account
- **THEN** no duplicate user is created and a JWT is returned for the existing account

#### Scenario: Missing OAuth configuration fails gracefully
- **WHEN** the application starts without Google/Facebook/Apple client ID configured
- **THEN** the startup either logs a warning and skips the provider, or throws a clear configuration error (not a null reference exception at runtime)

---

### Requirement: OAuth provider configuration discovery
The system SHALL expose an unauthenticated `GET /api/auth/oauth/providers` endpoint that reports, for each supported provider (`google`, `facebook`, `apple`), whether that provider's client configuration is populated (non-empty client ID). The response SHALL contain only boolean flags and MUST NOT leak client IDs, secrets, or any other configuration values.

#### Scenario: Provider fully configured
- **WHEN** `Auth:Google:ClientId` is a non-empty value in configuration
- **THEN** `GET /api/auth/oauth/providers` reports `google: true`

#### Scenario: Provider not configured
- **WHEN** `Auth:Facebook:ClientId` is empty or missing from configuration
- **THEN** `GET /api/auth/oauth/providers` reports `facebook: false`

#### Scenario: No provider secrets are exposed
- **WHEN** any client calls `GET /api/auth/oauth/providers`
- **THEN** the response body contains only boolean fields per provider, with no client ID, secret, key ID, or team ID values present

---

### Requirement: PlatformAdmin role
The system SHALL have a built-in ASP.NET Core Identity role named `PlatformAdmin`. Users in this role SHALL be able to access all platform administration endpoints regardless of household membership. The role SHALL be seeded into the `AspNetRoles` table by the initial migration or startup seed.

#### Scenario: PlatformAdmin accesses admin endpoint
- **WHEN** a user with the `PlatformAdmin` role sends a request to an `[Authorize(Roles = "PlatformAdmin")]` endpoint
- **THEN** the request succeeds with HTTP 200

#### Scenario: Non-admin is denied admin endpoint
- **WHEN** a user without the `PlatformAdmin` role sends a request to a PlatformAdmin-only endpoint
- **THEN** the API returns HTTP 403
