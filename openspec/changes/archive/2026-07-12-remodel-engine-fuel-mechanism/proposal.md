## Why

`Engine.FuelType` currently conflates two unrelated concepts — combustion mechanism (`TwoStroke`, `FourStroke`) and the actual fuel/energy carrier (`Gasoline`, `Diesel`, `Electric`, `None`) — which makes the field impossible to reason about (a value like `TwoStroke` says nothing about what fuel is burned) and blocks adding real mechanism variety (Diesel two-strokes, Rotary/Wankel) without further overloading it. Separately, `EngineType.Hybrid` exists but is a dead end: a single `Engine` row can't coherently represent a gas engine and an electric motor at once. And `FuelLog` is volume-only (`Gallons`/`Liters`), so there is no way to log an EV charging event at all. This change resolves all three by splitting mechanism from fuel, modeling hybrids as two `Engine` rows instead of one ambiguous value, and generalizing `FuelLog` to carry energy (`Kwh`) alongside volume.

## What Changes

- `EngineType` narrowed to `Ice | Electric`. `Hybrid` is removed as a stored value — **BREAKING**. "This asset is a hybrid" becomes a derived fact (an asset has both an `Active` `Ice` engine and an `Active` `Electric` engine), never persisted.
- New nullable `Mechanism` enum on `Engine`, set only when `EngineType == Ice`: `TwoStroke | FourStroke | Diesel | Rotary`. Deliberately flat and deliberately excludes aircraft-only mechanisms (`GasTurbine`, `Ramjet`, `Scramjet`, `Rocket`) until an aircraft asset type actually exists.
- `FuelType` trimmed to `Gasoline | Diesel | Propane`, now nullable, meaningful only when `EngineType == Ice` — **BREAKING**. `TwoStroke`/`FourStroke` move to `Mechanism`; `Electric` and `None` are removed (redundant now that `EngineType` and nullability cover those cases).
- New nullable `bool IsExternallyChargeable` on `Engine`, meaningful only when `EngineType == Electric`: distinguishes a plug-in-rechargeable motor/battery (BEV, PHEV) from one that's only ever charged internally (a conventional hybrid's motor/generator). Drives both the derived Hybrid vs. Plug-in Hybrid label and which engines are valid fuel/energy-log targets.
- New nullable `TwoStrokeOilDelivery` enum (`Premix | OilInjected`) and nullable `string TwoStrokeMixRatio` on `Engine`, both meaningful only when `Mechanism == TwoStroke`. Captures that many powersport two-strokes (snowmobiles) meter oil from a separate reservoir rather than requiring hand-premixed fuel.
- `FuelLog.Volume`/`VolumeUnit` generalized to `Quantity`/`Unit`, where `Unit` is `Gallons | Liters | Kwh` — **BREAKING**. Supports logging EV charging events, not just liquid fuel fillups.
- `FuelLog.EngineId` (present in the schema today but never actually populated by the frontend) becomes functionally required when an asset has 2+ "loggable engines" (`Active` `Ice` engines plus `Active` `Electric` engines with `IsExternallyChargeable = true`): auto-selected silently when exactly one loggable engine exists, required to pick when two or more exist (the PHEV case — gas pump vs. plug), and left optional/free-form when zero engines are modeled.
- Asset responses gain a derived, non-persisted powertrain summary (`Ice`-only → none, `Electric`-only → `Electric`, `Ice`+non-chargeable `Electric` → `Hybrid`, `Ice`+chargeable `Electric` → `Plug-in Hybrid`), computed from `Active` engines only.
- Asset creation wizard's engine-type step keeps four user-facing choices (`Ice`, `Electric`, `Hybrid`, `Plug-in Hybrid`) though only `Ice`/`Electric` are ever stored; selecting `Hybrid`/`Plug-in Hybrid` creates two `Engine` records in one wizard step.
- `EngineType`, `FuelType`, `Mechanism`, and `TwoStrokeOilDelivery` are persisted as strings (`HasConversion<string>()`) rather than int ordinals, per project convention going forward.
- Frontend gains UI for the existing but unexposed engine `Retire`/`Reactivate`/`MarkBroken` endpoints, since the derived powertrain label depends on accurate `Active` status.
- Since the project is pre-launch, EF Core migrations are reset to a single regenerated `InitialCreate` rather than layering incremental migrations — no data migration required.

## Capabilities

### New Capabilities
_None — this change modifies existing capabilities' requirements; no new capability domain is introduced._

### Modified Capabilities
- `domain-model`: `Engine` entity gains/changes fields (`EngineType`, `FuelType`, `Mechanism`, `IsExternallyChargeable`, `TwoStrokeOilDelivery`, `TwoStrokeMixRatio`); `FuelLog` entity's `Volume`/`VolumeUnit` become `Quantity`/`Unit`; migration reset requirement updated.
- `engine-management`: create/update engine endpoints accept the new/changed fields; validation rules for field applicability (e.g. `Mechanism` only when `EngineType == Ice`) are added.
- `fuel-tracking`: create/update fuel log endpoints accept `quantity`/`unit` instead of `volume`/`volumeUnit`; `engineId` selection/requirement logic added based on the asset's loggable engines.
- `asset-management`: `AssetResponse` (or equivalent) gains a derived, non-persisted powertrain summary field.
- `frontend-tracking-records`: fuel log create/edit form gains engine auto-selection/selector behavior (previously explicitly omitted for fuel logs); engine record UI gains retire/reactivate/mark-broken actions.
- `frontend-asset-creation-wizard`: Engine step's engine-type choice expands to four options and can create two `Engine` records in one submission.

## Impact

- **Steward.Domain**: `Engine` entity (new/changed properties), `FuelLog` entity (`Volume`→`Quantity`, `VolumeUnit`→`Unit`), new enums `Mechanism`, `TwoStrokeOilDelivery`; `EngineType`/`FuelType` enum value sets change.
- **Steward.Infrastructure**: `EngineConfiguration`/`FuelLogConfiguration` (`HasConversion<string>()` for the affected enums), `EngineService`, fuel log service, EF Core migrations reset to a single `InitialCreate`.
- **Steward.Application**: `Engines/Dtos.cs` + `Validators.cs`, `Tracking/FuelLogs/Dtos.cs` + `Validators.cs`, asset response DTOs (derived powertrain field).
- **Steward.Api**: engine and fuel log controllers (request/response shape changes), asset controller (response shape change).
- **Steward.Web**: `EnginesSection.tsx` (new fields, conditional visibility, status-transition actions), `FuelLogsPage.tsx` (quantity/unit, engine auto-select/selector), `AssetCreateWizardPage.tsx`/`EngineStep.tsx` (four-way engine-type choice, dual-engine creation), asset detail/list views (powertrain badge), `api/schema.d.ts` (regenerate via `generate:api`).
- **BREAKING**: `EngineType.Hybrid`, `FuelType.TwoStroke|FourStroke|Electric|None`, and `FuelLog.Volume`/`VolumeUnit` are removed/renamed. Acceptable pre-launch with no production data; EF migrations reset rather than migrated forward.
