### Requirement: Create registration renewal record
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/registrations` (Contributor or Owner only) accepting `{ kind, registrationNumber, issuingAuthority, renewedOn, validFrom, cost, expiresOn, notes }`. `kind` SHALL be required and one of `Registration | TrailPass | Permit`; all other fields SHALL be optional (`registrationNumber` is no longer required — short-lived passes may not carry a meaningful number). Each record represents one registration/pass/permit period; multiple records per asset SHALL be allowed to accumulate as history. On success it SHALL return HTTP 201 with the created `RegistrationResponse` (including `kind`, `validFrom`, and `hasDocument: false`).

#### Scenario: Contributor registers a vehicle for the first time
- **WHEN** a Contributor POSTs `{ kind: "Registration", registrationNumber: "ABC-1234", issuingAuthority: "Wisconsin", renewedOn: "2026-01-15", cost: 120.00, expiresOn: "2027-01-15" }` to the registrations endpoint
- **THEN** HTTP 201 is returned with a `RegistrationResponse` matching the submitted fields and `hasDocument: false`

#### Scenario: Contributor logs a trail pass without a number
- **WHEN** a Contributor POSTs `{ kind: "TrailPass", validFrom: "2026-01-01", expiresOn: "2026-01-07", cost: 35.00 }` with no `registrationNumber`
- **THEN** HTTP 201 is returned with `kind: "TrailPass"` and a null `registrationNumber`

#### Scenario: Contributor logs a subsequent renewal without losing prior history
- **WHEN** a Contributor POSTs a new renewal record for an asset that already has one prior registration record
- **THEN** HTTP 201 is returned, and the asset now has two registration records, both retrievable via the list endpoint

#### Scenario: Missing kind rejected
- **WHEN** a create request omits `kind` or supplies an unknown value
- **THEN** HTTP 400 is returned

#### Scenario: Viewer cannot create a registration record
- **WHEN** a user with `Role = Viewer` POSTs to the registrations endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: List registration history for an asset
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/registrations` (any Active member or PlatformAdmin) returning all registration records for the asset, ordered by `expiresOn` descending with records lacking `expiresOn` last, then by `validFrom` descending with nulls last.

#### Scenario: Member views full registration history
- **WHEN** a user with any Active role calls the list endpoint for an asset with three registration records spanning three years
- **THEN** HTTP 200 is returned with all three records, ordered newest-expiry-first

#### Scenario: Records without expiry sort last
- **WHEN** an asset has records expiring `2027-01-15` and `2026-01-15` plus one record with no `expiresOn`
- **THEN** the list returns the `2027-01-15` record first and the undated record last

---

### Requirement: Update registration record
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/registrations/{registrationId}` (Contributor or Owner only) accepting the same fields as create (including `kind`, which SHALL remain required and editable), for correcting a specific historical entry. On success it SHALL return HTTP 200 with the updated `RegistrationResponse`. Updating record fields SHALL NOT affect any attached document.

#### Scenario: Contributor corrects a renewal's cost
- **WHEN** a Contributor PUTs the registration endpoint with a corrected `cost`
- **THEN** HTTP 200 is returned with the updated `RegistrationResponse` and `hasDocument` unchanged

#### Scenario: Contributor corrects a mis-kinded record
- **WHEN** a Contributor PUTs a record originally saved as `kind: "Registration"` with `kind: "TrailPass"`
- **THEN** HTTP 200 is returned and the record's `kind` is updated

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
