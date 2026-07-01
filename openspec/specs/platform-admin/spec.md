### Requirement: PlatformAdmin role seeded on startup
The system SHALL ensure the `PlatformAdmin` Identity role exists in the database on every application startup. If the role does not exist it SHALL be created. This operation SHALL be idempotent.

#### Scenario: Role created on first startup
- **WHEN** the application starts against a fresh database with no roles
- **THEN** the `PlatformAdmin` role exists in `AspNetRoles` after startup completes

#### Scenario: Seed is idempotent
- **WHEN** the application starts and the `PlatformAdmin` role already exists
- **THEN** no duplicate is created and startup completes without error

---

### Requirement: PlatformAdmin user listing
The system SHALL provide `GET /api/admin/users` (PlatformAdmin only) returning a paginated list of all `ApplicationUser` records with `{ id, email, displayName, roles }`. SHALL support optional query parameter `?email=` for partial email search.

#### Scenario: Admin lists all users
- **WHEN** a PlatformAdmin calls `GET /api/admin/users`
- **THEN** HTTP 200 is returned with a paginated list of users

#### Scenario: Admin searches by email
- **WHEN** a PlatformAdmin calls `GET /api/admin/users?email=patrick`
- **THEN** only users whose email contains "patrick" (case-insensitive) are returned

#### Scenario: Non-admin is denied
- **WHEN** a user without PlatformAdmin role calls `GET /api/admin/users`
- **THEN** HTTP 403 is returned

---

### Requirement: PlatformAdmin role assignment
The system SHALL provide `POST /api/admin/users/{id}/roles` and `DELETE /api/admin/users/{id}/roles/{roleName}` (PlatformAdmin only) to add or remove Identity roles from a user. Assigning a non-existent role SHALL return HTTP 400. A PlatformAdmin SHALL NOT be able to remove their own PlatformAdmin role via this endpoint.

#### Scenario: Admin assigns PlatformAdmin to another user
- **WHEN** a PlatformAdmin calls `POST /api/admin/users/{id}/roles` with `{ role: "PlatformAdmin" }`
- **THEN** HTTP 200 is returned and the target user now has the PlatformAdmin role

#### Scenario: Admin cannot remove own PlatformAdmin role
- **WHEN** a PlatformAdmin calls `DELETE /api/admin/users/{ownId}/roles/PlatformAdmin`
- **THEN** HTTP 400 is returned

#### Scenario: Assigning unknown role returns 400
- **WHEN** a PlatformAdmin calls `POST /api/admin/users/{id}/roles` with a role name that doesn't exist
- **THEN** HTTP 400 is returned

---

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
