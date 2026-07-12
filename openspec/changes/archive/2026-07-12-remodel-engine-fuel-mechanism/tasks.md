## 1. Domain (Steward.Domain)

- [x] 1.1 Update `EngineType` enum to `Ice | Electric` (remove `Hybrid`)
- [x] 1.2 Add new `Mechanism` enum (`TwoStroke | FourStroke | Diesel | Rotary`)
- [x] 1.3 Update `FuelType` enum to `Gasoline | Diesel | Propane`
- [x] 1.4 Add new `TwoStrokeOilDelivery` enum (`Premix | OilInjected`)
- [x] 1.5 Update `Engine` entity: make `FuelType` nullable, add nullable `Mechanism`, nullable `bool? IsExternallyChargeable`, nullable `TwoStrokeOilDelivery?`, nullable `string? TwoStrokeMixRatio`
- [x] 1.6 Update `FuelLog` entity: rename `Volume`→`Quantity`, `VolumeUnit`→`Unit`
- [x] 1.7 Update `VolumeUnit` enum (or introduce a replacement) to include `Kwh` alongside `Gallons`/`Liters`

## 2. Infrastructure (Steward.Infrastructure)

- [x] 2.1 Update `EngineConfiguration`: add `HasConversion<string>()` for `EngineType`, `Mechanism`, `FuelType`, `TwoStrokeOilDelivery`
- [x] 2.2 Update `FuelLog`'s entity configuration: add `HasConversion<string>()` for `Unit`, rename column mappings for `Quantity`/`Unit` if explicitly configured
- [x] 2.3 Update `EngineService` create/update/`ToResponse` mapping for the new/changed `Engine` fields
- [x] 2.4 Update the fuel log service's create/update/response mapping for `Quantity`/`Unit`
- [x] 2.5 Add derived powertrain computation (from `Active` engines) to the asset query/mapping path used for `AssetResponse` and list/summary projections
- [x] 2.6 Delete existing EF Core migrations and regenerate a single `InitialCreate` reflecting the updated schema
- [x] 2.7 Verify `DashboardService`'s Cylinder Index / aggregate queries (`EngineType.Ice` filter) still compile and behave correctly against the narrowed `EngineType`

## 3. Application (Steward.Application)

- [x] 3.1 Update `Engines/Dtos.cs` (`EngineResponse`, `CreateEngineRequest`, `UpdateEngineRequest`) with the new/changed fields
- [x] 3.2 Update `Engines/Validators.cs`: reject `mechanism`/`fuelType` unless `engineType = Ice`; reject `isExternallyChargeable` unless `engineType = Electric`; reject `twoStrokeOilDelivery`/`twoStrokeMixRatio` unless `mechanism = TwoStroke`
- [x] 3.3 Update `Tracking/FuelLogs/Dtos.cs` (`FuelLogResponse`, `CreateFuelLogRequest`, `UpdateFuelLogRequest`): `volume`/`volumeUnit` → `quantity`/`unit`
- [x] 3.4 Update `Tracking/FuelLogs/Validators.cs` for the renamed/widened fields
- [x] 3.5 Add `Powertrain` (or similar) field to the asset response DTO(s) used by `AssetResponse` and any list/summary DTO

## 4. Api (Steward.Api)

- [x] 4.1 Verify engine and fuel log controllers pass through the updated DTOs without changes needed beyond recompilation
- [x] 4.2 Confirm asset controller endpoints surface the new derived powertrain field on single-asset and list responses

## 5. Frontend — Engine form and status UI (Steward.Web)

- [x] 5.1 Update `EnginesSection.tsx`: replace `ENGINE_TYPES`/`FUEL_TYPES` constants and Zod schema for the new enum values
- [x] 5.2 Add `Mechanism` field to the engine form, shown only when `engineType = Ice`
- [x] 5.3 Add `TwoStrokeOilDelivery` and `TwoStrokeMixRatio` fields, shown only when `mechanism = TwoStroke`; relabel `RecommendedOilType`'s label based on `mechanism` (2-stroke oil vs. engine oil)
- [x] 5.4 Add `IsExternallyChargeable` field, shown only when `engineType = Electric`
- [x] 5.5 Add Retire/Reactivate/Mark Broken action buttons to the engine table, gated by the engine's current `Status`, calling the existing backend endpoints
- [x] 5.6 Add a derived powertrain badge (Electric/Hybrid/Plug-in Hybrid) to the asset detail page and/or asset list cards, sourced from the new response field

## 6. Frontend — Fuel logs (Steward.Web)

- [x] 6.1 Update `FuelLogsPage.tsx` form/schema: `volume`/`volumeUnit` → `quantity`/`unit`, add `Kwh` as a selectable unit
- [x] 6.2 Compute the asset's "loggable engines" (Active Ice + Active externally-chargeable Electric) from the asset's engine list
- [x] 6.3 Implement auto-select behavior: 0 loggable engines → free unit choice, no selector; 1 → silent auto-select, unit constrained to that engine's supported units; 2+ → require engine selection, constrain unit to the selected engine
- [x] 6.4 Remove the hardcoded `engineId: null` and wire the resolved engine id into create/update submissions

## 7. Frontend — Asset creation wizard (Steward.Web)

- [x] 7.1 Update `EngineStep.tsx`: add `Hybrid`/`Plug-in Hybrid` to the engine-type choice, alongside `Ice`/`Electric`
- [x] 7.2 Show two field groups (gas engine, electric motor) when `Hybrid`/`Plug-in Hybrid` is selected; hide/discard the electric group's values when switching away
- [x] 7.3 Update `AssetCreateWizardPage.tsx`'s creation flow to issue two sequential engine-create calls when `Hybrid`/`Plug-in Hybrid` was selected, setting `isExternallyChargeable` accordingly on the electric engine
- [x] 7.4 Update the wizard's engine-creation failure handling to account for a partially-completed pair (one of two engines created) on retry

## 8. API contract sync

- [x] 8.1 Run `npm run generate:api` to regenerate `src/Steward.Web/src/api/schema.d.ts` against the updated OpenAPI contract
- [x] 8.2 Update any hand-written frontend types (`api/types.ts`) that mirror the changed enums/fields

## 9. Tests

- [x] 9.1 Unit tests: engine validators (field-applicability rules), fuel log validators (unit enum)
- [x] 9.2 Integration tests: create/update engine with the new fields and rejection cases; create fuel log with `Kwh` unit; derived powertrain computation for Ice-only/Electric-only/Hybrid/Plug-in-Hybrid/retired-engine scenarios
- [x] 9.3 Frontend tests: `EnginesSection` conditional field visibility, `FuelLogsPage` auto-select/selector behavior, wizard dual-engine creation

## 10. Manual verification

- [x] 10.1 Run the app locally (`docker compose up -d postgres`, `dotnet run --project src/Steward.Api`, `npm run dev`) and walk through: creating a snowmobile with a two-stroke oil-injected engine, creating a PHEV via the wizard, logging a gas fillup and an electric charge on the PHEV, retiring/reactivating an engine
