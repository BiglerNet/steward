## Context

The `Asset` TPH hierarchy and `Engine` entity were modeled and EF-configured in `core-solution-structure`, but no migration has created their tables and no service/controller exercises them. This change is the first to actually read/write rows in those tables, so it also produces the first real migration for them. Households and resource-based authorization (`HouseholdOperations`, `HouseholdResource`) already exist from `auth-and-households` and are reused as-is — no new authorization primitives are needed.

There are 10 concrete asset types across 3 abstract groups (Vehicle, Trailer, Equipment). The API needs a shape that doesn't require 10 near-identical controllers/DTOs/validators while still giving the frontend type-safe access to type-specific fields (VIN, HIN, trailer length, etc.).

## Goals / Non-Goals

**Goals:**
- CRUD for all 10 asset types through a single set of household-scoped endpoints.
- Nested CRUD for Engines under an asset, including Active/Retired status transitions.
- Reuse existing household role enforcement (Viewer/Contributor/Owner) with no new authorization concepts.
- Produce the first real EF Core migration for `Assets` and `Engines`.

**Non-Goals:**
- Tracking records (ServiceRecord, MileageLog, EngineHoursLog, FuelLog) — separate change.
- Registration/Warranty documents and file attachments — separate change.
- Public garage view / cross-household visibility — separate change.
- Historical-integrity logic for engine swaps (e.g., recomputing hours when an engine is retired mid-asset-life) — this change only exposes the `Status` field; the math lands with EngineHoursLog.

## Decisions

### 1. Single polymorphic endpoint per resource, not one endpoint per asset type
`POST/GET/PUT/DELETE /api/households/{householdId}/assets[/{assetId}]` handles all 10 types via an `assetType` discriminator field in the request/response body, instead of `/snowmobiles`, `/boats`, etc.
**Alternative considered**: per-type routes — rejected, would mean 10x the controllers/routes for no behavioral difference, and mirrors the TPH "one table" decision already made at the persistence layer.

### 2. Flat superset DTO instead of 10 request/response classes
`AssetResponse` and `CreateAssetRequest`/`UpdateAssetRequest` contain every field used by any asset type as nullable properties, plus the required `assetType` discriminator and shared base fields (`name`, `description`, `year`, `usageTrackingMode`, etc.).
**Alternative considered**: a response class per type — rejected for now because it would explode the generated OpenAPI schema and the TS client into ~30 near-duplicate types; the frontend can still get a discriminated union by narrowing on `assetType` client-side. Revisit if the type-specific field count grows much further.

### 3. Per-type validation rules branch inside one FluentValidation validator
`CreateAssetRequestValidator` uses `When(x => x.AssetType == AssetType.Boat, () => ...)` style branches rather than one validator class per type.
**Alternative considered**: a validator-selection factory keyed by type — rejected as unnecessary indirection for 10 types; revisit if this file exceeds ~150 lines or the branching becomes hard to follow.

### 4. `AssetType` is immutable after creation
`UpdateAssetRequest` does not include `assetType`. If an asset was miscategorized, the client must delete and recreate it.
**Rationale**: changing the discriminator on an existing TPH row means changing which subset of columns are "valid," which has no clean semantics (e.g. a Boat's `Hin` becoming meaningless if it's retyped to a Truck). Recreation is simple and assets have no deep history yet at this stage of the project.

### 5. Asset deletion cascades to its Engines; Engine deletion is otherwise unconstrained for now
`DeleteBehavior.Cascade` from Asset → Engine (an Engine has no existence independent of its Asset). A direct `DELETE` on an Engine is allowed unconditionally in this change.
**Rationale**: Once the tracking change adds `EngineHoursLog`/`FuelLog` with FKs to `Engine`, those will use `DeleteBehavior.Restrict`, which will then naturally block deleting an engine with logged history — no bespoke "has history" check needs to be written now or later.

### 6. List endpoint is one array across all types, with optional filter
`GET /api/households/{householdId}/assets` returns all assets regardless of type (each row carries `assetType`), with an optional `?assetType=Boat` query filter. No separate "list by type" route.

## Risks / Trade-offs

- **[Risk]** Flat DTO with ~15 nullable fields is less self-documenting than per-type schemas → **Mitigation**: group fields with clear naming (`vin`, `hin`, `trailerLengthFt`, etc.) and document in the spec which fields apply to which `assetType` values; revisit if it gets unwieldy.
- **[Risk]** Engine endpoints need an asset's `householdId` to authorize, requiring a join from `Engine` → `Asset` → `HouseholdId` → **Mitigation**: `IAssetService` exposes `GetHouseholdIdForAssetAsync(assetId)` used by `EnginesController` before calling `IAuthorizationService`.
- **[Risk]** Validation branching could become hard to follow as more asset-specific rules accrue → **Mitigation**: documented threshold (~150 lines) to split into per-type validators.

## Migration Plan

Single new EF Core migration `AddAssetsAndEngines` creating the `Assets` (TPH, single table, `Discriminator` column) and `Engines` tables plus their indexes/FKs. Greenfield — no existing data to migrate. Rollback is a standard `dotnet ef database update <previous>`.

## Open Questions

- `PhotoUrl` is accepted as a plain string for now (no upload endpoint). File upload/storage strategy is deferred to the same future decision noted as open in `core-solution-structure` (file storage provider, still unresolved) and will likely also cover Registration/Warranty document attachments.
