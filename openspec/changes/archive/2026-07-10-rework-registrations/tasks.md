# Tasks: rework-registrations

## 1. Domain

- [x] 1.1 Add `RegistrationKind` enum (Registration | TrailPass | Permit) in `Steward.Domain/Enums`
- [x] 1.2 Update `Registration` entity: add `Kind` (RegistrationKind), `ValidFrom` (DateOnly?); make `RegistrationNumber` nullable
- [x] 1.3 Add nullable `LicensePlate` to `Vehicle` and `Trailer` structural classes
- [x] 1.4 Add nullable `Country` and `Region` to `Household`

## 2. Region registry (backend)

- [x] 2.1 Create `Steward.Application/Regions`: `RegionDefinition`/`CountryDefinition` records + static `RegionRegistry` (US: 50 states + DC; CA: 13 provinces/territories) with `IsValidRegion(country, region)` / `IsValidCountry(code)` helpers
- [x] 2.2 Add `RegionsController` (`api/regions`, GET, `[AllowAnonymous]`) returning the registry; DTOs visible in OpenAPI
- [x] 2.3 Unit tests: registry counts (51 US / 13 CA), code prefixes, lookup helpers

## 3. Asset type registry + asset plumbing for licensePlate

- [x] 3.1 Add `licensePlate` to `IAssetTypeFields`, `CreateAssetRequest`, `UpdateAssetRequest`, `AssetResponse`, `AssetTypeFieldCheck`, and `AssetMapper.ApplyTypeFields`/`ToResponse`
- [x] 3.2 Add `licensePlate` to registry `applicableFields` for Car, Truck, Suv, Van, Motorcycle, and all four trailer categories
- [x] 3.3 Unit test: `typicalPermitKinds` values in the registry all parse to `RegistrationKind` members; existing registry structural-fields test covers `licensePlate`

## 4. Registrations backend

- [x] 4.1 Update registration DTOs: add `Kind` (required) + `ValidFrom` to create/update/response; make `RegistrationNumber` nullable
- [x] 4.2 Update validators: `kind` must be a defined enum value; drop `registrationNumber` NotEmpty (keep max length); sane-date rules unchanged
- [x] 4.3 Update `RegistrationService`: map new fields; list ordering `expiresOn` DESC NULLS LAST, then `validFrom` DESC NULLS LAST
- [x] 4.4 Update unit tests for validators; integration tests: trail pass without number (201), missing/unknown kind (400), kind edit (200), ordering with missing `expiresOn`

## 5. Households backend

- [x] 5.1 Add `country`/`region` to household DTOs (create/update/response) and map in `HouseholdService`
- [x] 5.2 Validators: country/region validated against `RegionRegistry` (region requires country, must belong to it)
- [x] 5.3 Integration tests: create with location, update set/clear location, mismatched region 400, region-without-country 400

## 6. Migration reset

- [x] 6.1 Delete `src/Steward.Infrastructure/Migrations/`, regenerate `InitialCreate`, apply to a clean database; confirm new columns (`Assets.LicensePlate`, `Registrations.Kind`/`ValidFrom` with nullable number, `Households.Country`/`Region`)
- [x] 6.2 Run full backend test suite (unit + integration) green

## 7. Frontend API layer

- [x] 7.1 Regenerate `schema.d.ts` from the running API; update `api/types.ts` (RegistrationKind, kind/validFrom on registration types, licensePlate on asset types, country/region on household types, region registry types)
- [x] 7.2 Add `api/regions.ts` (`listRegions()`) and `hooks/useRegionRegistry.ts` (staleTime/gcTime Infinity)
- [x] 7.3 Add `licensePlate` to `FIELD_PRESENTATION` in `lib/assetTypes.ts`

## 8. Frontend registrations UI

- [x] 8.1 Registration form: required kind select, per-kind number label, `validFrom` date input, issuing-authority combobox seeded from region registry (household region first, free text allowed)
- [x] 8.2 Registration list: kind badges; verify current-first ordering renders as returned
- [x] 8.3 Renew action: button per record opening the create form prefilled with dates/cost cleared
- [x] 8.4 Permit nudges on the registrations tab from registry `typicalPermitKinds` vs current records
- [x] 8.5 Component tests: kind badge + form kind requirement, renew prefill, nudge shown/cleared, combobox suggestion order

## 9. Frontend asset detail + household settings

- [x] 9.1 Asset detail header shows `licensePlate` when populated (no placeholder otherwise); form picks it up via registry (verify, no hardcoding)
- [x] 9.2 Household settings: country/region selectors (Owner-editable, region cleared on country change) wired to `PUT /api/households/{id}`
- [x] 9.3 Component tests: plate in detail header, settings location set/clear + country-change reset

## 10. Verification

- [x] 10.1 `dotnet build` + `dotnet test` green; `npm test`, `tsc -b`, lint, `vite build` green
- [x] 10.2 API-level smoke: household with region, car with plate, registration + trail pass lifecycle incl. renew flow payloads, `/api/regions` anonymous 200
