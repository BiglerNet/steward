# Design: split-boat-categories

## Context

The TPH hierarchy expresses user-facing taxonomy through `AssetCategory` + registry entries, not subclasses — a new boat kind is an enum value and a registry row, no new entity class. `Boat` (the structural class) already holds hin/hullMaterial/lengthFt/beamFt/make/model/color as nullable columns shared by `Boat` and `Pwc` categories. The product is pre-launch; enum renumbering and migration resets are free. This change assumes `asset-ux-polish` has landed (registry serves `icon`, not `iconColor`).

## Goals / Non-Goals

**Goals**
- Distinct PowerBoat and Sailboat categories with the fields each actually needs.
- Zero new entity classes — reuse the `Boat` structural class per the hierarchy's design rule.

**Non-Goals**
- No Pwc changes beyond keeping its existing field list.
- No sailboat-specific tracking semantics (sail hours vs engine hours stay as-is via `UsageTrackingMode`).
- No HIN decode or other lookup service.

## Decisions

### D1: Split at the category level, fields on the shared structural class

`PowerBoat` and `Sailboat` replace `Boat` as `AssetCategory` values; both map to the `Boat` structural class. New nullable columns land on that class and the registry's `applicableFields` gates which category sees which — exactly how `licensePlate` works on `Vehicle`/`Trailer`. Field lists:

- **PowerBoat**: `hin`, `hullMaterial`, `hullType`, `driveType`, `lengthFt`, `beamFt`, `make`, `model`, `color`
- **Sailboat**: `hin`, `hullMaterial`, `hullType`, `keelType`, `mastHeightFt`, `mastCount`, `lengthFt`, `beamFt`, `make`, `model`, `color`
- **Pwc**: unchanged

The existing inapplicable-field validation (registry-driven, rejects non-null values for fields a category doesn't declare) covers the new fields with no new validator logic.

### D2: Closed sets are enums, open sets are text

`DriveType` (Inboard | Outboard | SternDrive | JetDrive) and `HullType` (Monohull | Catamaran | Trimaran | Pontoon | Other) are small, stable, closed sets → domain enums, stored as int and serialized as string per repo convention, giving the frontend proper selects and future filtering. `KeelType` stays free text like `hullMaterial` — keel taxonomy is long-tailed (fin, full, wing, bulb, swing, centerboard, dagger, bilge…) and forcing an enum would mostly produce `Other`. `MastHeightFt` is decimal, `MastCount` int.

### D3: Registry data for the new entries

Both Water group, `structuralType: Boat`, `vinDecodeSupport: None`, `typicalPermitKinds: [Registration]`. `PowerBoat`: `typicallyHasEngine: true`, `defaultUsageTrackingMode: Both`, icon `"ship"`. `Sailboat`: `typicallyHasEngine: false` (auxiliary engines are common on larger boats but not "typical" across the category; the wizard stays short and engines remain addable from asset detail), `defaultUsageTrackingMode: Hours`, icon `"sailboat"`. These are data defaults, trivially adjustable.

### D4: Frontend selects for enum-backed type-specific fields

`AssetFieldsSection` currently renders text/number inputs from `applicableFields`. It gains a select field kind with frontend-owned option lists for `hullType` and `driveType` (mirroring how the engine form's `fuelType` select works — the generated schema types provide the enum members). Display labels get human spacing ("Stern drive (I/O)", "Jet drive").

### D5: Breaking rollout, reset migration

`Boat` disappears from the enum, DTOs, and registry in one commit; migrations reset to a fresh `InitialCreate` including the new Assets columns. Frontend and backend ship together (regenerated schema). Any seeded/dev data with `Boat` is simply recreated.

## Risks / Trade-offs

- **[Sailboat owners with auxiliary engines miss the wizard Engine step]** → engines are addable from asset detail; `typicallyHasEngine` is registry data and can flip if feedback says otherwise.
- **[Enum-backed form fields add a field-kind dimension to AssetFieldsSection]** → contained: one select kind with per-field option lists, reusing the existing fuel-type select pattern.
- **[Existing dev data references Boat]** → pre-launch reset convention already covers this; no migration path needed.

## Migration Plan

Apply after `asset-ux-polish` is archived (this change's registry entries and spec deltas assume the `icon` contract). Single deploy of API + frontend; DB recreated from the reset `InitialCreate`.

## Open Questions

None.
