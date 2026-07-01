### Requirement: Create registration renewal record
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/registrations` (Contributor or Owner only) accepting `{ registrationNumber, issuingAuthority, renewedOn, cost, expiresOn, notes }` with `registrationNumber` required. Each record represents one renewal cycle; multiple records per asset SHALL be allowed to accumulate as renewal history. On success it SHALL return HTTP 201 with the created `RegistrationResponse` (including `hasDocument: false`).

#### Scenario: Contributor registers a vehicle for the first time
- **WHEN** a Contributor POSTs `{ registrationNumber: "ABC-1234", issuingAuthority: "DMV", renewedOn: "2026-01-15", cost: 120.00, expiresOn: "2027-01-15" }` to the registrations endpoint
- **THEN** HTTP 201 is returned with a `RegistrationResponse` matching the submitted fields and `hasDocument: false`

#### Scenario: Contributor logs a subsequent renewal without losing prior history
- **WHEN** a Contributor POSTs a new renewal record for an asset that already has one prior registration record
- **THEN** HTTP 201 is returned, and the asset now has two registration records, both retrievable via the list endpoint

#### Scenario: Viewer cannot create a registration record
- **WHEN** a user with `Role = Viewer` POSTs to the registrations endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Missing registrationNumber rejected
- **WHEN** a create request omits `registrationNumber`
- **THEN** HTTP 400 is returned

---

### Requirement: List registration history for an asset
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/registrations` (any Active member or PlatformAdmin) returning all registration records for the asset, ordered by `expiresOn` descending so the most recent renewal appears first.

#### Scenario: Member views full registration history
- **WHEN** a user with any Active role calls the list endpoint for an asset with three registration records spanning three years
- **THEN** HTTP 200 is returned with all three records, ordered newest-expiry-first

#### Scenario: Most recent renewal surfaces first
- **WHEN** an asset has registration records expiring `2026-01-15`, `2027-01-15`, and `2028-01-15`
- **THEN** the list response returns the `2028-01-15` record first

---

### Requirement: Update registration record
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/registrations/{registrationId}` (Contributor or Owner only) accepting the same fields as create, for correcting a specific historical entry. On success it SHALL return HTTP 200 with the updated `RegistrationResponse`. Updating record fields SHALL NOT affect any attached document.

#### Scenario: Contributor corrects a renewal's cost
- **WHEN** a Contributor PUTs the registration endpoint with a corrected `cost`
- **THEN** HTTP 200 is returned with the updated `RegistrationResponse` and `hasDocument` unchanged

#### Scenario: Viewer cannot update a registration record
- **WHEN** a user with `Role = Viewer` PUTs the update endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Delete registration record
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/registrations/{registrationId}` (Contributor or Owner). On success the record and any attached document SHALL be permanently removed and HTTP 204 returned. Deleting one historical record SHALL NOT affect any other registration record for the asset.

#### Scenario: Contributor deletes a mis-entered renewal record
- **WHEN** a Contributor calls `DELETE` on a registration record
- **THEN** HTTP 204 is returned, that record and its document no longer exist, and any other registration records for the asset are unaffected

#### Scenario: Viewer cannot delete a registration record
- **WHEN** a user with `Role = Viewer` calls the delete endpoint
- **THEN** HTTP 403 is returned
