## 1. Application — Service Records

- [x] 1.1 [Application] Create `Steward.Application.Tracking.ServiceRecords` namespace with `ServiceRecordResponse`, `CreateServiceRecordRequest`, `UpdateServiceRecordRequest` records.
- [x] 1.2 [Application] Create `CreateServiceRecordRequestValidator`/`UpdateServiceRecordRequestValidator` — `date`/`description` required, non-negative `cost`/`odometerMiles`/`engineHours` when present.
- [x] 1.3 [Application] Define `IServiceRecordService` with `CreateAsync`, `ListAsync` (with `from`/`to` filter), `UpdateAsync`, `DeleteAsync` — all scoped by `assetId`; `CreateAsync`/`UpdateAsync` validate a provided `engineId` belongs to the same asset.

## 2. Application — Mileage Logs

- [x] 2.1 [Application] Create `Steward.Application.Tracking.MileageLogs` namespace with `MileageLogResponse`, `CreateMileageLogRequest`, `UpdateMileageLogRequest` records.
- [x] 2.2 [Application] Create validators — `date` required, at least one of `odometerReading`/`tripMiles` required and non-negative.
- [x] 2.3 [Application] Define `IMileageLogService` with `CreateAsync`, `ListAsync` (with `from`/`to` filter), `UpdateAsync`, `DeleteAsync` — scoped by `assetId`.

## 3. Application — Engine Hours Logs

- [x] 3.1 [Application] Create `Steward.Application.Tracking.EngineHoursLogs` namespace with `EngineHoursLogResponse`, `CreateEngineHoursLogRequest`, `UpdateEngineHoursLogRequest` records.
- [x] 3.2 [Application] Create validators — `date` required, at least one of `hoursReading`/`tripHours` required and non-negative.
- [x] 3.3 [Application] Define `IEngineHoursLogService` with `CreateAsync`, `ListAsync` (with `from`/`to` filter), `UpdateAsync`, `DeleteAsync` — scoped by `engineId`.

## 4. Application — Fuel Logs

- [x] 4.1 [Application] Create `Steward.Application.Tracking.FuelLogs` namespace with `FuelLogResponse`, `CreateFuelLogRequest`, `UpdateFuelLogRequest` records.
- [x] 4.2 [Application] Create validators — `logType` (known enum value), `date`, `volume`, `volumeUnit` required, non-negative numeric fields.
- [x] 4.3 [Application] Define `IFuelLogService` with `CreateAsync`, `ListAsync` (with `from`/`to` filter), `UpdateAsync`, `DeleteAsync` — scoped by `assetId`; `CreateAsync`/`UpdateAsync` validate a provided `engineId` belongs to the same asset.

## 5. Application — Authorization Lookup

- [x] 5.1 [Application] Add `GetHouseholdIdForEngineAsync(Guid engineId)` to `IEngineService` (joins `Engine` → `Asset` → `HouseholdId`), per design.md Decision 4.

## 6. Infrastructure — Service Implementations

- [x] 6.1 [Infrastructure] Implement `ServiceRecordService : IServiceRecordService`, `MileageLogService : IMileageLogService`, `EngineHoursLogService : IEngineHoursLogService`, `FuelLogService : IFuelLogService`, each ordering list results by `Date` descending and applying the `from`/`to` filter in the query.
- [x] 6.2 [Infrastructure] Implement `EngineService.GetHouseholdIdForEngineAsync`.
- [x] 6.3 [Infrastructure] Register the four new services in DI (extend the existing `AssetServiceExtensions`-style registration or add a sibling `TrackingServiceExtensions`).

## 7. Api — Controllers

- [x] 7.1 [Api] Create `ServiceRecordsController` at `api/households/{householdId}/assets/{assetId}/service-records`, mirroring `EnginesController`'s authorize-via-`GetHouseholdIdForAssetAsync` pattern; Create/Update require `HouseholdOperations.Edit`, List requires `HouseholdOperations.View`, Delete requires `HouseholdOperations.Edit` (per design.md Decision 3 — not `Delete`, since Contributor may delete tracking records).
- [x] 7.2 [Api] Create `MileageLogsController` at `api/households/{householdId}/assets/{assetId}/mileage-logs` following the same pattern.
- [x] 7.3 [Api] Create `FuelLogsController` at `api/households/{householdId}/assets/{assetId}/fuel-logs` following the same pattern.
- [x] 7.4 [Api] Create `EngineHoursLogsController` at `api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs`, authorizing via `GetHouseholdIdForEngineAsync` and returning 404 if the engine doesn't belong to `assetId`/`householdId`.
- [x] 7.5 [Api] Add `?from=&to=` query parameter binding to all four List actions.

## 8. Tests

- [x] 8.1 [IntegrationTests] CRUD happy-path tests for each of the four tracking record types, verifying role enforcement (Viewer 403 on write, Contributor CAN delete unlike assets/engines).
- [x] 8.2 [IntegrationTests] Cross-household isolation test — a tracking record under Household A's asset returns 404 when queried via Household B's route.
- [x] 8.3 [IntegrationTests] `from`/`to` date filter tests for each list endpoint.
- [x] 8.4 [IntegrationTests] Engine-mismatch tests — `engineId` from a different asset rejected on `ServiceRecord`/`FuelLog` create/update; `engineId` in route not belonging to `assetId` returns 404 on hours-log endpoints.
- [x] 8.5 [UnitTests] Validator tests for all four create/update validators (required fields, non-negative numeric constraints, unknown `logType` rejection).

