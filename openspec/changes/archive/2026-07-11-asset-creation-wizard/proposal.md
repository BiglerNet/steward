# Proposal: asset-creation-wizard

## Why

Creating an asset today means filling a single dense dialog where every field is manual — even though most of a vehicle's details (make, model, year, engine specs) can be decoded from its VIN, and the natural follow-ups (add an engine, add photos) are separate chores the user has to discover afterwards. A guided flow makes first-run asset entry faster and produces more complete records.

## What Changes

- Replace the **create** path of the asset dialog with a full-page wizard at `/households/:householdId/assets/new`: **Type → VIN → Details → Engine → Photos**. The existing dialog remains for **edit** only.
- Type step: category cards grouped by registry `group`, reusing registry labels/icons.
- VIN step: shown only when the selected category's registry `vinDecodeSupport != None`; user may enter a VIN and decode it, or skip. Decode is optional and never blocking — failures, timeouts, or partial results just fall through to manual entry.
- New backend endpoint proxying the NHTSA vPIC VIN-decode API (avoids CORS and keeps the third-party dependency server-side), returning a normalized subset of decoded fields.
- Details step: the existing registry-driven type-specific fields, prefilled from the decode result. A decoded body class that doesn't match the chosen category surfaces a soft hint only — never an error.
- Engine step: shown when the registry says `typicallyHasEngine`; prefilled from decoded engine data (fuel type, cylinders, displacement); skippable.
- Photos step: skippable; asset (and optional engine) are created when the user advances past the Engine/Details step, so photo uploads target the real asset via the existing asset-photos endpoints. Finishing (or skipping photos) lands on the new asset's detail page.
- New external dependency: NHTSA vPIC public API (server-side HTTP call only, no key required).

## Capabilities

### New Capabilities

- `vin-decode`: backend proxy endpoint that decodes a VIN via NHTSA vPIC and returns a normalized result (make, model, year, body class, engine fields).
- `frontend-asset-creation-wizard`: the guided full-page asset creation flow (steps, registry gating, decode prefill, creation timing, photos hand-off).

### Modified Capabilities

- `frontend-asset-management`: the "Type-adaptive asset create/edit form" requirement narrows to edit-only; create controls (asset list button, empty state) navigate to the wizard route instead of opening the dialog.

## Impact

- **Backend**: new `VinDecode` feature slice — Application interface + DTO, Infrastructure implementation using `HttpClient` against vPIC (registered via an `Add*` extension), thin Api controller. No domain or database changes; no migration.
- **Frontend**: new wizard route/page + step components under `pages/assets` / `components/assets`; `AssetFormDialog` trimmed to edit-only; asset list/empty-state create actions become links; new `api/vinDecode.ts` + hook; regenerated `schema.d.ts`.
- **No changes** to the asset-types registry contract, asset CRUD endpoints, engine endpoints, or asset-photos endpoints — the wizard composes them as-is.
