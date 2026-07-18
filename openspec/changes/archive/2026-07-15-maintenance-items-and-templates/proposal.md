## Why

`ServiceRecord` today can only represent a single completed, historical fact — a flat row logged after the work is done. That model can't express work that has a lifecycle (planned → in progress → done), can't hold a checklist for anything beyond a one-line job, can't track parts being ordered before the work happens, and can't be created from a reusable template. Users need to plan and track multi-step maintenance work (a full rebuild, a seasonal winterization touching multiple engines) with the same tool they use to log a five-minute oil change, without the simple case getting slower or more complex.

## What Changes

- **BREAKING**: `ServiceRecord` is removed and replaced by `MaintenanceItem`, a richer entity with a `Planned | InProgress | Done | Cancelled` lifecycle. Creating an item and immediately setting it to `Done` remains a single action — the lifecycle is available, never required.
- New `ChecklistItem` child entity (`Open | Done | Skipped` tri-state, reorderable, optionally scoped to a specific engine on the asset) for tracking sub-steps within a `MaintenanceItem`.
- New `PartLine` child entity for tracking parts needed/ordered/received for a `MaintenanceItem`, plus a new `Part` catalog table reserved as a schema seam for a future parts-inventory feature (no inventory behavior in this change — the table exists, nothing populates or reads it yet beyond the optional link).
- New `Template`/`TemplateStep` entities: reusable, ordered checklists (with optional per-step engine-scoping and recurrence interval) that can be applied when creating a `MaintenanceItem`, auto-expanding engine-scoped steps into one row per active engine and prefilling suggested parts. Templates are household-owned (Contributor/Owner edit, Viewer view) or platform-owned (`HouseholdId = null`, PlatformAdmin-only edit, readable by anyone, with a "duplicate to my household" action). A small built-in library of common templates (oil change, tire rotation, etc.) ships as seeded platform templates, each optionally tagged with which asset categories it applies to (e.g. an oil-change template can target Car, Truck, and PowerBoat all at once).
- New full-page, autosaving editor for `MaintenanceItem` (its own route, not a dialog) — a small dialog handles only the initial quick-create (title + optional template), then redirects to the full page for everything else (description, checklist, parts, engine, status). The asset detail page keeps an embedded list of the asset's maintenance items (replacing the service-records tab).
- New minimal admin console (`/admin`, gated to `PlatformAdmin`, linked from the top nav only for admins) hosting platform template management, built as a shell that future admin features can add sections to.
- `RecentActivity` dashboard widget is repointed from `ServiceRecord` to `Done` `MaintenanceItem` entries (same shape, different source).

## Capabilities

### New Capabilities
- `maintenance-items`: backend CRUD and granular update endpoints for `MaintenanceItem`, `ChecklistItem` (including reorder), and `PartLine`.
- `maintenance-templates`: backend CRUD for `Template`/`TemplateStep`, household/platform-scoped authorization, "duplicate to household," and the template-expansion logic used when creating a `MaintenanceItem` from a template.
- `frontend-maintenance-items`: the asset-page maintenance list, quick-create dialog, and full-page autosaving editor (checklist UI with drag-reorder + context-menu fallback, parts list UI, Done-transition confirmation for open checklist items).
- `frontend-maintenance-templates`: household template list/CRUD/duplicate UI, and the template picker in the quick-create dialog.
- `frontend-platform-admin`: the `/admin` shell, `PlatformAdminRoute` guard, conditional nav link, and the platform template management screen.

### Modified Capabilities
- `domain-model`: remove the `ServiceRecord` entity from "Cross-cutting tracking entities" and add `MaintenanceItem`, `ChecklistItem`, `PartLine`, and `Part` in its place; add a new requirement for the `Template`/`TemplateStep` shape.
- `service-record-tracking`: entire capability removed — all endpoints superseded by `maintenance-items`.
- `frontend-tracking-records`: service records removed from the set of tracking-log types listed/created/edited/deleted via the generic tracking-log UI (mileage, engine hours, and fuel logs are unaffected and keep working exactly as today).
- `dashboard-widgets`: `RecentActivity` now sources from `Done` `MaintenanceItem` entries instead of `ServiceRecord`.

## Impact

- **Backend**: new `MaintenanceItem`, `ChecklistItem`, `PartLine`, `Part`, `Template`, `TemplateStep` entities/tables; `ServiceRecords` table and its controller/service/DTOs removed; `RecentActivity` widget query repointed; new `PlatformAdmin`-branching authorization path for platform-owned templates (the first resource in the app with no household to check membership against). Migration reset (pre-launch, no data to preserve).
- **Frontend**: new routes (`/households/:householdId/assets/:assetId/maintenance/:itemId`, `/households/:householdId/templates`, `/admin`, `/admin/templates`); the service-records tab on the asset page is replaced by a maintenance-items tab; new granular (autosave-oriented) API calls replace the single-`PUT` pattern used by other tracking logs; depends on the `MarkdownEditor` component from the `shared-markdown-editor` change for `MaintenanceItem.description`.
- **No inventory behavior**: `Part.QuantityOnHand` and stock consumption are explicitly out of scope — only the catalog table and `PartLine.partId` seam are added.
