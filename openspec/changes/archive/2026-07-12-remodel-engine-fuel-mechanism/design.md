## Context

`Engine.FuelType` today mixes combustion mechanism (`TwoStroke`, `FourStroke`) with fuel/energy carrier (`Gasoline`, `Diesel`, `Electric`, `None`) in one enum. `EngineType` has an unused `Hybrid` value that no code branches on meaningfully. `FuelLog` is volume-only (`Volume`/`VolumeUnit: Gallons|Liters`), so EV charging events cannot be logged at all. `FuelLog.EngineId` exists in the schema and DTOs but the frontend (`FuelLogsPage.tsx`) hardcodes `engineId: null` on every submit — it was never wired up.

This was worked through in an explore session before this proposal; the shape below is the resolved output of that discussion, not a fresh design exercise.

The project is pre-launch (no production data), so this design resets EF Core migrations to a single `InitialCreate` rather than layering incremental migrations, per existing project convention (see `domain-model` spec's "Initial EF Core migration" requirement, established the same way for a prior change).

## Goals / Non-Goals

**Goals:**
- Separate "how the engine mechanically operates" (`Mechanism`) from "what it consumes" (`FuelType`), so both stay coherent as more engine variety is added later.
- Model hybrids as two `Engine` rows instead of a single ambiguous `Hybrid` value, eliminating fields-that-don't-apply on one row.
- Let `FuelLog` represent an EV charging event, not just a liquid-fuel fillup.
- Make `FuelLog.EngineId` do real work: auto-select when unambiguous, require a choice when genuinely ambiguous (PHEV), stay optional when no engines are modeled.
- Capture 2-stroke oil-delivery method and mix ratio, since powersport engines (snowmobiles) commonly use oil-injection rather than hand-premix.
- Leave a clear, low-cost extension path for aircraft-only mechanisms (`GasTurbine` and its subtypes, `Ramjet`, `Scramjet`, `Rocket`) without modeling them now.
- Store the affected enums as strings in Postgres, not int ordinals, so future value additions/reorderings don't corrupt existing rows.

**Non-Goals:**
- Building any Aircraft asset type or gas-turbine subtype taxonomy now — only the extension point (a nullable `Mechanism` enum that can grow) is established.
- Fuel-economy/MPG or MPGe calculations — none exist in the codebase today; this change only makes the underlying data capable of supporting them later.
- Converting unrelated existing int-stored enums (`HullType`, `DriveType`, etc.) to string storage — out of scope, a separate follow-up if desired.
- A general "hybrid type" taxonomy (series/parallel/mild-hybrid) — the single `IsExternallyChargeable` boolean is deliberately the only distinction modeled, because it's the only one that changes system behavior (which engines are loggable fuel/energy targets).

## Decisions

### `EngineType` narrowed to `Ice | Electric`; `Hybrid` dropped as a stored value
A single `Engine` row cannot coherently carry both a cylinder count/displacement and "no cylinders, it's a motor" at once. Since `Engine` is already 0..N per `Asset`, a hybrid vehicle is simply an asset with two `Engine` rows. "Is this asset a hybrid" is computed from `Active` engines at read time (see below), never stored.

*Alternative considered*: keep `Hybrid` as a value and let a single Engine row optionally carry both ICE and electric spec fields. Rejected — doubles the nullable-field surface on every `Engine` row for a case handled more cleanly by the entity's existing multiplicity.

### New nullable `Mechanism` enum, flat, ICE-only, deliberately incomplete
`Mechanism: TwoStroke | FourStroke | Diesel | Rotary`. This flattens ignition-type and stroke-count together (colloquial "diesel engine" implies compression ignition, typically 4-stroke) rather than modeling them as fully independent axes. Known gap: 2-stroke marine diesels don't fit cleanly (you'd pick `Diesel` and lose the stroke-count fact, or vice versa) — accepted, since no target asset category (vehicles, trailers, consumer equipment) is likely to hit this.

Aircraft-only mechanisms (`GasTurbine`, `Ramjet`, `Scramjet`, `Rocket`) are **not** added as values in this change. They're cheap to append later (string-backed enum, additive migration) exactly when an Aircraft asset type is built — adding them now would just be permanent dropdown noise for a garage/vehicle tracker. A `GasTurbineType` sub-enum (`Turbojet`/`Turbofan`/`Turboprop`/`Turboshaft`) would nest under `Mechanism.GasTurbine` at that point, as a third tier that doesn't disturb `TwoStroke`/`FourStroke`/`Diesel`/`Rotary`.

*Alternative considered*: pre-seed the aircraft values now to "prove" extensibility. Rejected per the above — extensibility is a property of the shape (separate nullable enum, string storage), not of having unused values present.

### `FuelType` trimmed to `Gasoline | Diesel | Propane`, nullable, ICE-only
Drops `TwoStroke`/`FourStroke` (moved to `Mechanism`), `Electric` (redundant — `EngineType.Electric` already says this), and `None` (replaced by nullability — an `Electric` engine simply has `FuelType: null`). Value set intentionally matches only what current asset categories (vehicles, trailers, riding mowers, power washers, small engines) plausibly run on; `NaturalGas`/`Ethanol`/`Hydrogen`/aviation fuels are deferred for the same reason as `Mechanism`'s aircraft values.

### `IsExternallyChargeable: bool?` on `Engine`, meaningful only when `EngineType == Electric`
Distinguishes a plug-in-rechargeable motor/battery from one only ever charged internally (a conventional hybrid's motor/generator via regen braking or the ICE). This one flag does three jobs: (1) determines the derived Hybrid vs. Plug-in Hybrid label, (2) determines which `Electric` engines are valid `FuelLog` targets (a non-plug-in hybrid's motor is never a real-world "refueling event" and must never appear as a loggable target), (3) keeps the model to a single boolean rather than a broader hybrid-type taxonomy that isn't needed to drive any actual behavior.

### Derived (non-persisted) powertrain summary, computed from `Active` engines only
```
Ice only                                      → no badge (not a hybrid)
Electric only                                 → "Electric"
Ice + Electric (IsExternallyChargeable=false) → "Hybrid"
Ice + Electric (IsExternallyChargeable=true)  → "Plug-in Hybrid"
```
Must filter to `EngineStatus == Active` so a retired/swapped engine (e.g. an ICE-to-EV conversion) doesn't leave a stale hybrid badge. Computed at the API layer when building `AssetResponse` (or the list projection used for asset cards/dashboard), not stored — avoids a cache-invalidation problem every time an engine's status or `IsExternallyChargeable` changes.

*Alternative considered*: store `IsHybrid`/`PowertrainType` directly on `Asset`, updated whenever engines change. Rejected — adds a synchronization obligation (every engine create/update/retire/reactivate/delete must remember to recompute it) for a value cheap to compute on read given the household's small per-asset engine counts.

### `FuelLog.Volume`/`VolumeUnit` generalized to `Quantity`/`Unit` (`Gallons | Liters | Kwh`)
Same fields (date, cost, miles/hours-at-log, notes) apply whether the asset took on gasoline, diesel, or kWh — only the unit changes. `FuelGrade` (already free text) continues to do double duty for octane rating or charge level (e.g. "89", "Level 2 AC") — no new enum, consistent with the reference-only string treatment given to 2-stroke oil type/ratio.

*Alternative considered*: a parallel `ChargeLog` entity mirroring `FuelLog`. Rejected — a hybrid or PHEV's usage history would be split across two tables for what is functionally one "energy went into this asset" timeline, complicating any future cost/economy rollups across both.

### `FuelLog.EngineId` selection logic
"Loggable engines" for an asset = `Active` `Ice` engines + `Active` `Electric` engines where `IsExternallyChargeable == true`.
- 0 loggable engines → today's behavior: free unit choice, `EngineId` stays `null` (assets without modeled engines).
- 1 loggable engine → auto-selected silently; no engine picker shown. This covers single-engine vehicles, pure EVs, *and* conventional (non-plug-in) hybrids, since the non-chargeable electric motor is excluded from the set.
- 2+ loggable engines → the user must pick (the PHEV case: gas pump vs. plug).

This logic lives in the frontend (`FuelLogsPage.tsx`, given the asset's already-fetched engine list) rather than the backend, since it's a UI-selection convenience; the backend continues to just validate that a submitted `engineId`, if present, belongs to the asset (existing behavior).

### 2-stroke oil fields
`TwoStrokeOilDelivery: Premix | OilInjected` and `TwoStrokeMixRatio: string?` (e.g. `"50:1"`), both meaningful only when `Mechanism == TwoStroke`. The existing `RecommendedOilType` string field is reused (not duplicated) for 2-stroke oil brand/spec — the frontend simply relabels it "Recommended 2-stroke oil" vs. "Recommended engine oil" based on `Mechanism`, since no single engine is ever both 2-stroke and 4-stroke. `RecommendedOctane`'s existing relevance is gated by `FuelType == Gasoline`, not by `Mechanism` — no schema change there, just a UI/validation relationship to document.

### Enum storage: string-backed, not int ordinals
`EngineType`, `FuelType`, `Mechanism`, and `TwoStrokeOilDelivery` get `HasConversion<string>()` in their EF configurations. Ordinal (default int) storage makes declaration order load-bearing for existing rows — inserting a value anywhere but the end silently remaps history. String storage is safe to extend in any order and is human-readable when inspecting the database directly. This is a repo-wide convention going forward (not retrofitted onto unrelated existing enums in this change).

### Migration approach: reset, not incremental
Per the existing `domain-model` spec precedent ("Initial EF Core migration" requirement), the `Infrastructure` project's migrations are deleted and a single `InitialCreate` regenerated reflecting the new schema. No backfill logic is needed since there's no production data.

## Risks / Trade-offs

- **[Risk]** Flattening `Mechanism` loses the ability to represent a 2-stroke diesel (e.g. large marine engines). → **Mitigation**: accepted gap, documented; no current asset category needs it. If it ever does, `Mechanism` can gain a compound representation without touching `FuelType` or `EngineType`.
- **[Risk]** Computing the powertrain badge on every asset read (rather than storing it) adds a join/computation over `Engines` for list views. → **Mitigation**: household engine counts are small (single-digit per asset); acceptable for a self-hosted app at this scale. Revisit with a stored/cached field only if a real performance problem shows up.
- **[Risk]** `EngineId` auto-selection logic living in the frontend means a user calling the API directly (or a future mobile client) doesn't get the same "auto-select the one loggable engine" convenience. → **Mitigation**: acceptable — the backend still accepts an explicit `engineId` or `null`; the convenience is a UI nicety, not a data-integrity requirement.
- **[Risk]** This change is breaking (enum values removed/renamed, `Volume`/`VolumeUnit` renamed) with no migration path for existing rows. → **Mitigation**: pre-launch, no production data; explicitly acceptable per project convention.

## Migration Plan

1. Update `Engine` and `FuelLog` domain entities and new enums.
2. Update `EngineConfiguration`/`FuelLogConfiguration` (string-backed enum conversions, any new column types).
3. Delete existing migrations; regenerate a single `InitialCreate` against the updated model.
4. Update `Steward.Application` DTOs/validators for `Engines` and `Tracking/FuelLogs`.
5. Update controllers/response shapes as needed (including the derived asset powertrain field).
6. Update frontend: `EnginesSection.tsx`, `FuelLogsPage.tsx`, wizard `EngineStep.tsx`, asset detail/list views for the powertrain badge, engine status-transition UI.
7. Regenerate `api/schema.d.ts` via `npm run generate:api`.
8. Run `dotnet test` (unit + integration) and frontend `npm test`/`npm run lint`.

No rollback strategy beyond standard git revert is needed — no production deployment exists yet.

## Open Questions

- None outstanding — all design points were resolved during the preceding explore session.
