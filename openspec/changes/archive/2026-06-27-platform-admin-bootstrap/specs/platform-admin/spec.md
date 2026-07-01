## ADDED Requirements

### Requirement: PlatformAdmin role is auto-granted at registration when email matches config
The system SHALL read the configuration value `PlatformAdmin:Email` (string, default empty). When a new user is created — via either direct email/password registration or first-time OAuth sign-in — and their email matches `PlatformAdmin:Email` (case-insensitive comparison), the system SHALL immediately call `AddToRoleAsync` to assign the `PlatformAdmin` role before returning the auth response. The auto-grant SHALL only occur at the moment of account creation; subsequent logins by the same user are unaffected. When `PlatformAdmin:Email` is empty or not configured, no auto-grant occurs and registration behavior is unchanged.

#### Scenario: Direct registration with matching email grants PlatformAdmin role
- **WHEN** a user registers via `POST /api/auth/register` with an email that matches `PlatformAdmin:Email` (case-insensitive)
- **THEN** the returned JWT contains `PlatformAdmin` in its roles claim, and the user has the `PlatformAdmin` role in the Identity store

#### Scenario: OAuth first-time sign-in with matching email grants PlatformAdmin role
- **WHEN** a user signs in via OAuth for the first time with an email that matches `PlatformAdmin:Email`
- **THEN** after the OAuth code exchange, the returned JWT contains `PlatformAdmin` in its roles claim

#### Scenario: Returning OAuth user with matching email is unaffected
- **WHEN** a user who already has an account signs in via OAuth and their email matches `PlatformAdmin:Email`
- **THEN** no additional role assignment occurs (they already have the role from initial registration)

#### Scenario: Non-matching email registration is unaffected
- **WHEN** a user registers with an email that does NOT match `PlatformAdmin:Email`
- **THEN** the `PlatformAdmin` role is not assigned and the auth response is identical to before this change

#### Scenario: Empty config disables auto-grant
- **WHEN** `PlatformAdmin:Email` is not set or is an empty string
- **THEN** no user receives the `PlatformAdmin` role via auto-grant regardless of their email

#### Scenario: Auto-granted admin can immediately access admin endpoints
- **WHEN** a newly registered admin uses the JWT returned from registration
- **THEN** `GET /api/admin/users` returns HTTP 200 without requiring a separate login step
