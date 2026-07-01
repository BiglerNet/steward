## Context

`ServiceRecord`, `MileageLog`, `EngineHoursLog`, and `FuelLog` were modeled and EF-configured in `core-solution-structure`, including the `DeleteBehavior.Restrict` foreign keys to `Asset`/`Engine` that `asset-management`'s design doc anticipated. None of the four are exercised by any service or controller yet. None of these entities carry a `HouseholdId` — they hang off `AssetId` (and sometimes `EngineId`), so authorization always requires resolving up to the owning household, exactly as `EnginesController` already does via `IAssetService.GetHouseholdIdForAssetAsync`.

Three of the four (`ServiceRecord`, `MileageLog`, `FuelLog`) are asset-scoped; `EngineHoursLog` is engine-scoped, since hours belong to one physical engine, not the asset as a whole (an asset can have two engines on different hour counts).

## Goals / Non-Goals

**Goals:**
- CRUD for all four tracking-record types through four small, independent endpoint sets (not one polymorphic endpoint — these are four genuinely different shapes, unlike the 10 asset types which shared a TPH base).
- Reuse the existing household role enforcement and the `GetHouseholdIdForAssetAsync`-style authorization lookup pattern.
- Allow Contributor (not just Owner) to delete tracking records, since correcting/removing a log entry is routine data entry, not a destructive household-level action like deleting an asset.
- No schema changes — tables and FK constraints already exist.

**Non-Goals:**
- Registration and Warranty (document-attachment entities) — separate change, blocked on the still-open file storage provider question.
- Computed/aggregate views (e.g., "fuel economy over time", "cost per mile") — pure CRUD only in this change; aggregation is a future reporting change.
- Automatic engine-hours/mileage syncing from `ServiceRecord.OdometerMiles`/`EngineHours` snapshots into `MileageLog`/`EngineHoursLog` — these stay independent, manually-entered logs for now; reconciling them is a future concern.

## Decisions

### 1. Four independent endpoint sets, not one generic "tracking record" endpoint
Each of `ServiceRecord`, `MileageLog`, `EngineHoursLog`, `FuelLog` gets its own controller, DTOs, validator, and service, rather than a shared polymorphic shape like assets got.
**Rationale**: unlike the 10 asset types (which share a TPH base and a large common field set), these four entities have almost no field overlap and different scoping (`AssetId` vs `EngineId`). Forcing them into one shape would produce a DTO that's mostly null on every record. **Alternative considered**: a generic `/tracking-events` endpoint with a `recordType` discriminator — rejected, the four payload shapes don't share enough structure to make a discriminated union less code than four small controllers.

### 2. `EngineHoursLog` is nested under `/engines/{engineId}/hours-logs`; the other three are nested under `/assets/{assetId}/...`
Mirrors the data model directly: hours belong to an engine, the other three belong to the asset (with `FuelLog`/`ServiceRecord` optionally tagging an `EngineId` as a body field, not a route segment, since they're still fundamentally asset-level events that may reference an engine).
**Alternative considered**: putting all four under `/assets/{assetId}/...` and passing `engineId` as a query/body param for hours logs too — rejected, it would let a client query hours logs without knowing which engine they belong to, which doesn't match the domain (hours are meaningless without an engine).

### 3. Tracking-record delete is Contributor+Owner, not Owner-only
Departs from the asset/engine pattern (Owner-only delete) established in `asset-management`.
**Rationale**: deleting an asset or engine is a structural, hard-to-undo household decision; deleting a mis-entered fuel log or service record is routine correction of day-to-day data entry, and requiring Owner approval for every typo fix would be friction with no safety benefit. **Alternative considered**: keep Owner-only delete everywhere for consistency — rejected as needless friction; the role matrix in `household-multitenancy` governs "add/edit" for Contributor+Owner already, and this change's specs extend that same Contributor capability to delete for these four capabilities specifically (captured as new requirements in the new capability specs, not as a change to the existing `household-multitenancy` spec text).

### 4. Authorization lookup: asset-scoped records reuse `GetHouseholdIdForAssetAsync`; the hours-log controller adds a parallel `GetHouseholdIdForEngineAsync`
`EngineHoursLogsController` needs `Engine → Asset → HouseholdId`. Rather than extending `IAssetService`, a new `IEngineService.GetHouseholdIdForEngineAsync(engineId)` is added, keeping the lookup colocated with the entity that owns the join.
**Alternative considered**: have `EngineHoursLogsController` call `IEngineService` then `IAssetService` to chain the two lookups — rejected as an unnecessary extra round trip; a single method that joins `Engine` to `Asset` is simpler and mirrors the existing `GetHouseholdIdForAssetAsync` pattern.

### 5. List endpoints support a `?from=&to=` date-range filter; no other filtering
All four list endpoints (`GET .../service-records`, etc.) accept optional `from`/`to` `DateOnly` query params filtering on each record's `Date` field, since the realistic use case is "show me this asset's history for the last year," not arbitrary filtering.
**Alternative considered**: no filtering at all, leave it to the frontend — rejected since these lists can grow unbounded over an asset's lifetime and a basic date filter is cheap to add now versus retrofitting pagination later.

## Risks / Trade-offs

- **[Risk]** Four near-identical CRUD controllers/services is more boilerplate than one generic endpoint → **Mitigation**: accepted trade-off per Decision 1; the shapes genuinely differ enough that a shared abstraction would be worse, not better.
- **[Risk]** Allowing Contributor to delete tracking records is a role-matrix departure from `asset-management` that could read as inconsistent → **Mitigation**: documented explicitly in Decision 3 and called out per-capability in the new specs rather than silently changing `household-multitenancy`'s existing text.
- **[Risk]** Unbounded list growth without pagination (only date filtering) → **Mitigation**: acceptable for now given expected household-scale data volumes (a single asset's lifetime history is at most a few hundred entries); add pagination if this proves wrong post-launch.

## Migration Plan

No new migration — `core-solution-structure`'s migration already created `ServiceRecords`, `MileageLogs`, `EngineHoursLogs`, `FuelLogs` tables with their FKs and indexes. This change is service/controller wiring only.

## Open Questions

None — Registration/Warranty (the remaining cross-cutting entities with the open file-storage question) are explicitly deferred to the next change.
