## 1. Domain — Adjustments Discovered During DTO Design

- [x] 1.1 [Domain] Review `Asset`/`Vehicle`/`Trailer`/`Equipment`/leaf-type properties against the flat DTO field list (design.md Decision 2); add any missing type-specific properties found (e.g. confirm `SnowmobileTrailer`/`EnclosedTrailer` have plate/registration-adjacent fields if any were omitted in `core-solution-structure`).
- [x] 1.2 [Domain] Add an `AssetType` enum (or reuse the existing `Discriminator` string convention) if a typed discriminator is preferred over a raw string in DTOs — confirm against `AssetConfiguration.HasDiscriminator` values.

## 2. Application — DTOs and Validators (Assets)

- [x] 2.1 [Application] Create `Steward.Application.Assets` namespace with `AssetResponse` (flat superset record, all type-specific fields nullable, includes `assetType`, `id`, `householdId`, shared base fields).
- [x] 2.2 [Application] Create `CreateAssetRequest` and `UpdateAssetRequest` records (same shape as `AssetResponse` minus generated fields; `UpdateAssetRequest` excludes `assetType`).
- [x] 2.3 [Application] Create `CreateAssetRequestValidator` (FluentValidation) — required `assetType` (must be a known type), required `name`, `When(...)` branches for type-specific field rules per design.md Decision 3.
- [x] 2.4 [Application] Create `UpdateAssetRequestValidator` — same shared rules as create, rejects a present `assetType` that differs from the asset's existing type (validated in the service, not the validator, since it needs the existing entity).
- [x] 2.5 [Application] Define `IAssetService` with `CreateAsync`, `ListAsync` (with optional type filter), `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`, and `GetHouseholdIdForAssetAsync` (used by `EnginesController` for authorization).

## 3. Application — DTOs and Validators (Engines)

- [x] 3.1 [Application] Create `EngineResponse`, `CreateEngineRequest`, `UpdateEngineRequest` records under `Steward.Application.Assets.Engines` (or a sibling `Engines` namespace).
- [x] 3.2 [Application] Create `CreateEngineRequestValidator` / `UpdateEngineRequestValidator` — `label` required, numeric fields (`cylinders`, `displacementCc`, mileage/hours snapshots) non-negative when present.
- [x] 3.3 [Application] Define `IEngineService` with `CreateAsync`, `ListAsync`, `UpdateAsync`, `RetireAsync`, `ReactivateAsync`, `DeleteAsync` — all scoped by `assetId`.

## 4. Infrastructure — EF Core Migration

- [x] 4.1 [Infrastructure] Review `AssetConfiguration`/`EngineConfiguration` (already written in `core-solution-structure`) for correctness against the finalized DTO field list from Section 1; adjust column mappings if Section 1 added properties.
- [x] 4.2 [Infrastructure] Configure `DeleteBehavior.Cascade` from `Asset` → `Engine` in `EngineConfiguration` (per design.md Decision 5).
- [x] 4.3 [Infrastructure] Generate EF Core migration `AddAssetsAndEngines` (`dotnet ef migrations add`) creating the `Assets` and `Engines` tables, discriminator column, and indexes (`HouseholdId`, `AssetId`).
- [x] 4.4 [Infrastructure] Apply the migration against the local docker-compose PostgreSQL instance and verify schema via `psql \d`.

## 5. Infrastructure — Service Implementations

- [x] 5.1 [Infrastructure] Implement `AssetService : IAssetService` — maps `CreateAssetRequest.assetType` to the correct concrete `Asset` subclass via a switch expression before calling `DbContext.Assets.Add`.
- [x] 5.2 [Infrastructure] Implement asset list/get/update/delete in `AssetService`, scoping every query by `HouseholdId` and returning 404-equivalent (`null`/throw `NotFoundException`) when an asset doesn't belong to the requested household.
- [x] 5.3 [Infrastructure] Implement `EngineService : IEngineService` — create/list/update scoped by `AssetId`; `RetireAsync`/`ReactivateAsync` validate the current `Status` before toggling (HTTP 400 via a domain exception if already in the target state).
- [x] 5.4 [Infrastructure] Register `IAssetService`/`IEngineService` in DI (`AuthServiceExtensions` or a new `AssetServiceExtensions`, following the existing `AddStewardAuth`-style extension method pattern).

## 6. Api — Assets Controller

- [x] 6.1 [Api] Create `AssetsController` at `api/households/{householdId}/assets` (versioned route per `core-solution-structure`'s API versioning decision) mirroring `HouseholdsController`'s pattern: `[Authorize]`, constructor-injected `IAssetService` + `IAuthorizationService` + validators.
- [x] 6.2 [Api] Implement `Create` — validate request, authorize `HouseholdOperations.Edit` against `new HouseholdResource(householdId)`, call service, return `CreatedAtAction`.
- [x] 6.3 [Api] Implement `List` (with `?assetType=` query param) and `GetById` — authorize `HouseholdOperations.View`.
- [x] 6.4 [Api] Implement `Update` — authorize `HouseholdOperations.Edit`, validate, reject mismatched `assetType` in body.
- [x] 6.5 [Api] Implement `Delete` — authorize `HouseholdOperations.Delete`.

## 7. Api — Engines Controller

- [x] 7.1 [Api] Create `EnginesController` at `api/households/{householdId}/assets/{assetId}/engines`, resolving `assetId`'s `HouseholdId` via `IAssetService.GetHouseholdIdForAssetAsync` before authorizing (and returning 404 if the asset doesn't belong to `householdId`).
- [x] 7.2 [Api] Implement `Create`/`List`/`Update`/`Delete` mirroring the same authorize-then-call pattern as `AssetsController`.
- [x] 7.3 [Api] Implement `Retire`/`Reactivate` POST endpoints — authorize `HouseholdOperations.Edit`, return HTTP 400 (via `ValidationProblem` or a `BadRequest` with a clear message) if the engine is already in the target status.

## 8. Tests

- [x] 8.1 [IntegrationTests] Asset CRUD happy-path tests covering at least one Vehicle, one Trailer, and one Equipment type, verifying role enforcement (Viewer 403 on write, Owner-only delete).
- [x] 8.2 [IntegrationTests] Cross-household isolation test — asset in Household A returns 404 when queried via Household B's route.
- [x] 8.3 [IntegrationTests] Engine CRUD + retire/reactivate happy-path and error-state tests (retiring an already-retired engine returns 400).
- [x] 8.4 [UnitTests] `CreateAssetRequestValidator`/`UpdateAssetRequestValidator` rule tests for the type-branching validation logic.
