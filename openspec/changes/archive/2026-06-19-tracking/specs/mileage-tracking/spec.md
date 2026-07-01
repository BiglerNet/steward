## ADDED Requirements

### Requirement: Create mileage log entry
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/mileage-logs` (Contributor or Owner only) accepting `{ date, odometerReading, tripMiles, notes }` with `date` required and at least one of `odometerReading`/`tripMiles` required. On success it SHALL return HTTP 201 with the created `MileageLogResponse`.

#### Scenario: Contributor logs an odometer reading
- **WHEN** a Contributor POSTs `{ date: "2026-06-01", odometerReading: 12450 }` to the mileage logs endpoint
- **THEN** HTTP 201 is returned with a `MileageLogResponse` matching the submitted fields

#### Scenario: Viewer cannot create a mileage log
- **WHEN** a user with `Role = Viewer` POSTs to the mileage logs endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Neither odometerReading nor tripMiles provided
- **WHEN** a create request omits both `odometerReading` and `tripMiles`
- **THEN** HTTP 400 is returned

---

### Requirement: List mileage logs for an asset
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/mileage-logs` (any Active member or PlatformAdmin) returning all mileage log entries for the asset, ordered by `date` descending, with optional `?from=&to=` filters.

#### Scenario: Member lists mileage history
- **WHEN** a user with any Active role calls the list endpoint for an asset with logged mileage entries
- **THEN** HTTP 200 is returned with all entries, ordered newest first

---

### Requirement: Update mileage log entry
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/mileage-logs/{mileageLogId}` (Contributor or Owner only) accepting the same fields as create. On success it SHALL return HTTP 200 with the updated `MileageLogResponse`.

#### Scenario: Contributor corrects an odometer typo
- **WHEN** a Contributor PUTs the mileage log endpoint with a corrected `odometerReading`
- **THEN** HTTP 200 is returned with the updated `MileageLogResponse`

---

### Requirement: Delete mileage log entry
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/mileage-logs/{mileageLogId}` (Contributor or Owner). On success the entry SHALL be permanently removed and HTTP 204 returned.

#### Scenario: Contributor deletes a duplicate entry
- **WHEN** a Contributor calls `DELETE` on the mileage log endpoint
- **THEN** HTTP 204 is returned and the entry no longer exists

#### Scenario: Viewer cannot delete a mileage log
- **WHEN** a user with `Role = Viewer` calls the delete endpoint
- **THEN** HTTP 403 is returned
