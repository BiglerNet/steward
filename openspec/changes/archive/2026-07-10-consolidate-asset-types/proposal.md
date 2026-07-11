## Why

The asset type system conflates three concerns — structure (which fields exist), category (what the user calls the thing), and behavior (usage-tracking default, has-engine, VIN-decodability) — into a single TPH class hierarchy. Most concrete classes add zero fields (`Car`, `Truck`, `Utv` are empty), adding a type requires touching a C# class, discriminator mapping, mapper, and a hand-maintained frontend config that can drift, and there is no home for per-type behavior metadata needed by upcoming work (creation wizard, VIN decode, registration nudges).

## What Changes

- **BREAKING** Flatten the TPH hierarchy to four concrete structural classes that exist only where fields differ: `Vehicle` (vin, make, model, color, trackLengthIn), `Boat` (hin, hullMaterial, lengthFt, beamFt, plus make/model/color — sibling of Vehicle, not a subclass), `Trailer` (ballSizeIn, maxLoadLbs, interiorHeightFt, interiorLengthFt), `Equipment` (cuttingWidthIn, maxPsi, maxGpm, equipmentDescription). All type-specific fields nullable. The 10 leaf classes (`Car`, `Truck`, `Utv`, `Snowmobile`, `SnowmobileTrailer`, `EnclosedTrailer`, `RidingMower`, `PowerWasher`, `SmallEngine`, and concrete `Boat`) are removed as classes.
- Add a user-facing `Category` enum on `Asset` (~15–20 values: Car, Truck, Suv, Van, Motorcycle, Utv, Atv, Snowmobile, DirtBike, GolfCart, Boat, Pwc, UtilityTrailer, EnclosedTrailer, SnowmobileTrailer, BoatTrailer, RidingMower, PowerWasher, Generator, SmallEngine). Adding a future category is one enum value plus one registry entry — no new class, no mapper change.
- Add a **backend-owned asset type registry**: static config mapping each category to its group (Road | Powersport | Water | Trailer | Equipment), structural class, default `UsageTrackingMode`, typically-has-engine flag, VIN-decode support level (yes | best-effort | none), typical permit kinds, applicable field list, and icon color. Served via `GET /api/asset-types`.
- **BREAKING** Asset API contract: `CreateAssetRequest.assetType` (10 leaf values) is replaced by `category`; the server derives the structural class from the registry. List filtering moves to `?category=` (plus `?group=`). Responses expose `category` and the structural type.
- New assets default `UsageTrackingMode` from the registry entry when not explicitly provided (still stored per-asset and editable).
- Frontend: delete the hand-maintained `assetTypeFieldConfig.ts`; a `useAssetTypeRegistry()` TanStack Query hook (fetched once per session, `staleTime: Infinity`) drives labels, grouping, applicable fields, icon colors, and usage-tracking defaults in the asset form, list, and detail views.
- Migrations: the product is not live — existing EF Core migrations are deleted and a fresh `InitialCreate` is generated. No data migration.

Out of scope (follow-up changes already sketched): creation wizard and VIN decoding, registration rework (plate, permit kinds consumption), asset photos/`PhotoUrl` removal. The registry's `vinDecodeSupport`, `typicallyHasEngine`, and permit-kind fields are populated and served here but consumed by those follow-ups.

## Capabilities

### New Capabilities
- `asset-type-registry`: backend-owned per-category metadata (group, structural class, field applicability, behavior defaults) exposed via `GET /api/asset-types` and consumed by the frontend as the single source of truth for type-driven UI.

### Modified Capabilities
- `domain-model`: Asset TPH hierarchy restructured to four concrete structural classes; `Category` enum added to Asset base properties; fresh `InitialCreate` migration replaces existing migrations.
- `asset-management`: create/list/filter contract keys on `category` instead of the 10-value `assetType`; type-specific field validation driven by the registry; `UsageTrackingMode` defaulting behavior.
- `frontend-asset-management`: type-adaptive form, list filter, and detail view driven by the served registry instead of a hardcoded frontend config; category picker presents categories grouped by registry group.

## Impact

- **Domain**: `Steward.Domain/Entities/Assets/*` (10 classes deleted, 4 restructured), new `AssetCategory` enum; `AssetType` enum removed or repurposed.
- **Infrastructure**: `AssetConfiguration.cs` discriminator remapping, `AssetMapper.cs` rewrite, EF migrations folder reset, registry definition + service registration.
- **Application**: asset DTOs/validators keyed by category + registry-driven field applicability; new registry DTOs/service interface.
- **Api**: `AssetsController` contract change; new asset-types endpoint/controller.
- **Frontend**: `api/types.ts` + regenerated `schema.d.ts`, `assetTypeFieldConfig.ts` removed, new `useAssetTypeRegistry` hook, `AssetFormDialog`, asset list/detail pages, related tests.
- **Tests**: unit + integration tests covering asset CRUD and mapping updated for the new contract.
- No new external dependencies. API versioning unchanged (existing v1 surface changes are acceptable pre-launch).
