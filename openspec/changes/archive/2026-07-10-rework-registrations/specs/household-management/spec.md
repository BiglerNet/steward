## MODIFIED Requirements

### Requirement: Create household
The system SHALL provide `POST /api/households` (requires authentication) accepting `{ name, publicSlug, isPublicVisible, country, region }` where `country` and `region` are optional. On success it SHALL create the `Household` and an `Owner` membership for the calling user in a single transaction, returning HTTP 201 with the created household. `publicSlug` SHALL be validated as URL-safe (lowercase alphanumeric and hyphens only) and unique; duplicate slug SHALL return HTTP 409.

`country` and `region` SHALL be validated against the region registry: an unknown `country` code, an unknown `region` code, a `region` not belonging to the given `country`, or a `region` supplied without a `country` SHALL each return HTTP 400.

#### Scenario: Successful household creation
- **WHEN** an authenticated user POSTs to `/api/households` with a valid name and unique slug
- **THEN** HTTP 201 is returned with the new household and an Owner membership is created for the user

#### Scenario: Duplicate slug rejected
- **WHEN** a household is created with a `publicSlug` already in use
- **THEN** HTTP 409 is returned

#### Scenario: Invalid slug format rejected
- **WHEN** a household is created with a `publicSlug` containing uppercase letters, spaces, or special characters
- **THEN** HTTP 400 is returned with a validation error

#### Scenario: Household created with a location
- **WHEN** an authenticated user POSTs with `country: "US"` and `region: "US-WI"`
- **THEN** HTTP 201 is returned and the response includes both values

#### Scenario: Mismatched region rejected
- **WHEN** a create request has `country: "US"` and `region: "CA-ON"`, or a `region` without a `country`
- **THEN** HTTP 400 is returned with a validation error

---

### Requirement: Update household
The system SHALL provide `PUT /api/households/{id}` accepting `{ name, publicSlug, isPublicVisible, country, region }` with `country`/`region` optional and validated against the region registry as on create. Only Owner role (or PlatformAdmin) SHALL be permitted. `publicSlug` changes SHALL enforce uniqueness.

#### Scenario: Owner updates household name
- **WHEN** an Owner calls `PUT /api/households/{id}` with a new name
- **THEN** HTTP 200 is returned with the updated household

#### Scenario: Contributor cannot update household
- **WHEN** a Contributor calls `PUT /api/households/{id}`
- **THEN** HTTP 403 is returned

#### Scenario: Owner sets the household location
- **WHEN** an Owner calls `PUT /api/households/{id}` adding `country: "CA"` and `region: "CA-ON"`
- **THEN** HTTP 200 is returned and subsequent `GET /api/households/{id}` responses include the location

#### Scenario: Owner clears the household location
- **WHEN** an Owner calls `PUT /api/households/{id}` with `country: null` and `region: null`
- **THEN** HTTP 200 is returned and the household no longer has a location
