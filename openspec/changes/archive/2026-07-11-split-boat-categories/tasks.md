# Tasks: split-boat-categories

> Prerequisite: `asset-ux-polish` applied and archived (registry `icon` contract).

## 1. Domain

- [x] 1.1 `AssetCategory`: remove `Boat`, add `PowerBoat` + `Sailboat`; new enums `HullType` (Monohull | Catamaran | Trimaran | Pontoon | Other) and `DriveType` (Inboard | Outboard | SternDrive | JetDrive)
- [x] 1.2 `Boat` entity: add `HullType?`, `DriveType?`, `KeelType` (string?), `MastHeightFt` (decimal?), `MastCount` (int?)

## 2. Registry

- [x] 2.1 Replace the `Boat` entry with `PowerBoat` (Water, Boat structural, `typicallyHasEngine: true`, `Both`, icon `ship`, fields + `hullType`/`driveType`) and `Sailboat` (Water, Boat structural, `typicallyHasEngine: false`, `Hours`, icon `sailboat`, fields + `hullType`/`keelType`/`mastHeightFt`/`mastCount`); Pwc unchanged
- [x] 2.2 Registry unit tests green (completeness, structural-field consistency for the new fields)

## 3. Application + Api

- [x] 3.1 Asset DTOs/requests/responses: add the five new fields; `AssetMapper` maps them for Boat-structural assets
- [x] 3.2 Confirm registry-driven inapplicable-field validation covers the new fields (e.g. `driveType` on a Sailboat rejected); add validator tests
- [x] 3.3 Integration tests: create/read a Sailboat with rig fields and a PowerBoat with drive type; `Boat` category gone from the contract

## 4. Migration reset

- [x] 4.1 Delete `Migrations/`, regenerate `InitialCreate` with the new Assets columns; apply to a clean DB; full backend suite green

## 5. Frontend

- [x] 5.1 Regenerate `schema.d.ts`; update `api/types.ts` and fixtures (PowerBoat/Sailboat entries, new fields, enum types)
- [x] 5.2 `AssetFieldsSection`: select field kind for `hullType`/`driveType` with readable option labels (fuel-type select pattern); text/number inputs for `keelType`/`mastHeightFt`/`mastCount`
- [x] 5.3 Component tests: Sailboat form renders exactly the registry fields with selects for enums; PowerBoat shows drive type; Pwc unchanged
- [x] 5.4 Sweep remaining `Boat` category references (labels, filters, tests)

## 6. Verification

- [x] 6.1 `dotnet build` + `dotnet test`, `npm test`, `tsc -b`, lint, `vite build` all green
- [x] 6.2 Smoke: wizard end-to-end for a Sailboat (no VIN/Engine step, rig fields present) and a PowerBoat (engine step present, drive-type select)
