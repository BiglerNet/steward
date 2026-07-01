### Requirement: Create engine hours log entry
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs` (Contributor or Owner only) accepting `{ date, hoursReading, tripHours, notes }` with `date` required and at least one of `hoursReading`/`tripHours` required. On success it SHALL return HTTP 201 with the created `EngineHoursLogResponse`.

#### Scenario: Contributor logs engine hours
- **WHEN** a Contributor POSTs `{ date: "2026-06-01", hoursReading: 340.5 }` to the hours logs endpoint for a boat engine
- **THEN** HTTP 201 is returned with an `EngineHoursLogResponse` matching the submitted fields

#### Scenario: Viewer cannot create an hours log
- **WHEN** a user with `Role = Viewer` POSTs to the hours logs endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Neither hoursReading nor tripHours provided
- **WHEN** a create request omits both `hoursReading` and `tripHours`
- **THEN** HTTP 400 is returned

#### Scenario: Engine belongs to a different asset
- **WHEN** `engineId` in the route does not belong to `assetId` in the route
- **THEN** HTTP 404 is returned

---

### Requirement: List engine hours logs
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs` (any Active member or PlatformAdmin) returning all hours log entries for the engine, ordered by `date` descending, with optional `?from=&to=` filters.

#### Scenario: Member lists hours history
- **WHEN** a user with any Active role calls the list endpoint for an engine with logged hours entries
- **THEN** HTTP 200 is returned with all entries, ordered newest first

---

### Requirement: Update engine hours log entry
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs/{hoursLogId}` (Contributor or Owner only) accepting the same fields as create. On success it SHALL return HTTP 200 with the updated `EngineHoursLogResponse`.

#### Scenario: Contributor corrects an hours reading
- **WHEN** a Contributor PUTs the hours log endpoint with a corrected `hoursReading`
- **THEN** HTTP 200 is returned with the updated `EngineHoursLogResponse`

---

### Requirement: Delete engine hours log entry
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs/{hoursLogId}` (Contributor or Owner). On success the entry SHALL be permanently removed and HTTP 204 returned.

#### Scenario: Contributor deletes a duplicate entry
- **WHEN** a Contributor calls `DELETE` on the hours log endpoint
- **THEN** HTTP 204 is returned and the entry no longer exists

#### Scenario: Viewer cannot delete an hours log
- **WHEN** a user with `Role = Viewer` calls the delete endpoint
- **THEN** HTTP 403 is returned
