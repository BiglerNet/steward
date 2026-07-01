## ADDED Requirements

### Requirement: Create household
The system SHALL provide `POST /api/households` (requires authentication) accepting `{ name, publicSlug, isPublicVisible }`. On success it SHALL create the `Household` and an `Owner` membership for the calling user in a single transaction, returning HTTP 201 with the created household. `publicSlug` SHALL be validated as URL-safe (lowercase alphanumeric and hyphens only) and unique; duplicate slug SHALL return HTTP 409.

#### Scenario: Successful household creation
- **WHEN** an authenticated user POSTs to `/api/households` with a valid name and unique slug
- **THEN** HTTP 201 is returned with the new household and an Owner membership is created for the user

#### Scenario: Duplicate slug rejected
- **WHEN** a household is created with a `publicSlug` already in use
- **THEN** HTTP 409 is returned

#### Scenario: Invalid slug format rejected
- **WHEN** a household is created with a `publicSlug` containing uppercase letters, spaces, or special characters
- **THEN** HTTP 400 is returned with a validation error

---

### Requirement: List user's households
The system SHALL provide `GET /api/households` (requires authentication) returning all households where the calling user has an Active membership, including the user's role in each.

#### Scenario: User sees their households
- **WHEN** a user with Active memberships in two households calls `GET /api/households`
- **THEN** both households are returned with the user's role in each

#### Scenario: Pending and revoked memberships excluded
- **WHEN** a user has a Pending invite to one household and an Active membership in another
- **THEN** only the Active household is returned

---

### Requirement: Get household by ID
The system SHALL provide `GET /api/households/{id}` returning the household details. Only users with an Active membership in the household (or PlatformAdmin) SHALL receive HTTP 200; others SHALL receive HTTP 403.

#### Scenario: Member views household
- **WHEN** a user with any Active role calls `GET /api/households/{id}`
- **THEN** HTTP 200 is returned with household details

#### Scenario: Non-member is denied
- **WHEN** a user with no membership calls `GET /api/households/{id}`
- **THEN** HTTP 403 is returned (not 404, to avoid leaking existence)

---

### Requirement: Update household
The system SHALL provide `PUT /api/households/{id}` accepting `{ name, publicSlug, isPublicVisible }`. Only Owner role (or PlatformAdmin) SHALL be permitted. `publicSlug` changes SHALL enforce uniqueness.

#### Scenario: Owner updates household name
- **WHEN** an Owner calls `PUT /api/households/{id}` with a new name
- **THEN** HTTP 200 is returned with the updated household

#### Scenario: Contributor cannot update household
- **WHEN** a Contributor calls `PUT /api/households/{id}`
- **THEN** HTTP 403 is returned

---

### Requirement: Delete household
The system SHALL provide `DELETE /api/households/{id}`. Only the Owner (or PlatformAdmin) SHALL be permitted. If the household has any associated assets, the request SHALL be rejected with HTTP 409. On success the household and all its memberships SHALL be deleted and HTTP 204 returned.

#### Scenario: Owner deletes empty household
- **WHEN** an Owner calls `DELETE /api/households/{id}` and the household has no assets
- **THEN** HTTP 204 is returned and the household and its memberships no longer exist

#### Scenario: Delete blocked by existing assets
- **WHEN** an Owner calls `DELETE /api/households/{id}` and the household has one or more assets
- **THEN** HTTP 409 is returned with a message indicating assets must be removed first

#### Scenario: Non-owner cannot delete
- **WHEN** a Contributor or Viewer calls `DELETE /api/households/{id}`
- **THEN** HTTP 403 is returned
