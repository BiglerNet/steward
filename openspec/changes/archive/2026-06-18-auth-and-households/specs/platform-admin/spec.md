## ADDED Requirements

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
