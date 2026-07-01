## Context

The backend models all assets as a single `Asset` entity with a flat set of nullable type-specific fields (`Vin`/`Make`/`Model` for vehicles, `Hin`/`HullMaterial`/`LengthFt`/`BeamFt` for boats, etc.) discriminated by an `AssetType` enum, plus a separate `Engine` entity nested under an asset, and four independent append-only tracking-record logs (`ServiceRecord`, `MileageLog`, `EngineHoursLog`, `FuelLog`) scoped to an asset. `frontend-foundation` already gives us household-scoped routing, the `AuthContext`, the household switcher, and the global toast/error pattern this change builds directly on top of. `HouseholdResponse` (already fetched by `useHouseholds`) carries a `userRole` field per household, which is the source of truth for role-based UI gating here.

## Goals / Non-Goals

**Goals:**
- Asset CRUD UI that adapts its form fields to the 10 `AssetType` values without 10 separate form components.
- Engine CRUD UI nested under an asset.
- One reusable tracking-log list/create/edit/delete UI pattern, instantiated for all four log types (service records, mileage, engine hours, fuel) rather than four bespoke implementations.
- Role-based UI gating matching the backend exactly: Viewer = read-only; Contributor/Owner = create/edit/delete tracking logs; Owner-only = delete assets/engines.

**Non-Goals:**
- Registration/Warranty UI and document upload/download — deferred to `frontend-documents`, since file upload needs its own UI pattern (progress, preview, download) distinct from a plain CRUD form.
- Asset photo *upload* — `Asset.PhotoUrl` is a plain string field with no backend upload endpoint; this change treats it as a pasted-URL text field, not a file upload. Wiring it through `IFileStorageService` would be a backend change, out of scope here.
- Cross-household visibility (public garage view) — separate future change.

## Decisions

### 1. One type-adaptive asset form, not ten per-type forms
The create/edit asset form always shows the shared base fields (name, description, year, photo URL, usage tracking mode) plus an `AssetType` select; choosing a type reveals only that type's field group (e.g. selecting `Boat` reveals HIN/hull material/length/beam, hides VIN/make/model). Field groups are driven by a static `assetTypeFieldConfig` lookup (type → field list + labels + Zod schema fragment), not per-type React components.
**Alternative considered**: a separate form component per `AssetType` — rejected, the underlying request DTO is already one flat shape on the backend; ten components would duplicate the base-field logic tenfold for no behavioral difference.

### 2. One generic tracking-log component, configured per log type
A single `TrackingLogSection<TRecord>` component (list table + create/edit dialog) takes a config object per log type: columns, the Zod schema, the typed API functions (`list`/`create`/`update`/`delete`), and whether an engine selector applies (Service Records and Engine Hours Logs can reference an `engineId`; Mileage and Fuel logs do not). The asset detail page renders four instances, one per log type, as tabs.
**Alternative considered**: four independent page/component implementations — rejected as direct copy-paste of list/create/edit/delete with only field and column differences; a shared component is mechanical and keeps a single place to fix bugs (e.g. pagination, sort order) across all four logs.

### 3. Role-based UI gating via a `useHouseholdRole()` hook reading `HouseholdResponse.userRole`
A small hook resolves the active household's role from the already-fetched households list and exposes `canEdit`/`canDeleteStructural` booleans (`canEdit` = Contributor or Owner; `canDeleteStructural` = Owner only). Components consult this hook to hide/disable controls; the API call remains the authority (a stale role still gets a `403`, handled by the existing global toast pattern from `frontend-shell`).
**Alternative considered**: re-deriving role from a fresh `/me`-style call per page — rejected, `HouseholdResponse.userRole` is already fetched by the switcher/shell and re-fetching it per page would just be redundant network calls for data already in the TanStack Query cache.

### 4. Asset detail tracking logs are routed, not just tabbed-in-place
Each log type gets its own URL segment (`/assets/:assetId/service-records`, `/assets/:assetId/mileage-logs`, etc.) rendered via nested routes, with a tab-styled nav between them — not client-only tab state.
**Alternative considered**: a single `/assets/:assetId` page with in-memory tab state — rejected, it breaks deep-linking (can't link directly to "this asset's fuel log") and loses the active tab on refresh, the same reasoning `frontend-foundation` applied to household scoping.

## Risks / Trade-offs

- **[Risk]** The generic `TrackingLogSection` config object could become an awkward abstraction if a fifth log type later needs a meaningfully different UI shape — **Mitigation**: keep the config surface small (columns, schema, API functions, optional engine selector); if a future log type doesn't fit, build it standalone rather than forcing the shared component to grow new conditional branches.
- **[Risk]** Type-adaptive asset form means switching `AssetType` mid-edit could leave stale type-specific values in unrelated fields (e.g. switching from Boat to Car after entering HIN) — **Mitigation**: clear type-specific fields not in the newly selected type's field group when the `AssetType` select changes.

## Migration Plan

No backend/database changes. Purely additive frontend work.

## Open Questions

None.
