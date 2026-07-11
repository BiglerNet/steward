## 1. Domain restructure

- [x] 1.1 (Domain) Add `AssetCategory` enum (20 initial values per design D2) and `AssetGroup` enum (Road, Powersport, Water, Trailer, Equipment); add required `Category` property to `Asset` base
- [x] 1.2 (Domain) Restructure `Vehicle` to concrete class with `Vin`, `Make`, `Model`, `Color`, `TrackLengthIn`; restructure `Boat` to concrete sibling of `Vehicle` (keep `Hin`, `HullMaterial`, `LengthFt`, `BeamFt`, add `Make`/`Model`/`Color`)
- [x] 1.3 (Domain) Restructure `Trailer` and `Equipment` to concrete classes with merged nullable fields; delete the 10 leaf classes (`Car`, `Truck`, `Utv`, `Snowmobile`, `SnowmobileTrailer`, `EnclosedTrailer`, `RidingMower`, `PowerWasher`, `SmallEngine` and old `Boat` leaf behavior); remove the old `AssetType` enum

## 2. Asset type registry (Application)

- [x] 2.1 (Application) Create `AssetTypes/` area: registry entry model (category, group, structural type, display label, default usage tracking mode, typically-has-engine, VIN decode support enum, typical permit kinds, applicable fields, icon color) and static `AssetTypeRegistry` with one entry per category (usage defaults and icon colors per design D3/Open Questions; icon colors from `docs/design/tokens.md`)
- [x] 2.2 (UnitTests) Bijection test: every `AssetCategory` value has exactly one registry entry and no orphan entries; applicable-fields-match-structural-type test

## 3. Infrastructure

- [x] 3.1 (Infrastructure) Update `AssetConfiguration.cs`: TPH discriminator for the four structural classes; map `Category` using the existing enum persistence convention
- [x] 3.2 (Infrastructure) Rewrite `AssetMapper.cs`: category → structural class instantiation via registry (single 4-case structural switch); flat entity↔DTO mapping for merged fields
- [x] 3.3 (Infrastructure) Delete `Migrations/` folder and generate fresh `InitialCreate`; verify `dotnet ef database update` applies cleanly on a fresh database; recreate local dev/test databases (compose volume + `steward_test`)

## 4. Application services & validation

- [x] 4.1 (Application) Update asset DTOs: `CreateAssetRequest` with required `category` and nullable `usageTrackingMode`; `UpdateAssetRequest` without category; `AssetResponse` with `category` + `structuralType`
- [x] 4.2 (Application) Registry-driven FluentValidation: non-null type-specific field not in the category's `applicableFields` → validation error naming the field (create and update)
- [x] 4.3 (Application/Infrastructure) Asset service: apply registry `defaultUsageTrackingMode` when request omits it; implement `?category=` and `?group=` list filtering; reject category change on update

## 5. Api

- [x] 5.1 (Api) New `AssetTypesController`: `GET /api/asset-types`, `[AllowAnonymous]`, returns all registry entries
- [x] 5.2 (Api) Update `AssetsController` for the new create/list/update contract; ensure OpenAPI document reflects new enums and DTOs

## 6. Backend tests

- [x] 6.1 (IntegrationTests) Update all asset CRUD tests for `category` contract; add scenarios: category→structural mapping (Snowmobile→Vehicle discriminator), inapplicable field 400 (create + update), usage-tracking default applied, `?category=`/`?group=` filters, unknown category 400, category-change-on-update 400
- [x] 6.2 (IntegrationTests) Asset-types endpoint test: anonymous 200 with one entry per category and all fields populated; fix any other tests referencing old `assetType` values

## 7. Frontend

- [x] 7.1 (Web) Run `npm run generate:api`; add `api/assetTypes.ts` fetch function and `useAssetTypeRegistry()` hook (`staleTime`/`gcTime` Infinity); update `api/types.ts` re-exports
- [x] 7.2 (Web) Replace `lib/assetTypeFieldConfig.ts` with thin `lib/assetTypes.ts` (field label/input-kind presentation map + clear-inapplicable-fields helper taking a registry entry); delete the old module
- [x] 7.3 (Web) Update `AssetFormDialog`: category select grouped by registry group, registry-driven type-specific fields, usage-tracking prefill on category selection, category locked on edit, loading/error gate on registry
- [x] 7.4 (Web) Update asset list page (category filter, registry labels/icon colors) and detail views (display label, applicable fields); update `AssetListPage`/`AssetDetailLayout`/`AssetFormDialog`/`EnginesSection` tests with registry mocks

## 8. Verification

- [x] 8.1 `dotnet build` + `dotnet test` green; `npm run build`, `npm run lint`, `npm test` green
- [x] 8.2 End-to-end smoke: fresh DB via compose, create one asset per structural type through the UI, verify list/filter/detail/edit and Scalar doc renders the new contract
