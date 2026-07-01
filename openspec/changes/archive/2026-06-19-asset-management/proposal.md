## Why

Households and auth now exist, but there is no way to actually create or manage the assets a household owns — the entire point of the product. This change wires up the asset hierarchy (already modeled in the domain layer as TPH entities) and the Engine entity behind household-scoped REST endpoints, so a household can start cataloging their snowmobiles, boats, trailers, and equipment.

## What Changes

- Add CRUD endpoints for assets across all 10 concrete asset types (Snowmobile, Utv, Boat, Car, Truck, SnowmobileTrailer, EnclosedTrailer, RidingMower, PowerWasher, SmallEngine), scoped to a household.
- Add a discriminated request/response DTO shape so a single `POST /api/households/{householdId}/assets` endpoint can create any asset type via an `assetType` field, and `GET` responses include only the fields relevant to that type.
- Add nested CRUD endpoints for Engines under an asset (`/api/households/{householdId}/assets/{assetId}/engines`), including the Active/Retired replacement workflow (retiring an engine instead of deleting it when it has logged history — full history-impact logic lands in the tracking change; this change only exposes Status transitions).
- Enforce existing household role capability matrix: Viewer can read, Contributor/Owner can create and edit, Owner only can delete.
- Add EF Core configurations and a migration for the asset/engine tables (already scaffolded as entities/configurations in `core-solution-structure`, but not yet exercised by any service or controller).

## Capabilities

### New Capabilities
- `asset-management`: Household-scoped CRUD for the polymorphic asset hierarchy (Vehicle/Trailer/Equipment and their concrete leaf types).
- `engine-management`: CRUD and lifecycle (Active/Retired) for Engines attached to an asset.

### Modified Capabilities
- (none — the existing `household-multitenancy` capability matrix already specifies the role rules this change enforces; no requirement text changes)

## Impact

- **Domain**: No new entities — `Asset` hierarchy and `Engine` already exist from `core-solution-structure`. May need minor property adjustments discovered during DTO design (see design.md open questions).
- **Application**: New `Steward.Application.Assets` namespace — DTOs, `IAssetService`, FluentValidation validators per asset type.
- **Infrastructure**: New `AssetService` implementation; first EF Core migration that actually creates the `Assets` and `Engines` tables (TPH discriminator + indexes).
- **Api**: New `AssetsController`, new `EnginesController` (nested route), both under `[Authorize]` + resource-based household authorization.
- **Dependencies**: None new — reuses `HouseholdOperations`/`HouseholdResource` authorization from `auth-and-households`.
