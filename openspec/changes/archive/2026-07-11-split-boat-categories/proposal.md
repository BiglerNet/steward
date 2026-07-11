# Proposal: split-boat-categories

## Why

A single `Boat` category can't capture the powered-vs-sail distinction, so the fields that matter most to each — drive type for powerboats; rig, keel, and hull configuration for sailboats — have no home. Splitting the category gives each kind its proper type-specific fields through the existing registry machinery.

## What Changes

- **BREAKING** (enum + API contract): `AssetCategory.Boat` is removed, replaced by `PowerBoat` and `Sailboat`. `Pwc` is unchanged. Pre-launch: migrations reset to a fresh `InitialCreate`, no data migration.
- The `Boat` structural entity gains nullable fields: `HullType` (enum: Monohull | Catamaran | Trimaran | Pontoon | Other), `DriveType` (enum: Inboard | Outboard | SternDrive | JetDrive), `KeelType` (free text), `MastHeightFt` (decimal), `MastCount` (int).
- Registry entries: `PowerBoat` (adds `hullType`, `driveType` to the boat fields) and `Sailboat` (adds `hullType`, `keelType`, `mastHeightFt`, `mastCount`); both Water group, structural type Boat. `Pwc`'s field list is unchanged.
- Frontend: the registry-driven asset form gains select-type inputs for the two enum-backed fields; everything else (wizard, list, filters) picks the new categories up from the registry automatically.
- Depends on `asset-ux-polish` being applied and archived first — registry entries here include `icon` names and the spec deltas are written against the post-polish spec text.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `domain-model`: Boat structural fields, `AssetCategory` value list (Boat → PowerBoat + Sailboat), new `HullType`/`DriveType` enums.
- `frontend-asset-management`: registry-consumption requirement's field-applicability example updates from `Boat` to the new categories, and enum-backed type-specific fields render as selects.

## Impact

- **Backend**: `AssetCategory`, new `HullType`/`DriveType` enums, `Boat` entity, `AssetTypeRegistry` entries, asset DTOs/validators/mapper, migration reset. Existing enum conventions apply (stored as int, serialized as string).
- **Frontend**: regenerated `schema.d.ts`, `AssetFieldsSection` select support for enum-backed fields, fixtures/tests, wizard picks up the categories via the registry.
- **No changes** to endpoints, authorization, tracking records, or the asset-type registry *contract* (entries are data).
