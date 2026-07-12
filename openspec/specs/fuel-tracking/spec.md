# fuel-tracking Specification

## Purpose
Defines fuel log CRUD endpoints for tracking fuel and energy consumption.

## Requirements
### Requirement: Create fuel log entry
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/fuel-logs` (Contributor or Owner only) accepting `{ logType, date, quantity, unit, fuelGrade, pricePerUnit, totalCost, milesAtLog, hoursAtLog, engineId, notes }` with `logType` (one of `Fillup`, `Consumption`), `date`, `quantity`, and `unit` required. `unit` SHALL be one of `Gallons` | `Liters` | `Kwh`. If `engineId` is provided it SHALL belong to the same asset. On success it SHALL return HTTP 201 with the created `FuelLogResponse`.

#### Scenario: Contributor logs a fillup
- **WHEN** a Contributor POSTs `{ logType: "Fillup", date: "2026-06-01", quantity: 12.5, unit: "Gallons", totalCost: 48.75 }` to the fuel logs endpoint
- **THEN** HTTP 201 is returned with a `FuelLogResponse` matching the submitted fields

#### Scenario: Contributor logs an EV charging event
- **WHEN** a Contributor POSTs `{ logType: "Fillup", date: "2026-06-01", quantity: 62, unit: "Kwh", engineId: "<the asset's electric engine>", totalCost: 9.30 }` to the fuel logs endpoint
- **THEN** HTTP 201 is returned with a `FuelLogResponse` matching the submitted fields, including `unit: "Kwh"`

#### Scenario: Viewer cannot create a fuel log
- **WHEN** a user with `Role = Viewer` POSTs to the fuel logs endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Unknown logType rejected
- **WHEN** a create request has `logType: "Refund"`
- **THEN** HTTP 400 is returned

#### Scenario: Unknown unit rejected
- **WHEN** a create request has `unit: "Amps"`
- **THEN** HTTP 400 is returned

#### Scenario: engineId from a different asset rejected
- **WHEN** a create request's `engineId` belongs to an engine on a different asset
- **THEN** HTTP 400 is returned

---

### Requirement: List fuel logs for an asset
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/fuel-logs` (any Active member or PlatformAdmin) returning all fuel log entries for the asset, ordered by `date` descending, with optional `?from=&to=` filters.

#### Scenario: Member lists fuel history
- **WHEN** a user with any Active role calls the list endpoint for an asset with logged fuel entries
- **THEN** HTTP 200 is returned with all entries, ordered newest first

---

### Requirement: Update fuel log entry
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/fuel-logs/{fuelLogId}` (Contributor or Owner only) accepting the same fields as create. On success it SHALL return HTTP 200 with the updated `FuelLogResponse`.

#### Scenario: Contributor corrects total cost
- **WHEN** a Contributor PUTs the fuel log endpoint with a corrected `totalCost`
- **THEN** HTTP 200 is returned with the updated `FuelLogResponse`

#### Scenario: Contributor corrects a fillup's quantity and unit
- **WHEN** a Contributor PUTs the fuel log endpoint with `quantity: 55, unit: "Liters"` correcting an entry previously logged in Gallons
- **THEN** HTTP 200 is returned with the updated `FuelLogResponse` reflecting the new `quantity`/`unit`

---

### Requirement: Delete fuel log entry
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/fuel-logs/{fuelLogId}` (Contributor or Owner). On success the entry SHALL be permanently removed and HTTP 204 returned.

#### Scenario: Contributor deletes a duplicate entry
- **WHEN** a Contributor calls `DELETE` on the fuel log endpoint
- **THEN** HTTP 204 is returned and the entry no longer exists

#### Scenario: Viewer cannot delete a fuel log
- **WHEN** a user with `Role = Viewer` calls the delete endpoint
- **THEN** HTTP 403 is returned
