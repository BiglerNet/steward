## Why

`frontend-foundation` gave the app routing, auth, and a household-scoped shell, but there's still no way to actually see or manage the things a household tracks. The backend already supports 10 asset types (with type-specific fields), engines nested under assets, and four tracking-record logs (service records, mileage, engine hours, fuel) — all household/asset-scoped and role-gated. This change builds the UI for all of that: list/create/edit/delete assets and engines, and log/list/edit/delete entries in each of the four tracking logs.

## What Changes

- Add an asset list page under `/households/:householdId/assets`, filterable by asset type, each card/row showing name, type, year, and a thumbnail (`photoUrl` if present).
- Add a create/edit asset form that adapts its fields to the selected `AssetType` (e.g. VIN/make/model for Car/Truck, HIN/hull material/length/beam for Boat, track length for Snowmobile, ball size for Utv, max load/interior dimensions for trailers, cutting width for RidingMower, max PSI/GPM for PowerWasher, equipment description for SmallEngine), reusing the single flat `CreateAssetRequest`/`UpdateAssetRequest` shape from the API.
- Add an asset detail page showing the asset's fields, its engines, and tabs/sections for each of the four tracking logs.
- Add engine list/create/edit/delete UI nested under an asset's detail page.
- Add a tracking-log UI pattern (list ordered newest-first, create form, edit, delete) implemented once and reused across the four log types: Service Records, Mileage Logs, Engine Hours Logs, Fuel Logs — each scoped to an asset (and optionally an engine for hours/service).
- Apply role-based UI gating consistent with the backend: Viewers see read-only views; Contributors/Owners see create/edit/delete controls for tracking logs; only Owners see delete for assets/engines (matching the backend's structural-resource-delete rule).
- Add empty states (no assets yet, no log entries yet) and asset-type icons/labels for the switcher-adjacent navigation.

## Capabilities

### New Capabilities
- `frontend-asset-management`: Asset list, create/edit (type-adaptive form), detail view, delete; engine list/create/edit/delete nested under an asset.
- `frontend-tracking-records`: List/create/edit/delete UI for service records, mileage logs, engine hours logs, and fuel logs, scoped to an asset.

### Modified Capabilities
- (none)

## Impact

- **Web**: New `src/pages/assets/` and nested routes under `/households/:householdId/assets/:assetId/...`; new `src/api/assets.ts`, `src/api/engines.ts`, `src/api/tracking.ts` typed clients; a shared `TrackingLogTable`/`TrackingLogForm` pattern reused across the four log types rather than four separate implementations; new shadcn components as needed (`select`, `tabs`, `table`) for the type-adaptive form and the asset detail tabs.
- **No backend changes** — consumes the already-built `assets`, `engine-management`, `service-record-tracking`, `mileage-tracking`, `engine-hours-tracking`, and `fuel-tracking` capabilities.
- **Out of scope for this change**: Registration and Warranty UI, and document upload/download — deferred to a following `frontend-documents` change, since those need their own upload/preview UI pattern.
