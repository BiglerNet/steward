## 1. Shared Foundations

- [x] 1.1 Add `src/lib/permissions.ts` / `useHouseholdRole()` hook deriving `canEdit`/`canDeleteStructural` from the active household's `userRole` (already available via `useHouseholds`).
- [x] 1.2 Add `assetTypeFieldConfig` lookup (per `AssetType`: field list, labels, Zod schema fragment) used by the asset form.
- [x] 1.3 Add shadcn `select`, `tabs`, and `table` components if not already present.

## 2. Asset API Clients

- [x] 2.1 Add `src/api/assets.ts`: typed `list`/`get`/`create`/`update`/`delete` using generated `schema.d.ts` types.
- [x] 2.2 Add `src/api/engines.ts`: typed `list`/`create`/`update`/`delete` scoped to an asset.
- [x] 2.3 Add `src/api/tracking.ts`: typed `list`/`create`/`update`/`delete` for service records, mileage logs, engine hours logs, and fuel logs.

## 3. Asset Management UI

- [x] 3.1 Add asset list page (`/households/:householdId/assets`) with type filter and empty state.
- [x] 3.2 Add type-adaptive create/edit asset form (dialog or dedicated route), using `assetTypeFieldConfig`, clearing non-applicable fields on type change.
- [x] 3.3 Add asset detail page shell (`/households/:householdId/assets/:assetId`) showing base fields + type-specific fields, with tab navigation to engines and the four tracking logs.
- [x] 3.4 Add delete-asset control, gated to Owner role via `useHouseholdRole()`.

## 4. Engine Management UI

- [x] 4.1 Add engine list section on the asset detail page.
- [x] 4.2 Add create/edit engine form.
- [x] 4.3 Add delete-engine control, gated to Owner role.

## 5. Generic Tracking-Log Component

- [x] 5.1 Build `TrackingLogSection<TRecord>`: list table (newest-first) + create/edit dialog + delete confirm, configured via columns/schema/API-functions/optional-engine-selector.
- [x] 5.2 Instantiate for Service Records (`/assets/:assetId/service-records`), including the engine selector.
- [x] 5.3 Instantiate for Engine Hours Logs (`/assets/:assetId/engine-hours-logs`), including the engine selector.
- [x] 5.4 Instantiate for Mileage Logs (`/assets/:assetId/mileage-logs`), no engine selector.
- [x] 5.5 Instantiate for Fuel Logs (`/assets/:assetId/fuel-logs`), no engine selector.
- [x] 5.6 Gate create/edit/delete controls to Contributor/Owner via `useHouseholdRole()` (`canEdit`, not `canDeleteStructural` — tracking-log delete is Contributor-allowed).

## 6. Tests

- [x] 6.1 Asset form tests: field-group switching per `AssetType`, field clearing on type change, validation errors.
- [x] 6.2 Asset list/detail tests: filtering, empty state, role-gated delete control visibility.
- [x] 6.3 Engine CRUD tests: create/edit happy path, role-gated delete visibility.
- [x] 6.4 `TrackingLogSection` tests (one instantiation, e.g. Service Records): list ordering, create, edit, delete, role-gated controls (Viewer sees none, Contributor sees create/edit/delete).
- [x] 6.5 Engine-selector tests: present for Service Records/Engine Hours Logs, absent for Mileage/Fuel Logs.

## 7. Manual Verification

- [x] 7.1 Against the local Docker Compose stack: create one asset of each of the 10 types, confirm only the relevant fields are editable per type.
- [x] 7.2 Add an engine to a Car, log a service record against that engine, confirm the engine association round-trips.
- [x] 7.3 As a Viewer-role test account, confirm no create/edit/delete controls render anywhere in this change's UI, and that the underlying API still returns `403` if attempted directly (defense in depth).
