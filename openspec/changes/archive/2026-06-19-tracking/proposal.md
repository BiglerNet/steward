## Why

Assets and Engines can now be created, but a maintenance tracker with no way to log service, mileage, engine hours, or fuel is just an inventory list. This change exposes the four tracking-record entities (already modeled and EF-configured in `core-solution-structure`) behind household-scoped REST endpoints, so households can start recording the actual maintenance history the product exists to capture.

## What Changes

- Add CRUD endpoints for `ServiceRecord` (`/api/households/{householdId}/assets/{assetId}/service-records`), optionally tagging a specific `EngineId` on the asset.
- Add CRUD endpoints for `MileageLog` (`/api/households/{householdId}/assets/{assetId}/mileage-logs`), append-only odometer/trip readings.
- Add CRUD endpoints for `EngineHoursLog` (`/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs`), scoped under the engine since hours belong to a specific engine, not the asset.
- Add CRUD endpoints for `FuelLog` (`/api/households/{householdId}/assets/{assetId}/fuel-logs`), supporting both `Fillup` and `Consumption` log types, optionally tagging an `EngineId`.
- Enforce the existing household role capability matrix: Viewer can read, Contributor/Owner can create/edit/delete tracking records (no Owner-only restriction here — unlike asset/engine deletion, deleting a log entry is a routine correction, not a destructive household-level action).
- No new EF Core migration needed for the entities themselves — `core-solution-structure`'s migration already created these tables with `DeleteBehavior.Restrict` FKs to `Asset`/`Engine`, which already prevents deleting an asset or engine that has logged history (validated by this change's tests rather than new schema).

## Capabilities

### New Capabilities
- `service-record-tracking`: Household-scoped CRUD for service/maintenance history entries on an asset, optionally linked to a specific engine.
- `mileage-tracking`: Household-scoped CRUD for odometer/trip mileage log entries on an asset.
- `engine-hours-tracking`: Household-scoped CRUD for engine hours log entries, nested under a specific engine.
- `fuel-tracking`: Household-scoped CRUD for fuel fillup/consumption log entries on an asset, optionally linked to a specific engine.

### Modified Capabilities
- (none — `household-multitenancy`'s existing role matrix already covers "add/edit" for Contributor+Owner; this change additionally allows Contributor to delete tracking records, which is a new requirement nuance captured in each new capability's own spec rather than by editing `household-multitenancy`)

## Impact

- **Domain**: No new entities — `ServiceRecord`, `MileageLog`, `EngineHoursLog`, `FuelLog` already exist from `core-solution-structure`.
- **Application**: New `Steward.Application.Tracking` namespace (with `ServiceRecords`, `MileageLogs`, `EngineHoursLogs`, `FuelLogs` sub-namespaces) — DTOs, four `I*Service` interfaces, FluentValidation validators.
- **Infrastructure**: Four new service implementations; no migration changes (tables/FKs already exist).
- **Api**: New `ServiceRecordsController`, `MileageLogsController`, `EngineHoursLogsController` (nested under engines), `FuelLogsController` — all `[Authorize]` + resource-based household authorization via the existing `GetHouseholdIdForAssetAsync`/engine-equivalent lookup pattern from `EnginesController`.
- **Dependencies**: None new — reuses `HouseholdOperations`/`HouseholdResource` authorization.
