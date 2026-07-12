# warranty-tracking Specification

## Purpose
Defines warranty record CRUD endpoints.

## Requirements
### Requirement: Create warranty record
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/warranties` (Contributor or Owner only) accepting `{ provider, description, startsOn, expiresOn, notes }` with `provider` required. On success it SHALL return HTTP 201 with the created `WarrantyResponse` (including `hasDocument: false`).

#### Scenario: Contributor adds a manufacturer warranty
- **WHEN** a Contributor POSTs `{ provider: "Mercury Marine", expiresOn: "2028-01-01" }` to the warranties endpoint
- **THEN** HTTP 201 is returned with a `WarrantyResponse` having `hasDocument: false`

#### Scenario: Viewer cannot create a warranty
- **WHEN** a user with `Role = Viewer` POSTs to the warranties endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Missing provider rejected
- **WHEN** a create request omits `provider`
- **THEN** HTTP 400 is returned

---

### Requirement: List warranties for an asset
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/warranties` (any Active member or PlatformAdmin) returning all warranty records for the asset.

#### Scenario: Member lists warranties
- **WHEN** a user with any Active role calls the list endpoint for an asset with a warranty record
- **THEN** HTTP 200 is returned with the record included

---

### Requirement: Update warranty record
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/warranties/{warrantyId}` (Contributor or Owner only) accepting the same fields as create. On success it SHALL return HTTP 200 with the updated `WarrantyResponse`. Updating record fields SHALL NOT affect any attached document.

#### Scenario: Contributor updates warranty description
- **WHEN** a Contributor PUTs the warranty endpoint with a new `description`
- **THEN** HTTP 200 is returned with the updated `WarrantyResponse` and `hasDocument` unchanged

---

### Requirement: Delete warranty record
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/warranties/{warrantyId}` (Contributor or Owner). On success the record and any attached document SHALL be permanently removed and HTTP 204 returned.

#### Scenario: Contributor deletes a warranty
- **WHEN** a Contributor calls `DELETE` on the warranty endpoint
- **THEN** HTTP 204 is returned and the record and its document no longer exist

#### Scenario: Viewer cannot delete a warranty
- **WHEN** a user with `Role = Viewer` calls the delete endpoint
- **THEN** HTTP 403 is returned
