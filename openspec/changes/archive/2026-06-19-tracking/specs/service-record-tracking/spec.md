## ADDED Requirements

### Requirement: Create service record
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/service-records` (Contributor or Owner only) accepting `{ date, description, providerName, cost, odometerMiles, engineHours, engineId, notes }` with `date` and `description` required. If `engineId` is provided it SHALL belong to the same asset. On success it SHALL return HTTP 201 with the created `ServiceRecordResponse`.

#### Scenario: Contributor logs an oil change
- **WHEN** a Contributor POSTs `{ date: "2026-06-01", description: "Oil change", cost: 85.00 }` to the service records endpoint
- **THEN** HTTP 201 is returned with a `ServiceRecordResponse` matching the submitted fields

#### Scenario: Viewer cannot create a service record
- **WHEN** a user with `Role = Viewer` POSTs to the service records endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Missing description rejected
- **WHEN** a create request omits `description`
- **THEN** HTTP 400 is returned

#### Scenario: engineId from a different asset rejected
- **WHEN** a create request's `engineId` belongs to an engine on a different asset
- **THEN** HTTP 400 is returned

---

### Requirement: List service records for an asset
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/service-records` (any Active member or PlatformAdmin) returning all service records for the asset, ordered by `date` descending, with optional `?from=&to=` `DateOnly` filters on `date`.

#### Scenario: Member lists service history
- **WHEN** a user with any Active role calls the list endpoint for an asset with three service records
- **THEN** HTTP 200 is returned with all three, ordered newest first

#### Scenario: Date range filter
- **WHEN** a user calls the list endpoint with `?from=2026-01-01&to=2026-03-31`
- **THEN** only records with `date` in that range are returned

---

### Requirement: Update service record
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/service-records/{serviceRecordId}` (Contributor or Owner only) accepting the same fields as create. On success it SHALL return HTTP 200 with the updated `ServiceRecordResponse`.

#### Scenario: Contributor corrects a cost entry
- **WHEN** a Contributor PUTs the service record endpoint with a corrected `cost`
- **THEN** HTTP 200 is returned with the updated `ServiceRecordResponse`

---

### Requirement: Delete service record
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/service-records/{serviceRecordId}` (Contributor or Owner). On success the record SHALL be permanently removed and HTTP 204 returned.

#### Scenario: Contributor deletes a mis-entered record
- **WHEN** a Contributor calls `DELETE` on the service record endpoint
- **THEN** HTTP 204 is returned and the record no longer exists

#### Scenario: Viewer cannot delete a service record
- **WHEN** a user with `Role = Viewer` calls the delete endpoint
- **THEN** HTTP 403 is returned
