## Context

`ServiceRecord` (see `openspec/specs/service-record-tracking/spec.md` and the "Cross-cutting tracking entities" requirement in `openspec/specs/domain-model/spec.md`) is a flat, retrospective log: `Date`, `Description`, `ProviderName`, `Cost`, `OdometerMiles`, `EngineHours`, `AssetId`, optional `EngineId`, `Notes`. It's created and edited through the generic `TrackingLogSection` dialog component shared with `MileageLog`/`EngineHoursLog`/`FuelLog`, and it's one of the sources feeding the dashboard's `RecentActivity` widget.

This change replaces it with a richer entity family that supports planned/in-progress work, checklists, parts tracking, and reusable templates, while explicitly preserving the "just log it" one-step flow for the common case. This is the product of an extended design exploration (see conversation history / prior session); the shapes below are the settled result, not a first draft. It's a large change and deliberately scoped to *not* include recurrence computation or the kanban board — those land in a follow-up change (`maintenance-recurrence-and-kanban`) once these entities exist and are stable.

## Goals / Non-Goals

**Goals:**
- Support both a five-minute oil change (create → immediately `Done`, one motion) and a multi-week project (checklist, parts, status transitions) with the same entity, not two parallel systems.
- Make templates a genuine anchor for recurring work identity (a stable `TemplateStepId` other work — the recurrence follow-up change — can key off), not just an autofill convenience.
- Reserve the instance-vs-catalog seam for parts now, so a future inventory feature is additive.
- Keep the day-to-day editing experience autosaving and dialog-free for anything with real structure.

**Non-Goals:**
- No recurrence computation ("last done" / "next due"), no kanban board — both are the next change.
- No parts-inventory behavior (`QuantityOnHand`, stock consumption) — only the `Part` table shape.
- No multi-asset `MaintenanceItem` — stays single-`AssetId`-scoped like `ServiceRecord` today; work spanning multiple assets is multiple items sharing a date.
- No scheduler or background job of any kind.

## Decisions

### Decision: `Status` lifecycle with `Blocked` as a derived badge, not a stored value
`MaintenanceItem.Status`: `Planned | InProgress | Done | Cancelled`. There is no `Blocked` status — a UI badge is derived at read time from "has any `PartLine` still `Needed` or `Ordered`." Storing `Blocked` as a real status would require deciding what un-blocks it and which prior status to return to; deriving it avoids that entirely and can never drift out of sync with the parts data it's based on.

Alternative considered: explicit `Blocked` status with its own transitions. Rejected — combinatorial complexity (blocked-while-planned vs. blocked-while-in-progress) for a fact that's already fully determined by `PartLine` state.

### Decision: quick-log stays one API call
Creating a `MaintenanceItem` with `status: "Done"` directly in the create request is valid and requires nothing else — no forced `Planned`/`InProgress` intermediate steps. This is what preserves "I just changed the oil" as a single action.

### Decision: Done-transition confirmation is entirely frontend; the backend has no checklist-completeness gate
Moving `Status` to `Done` never requires all `ChecklistItem`s to be resolved — the API accepts it unconditionally. The three-option prompt ("Go back" / "Mark remaining as Skipped, then complete" / "Complete anyway") is client-side UX: "Mark remaining as Skipped" is the client issuing one `PATCH` per open `ChecklistItem` (setting `Status = Skipped`) followed by the `Status = Done` `PATCH` on the parent — no composite backend endpoint. This keeps the backend contract simple and keeps the checklist a helper, never a workflow gate.

### Decision: `ChecklistItem` tri-state (`Open | Done | Skipped`) with a single `ResolvedAt` timestamp
`Skipped` is distinct from `Done` specifically so recurrence (the follow-up change) can tell "didn't get to it" from "did it" — skipping a step must not look like completing it. One nullable `ResolvedAt` (set on entering either `Done` or `Skipped`, cleared on `Reopen`) covers both outcomes rather than two near-duplicate timestamp columns. Checklist items deliberately have no `Notes` or `Cost` field of their own — that granularity isn't needed and would duplicate data better kept on `MaintenanceItem`/`PartLine`.

### Decision: engine-scoping lives on `ChecklistItem`, not on `MaintenanceItem`
A single `MaintenanceItem` ("Winterize the boat") can have checklist rows that apply to different engines (main outboard vs. kicker) because engines genuinely have independent service histories even on the same asset — a nullable `EngineId` on `ChecklistItem` (must belong to the parent item's `AssetId`) captures this without needing a multi-engine `MaintenanceItem` concept. `MaintenanceItem` itself keeps a single nullable top-level `EngineId`, unchanged from `ServiceRecord`'s shape, for items that are inherently single-engine or asset-level.

### Decision: `TemplateStep` is the stable identity for recurrence, not free text matching
`ChecklistItem.TemplateStepId` (nullable) links an instantiated checklist row back to the template step it came from. This is what lets a later query ask "when was this specific step, on this specific engine, last done" across many applications of the same template over time — without it, every application of a template produces checklist rows with no relationship to each other. Ad hoc checklist items typed free-hand (not from a template) simply have `TemplateStepId = null` and aren't part of any recurrence rollup — that's expected, not a gap.

### Decision: one recurrence rule per step, not one per (step, engine) pair
`TemplateStep.RecurrenceIntervalMonths`/`RecurrenceIntervalMiles`/`RecurrenceIntervalHours` are single nullable values (due at whichever threshold is hit first). When a step is `EngineScoped`, the *same* rule is evaluated independently per engine using that engine's own history — divergent "last done" status between two engines (e.g. a kicker that's never used vs. a main outboard run every weekend) comes from divergent usage data, not from configuring two different interval numbers. This was a deliberate simplification after an earlier design considered per-engine custom intervals and found it added configuration surface without a real corresponding need.

### Decision: `SuggestedParts` on `TemplateStep` is a one-time copy, not a live link
Applying a template copies its steps' suggested part names/quantities into fresh `PartLine`s at `Status = Needed`. There is no persistent link back to the template for parts (unlike checklist items, which keep `TemplateStepId`) — a part is inherently instance data ("I bought *this* filter for *this* job"), and there's no meaningful "last time I ordered an oil filter" query the way there is for "last time this step was done."

### Decision: `Part` catalog table added now, with zero behavior
`Part` (`HouseholdId`, `Name`, `PartNumber`, `DefaultVendor`) is added as a table and domain entity in this change, but with **no service, no controller, no endpoints, and no UI** — purely a migration-level seam. `PartLine.PartId` (nullable FK) exists but nothing sets it yet; `PartLine` always keeps its own denormalized `Name`/`PartNumber`/`Vendor` regardless, so history reads correctly even after a future catalog entry is renamed or deleted. This was an explicit call to avoid a `PartLine` schema change when inventory tracking is eventually built.

### Decision: `Template.ApplicableCategories` as a Postgres array column, not a join table
A `List<AssetCategory>` mapped to a native Postgres array column (Npgsql supports this directly for enum-backed columns stored as strings) rather than a `TemplateApplicableCategory` join table — this list is small, read together as a whole, and never queried element-by-element on its own, so a join table would be pure overhead. An empty/null list means "applicable to any category."

### Decision: `TemplateStep.SuggestedParts` as a `TemplateStepSuggestedPart` child table, not `jsonb`
A small list of `{ name, quantity }` suggestions per step, backed by a proper child entity (`Id`, `TemplateStepId` FK, `Name`, `Quantity`, `SortOrder`) — the same shape as `ChecklistItem`/`PartLine` already use elsewhere in this change — rather than a `jsonb` column.

An earlier version of this decision went the other way (jsonb, reasoning that a sixth child table wasn't worth it for values that are only ever read/written as a whole). Implementing it surfaced why that reasoning didn't hold up: mapping a mutable list through `jsonb` isn't free in EF Core — it required a hand-written `HasConversion` (manual `System.Text.Json` serialize/deserialize) *and* a hand-rolled `ValueComparer`, because EF's change tracker can't otherwise detect in-place mutations to a converted collection. That's real, easy-to-get-subtly-wrong code, and it bought nothing: nothing in the app ever queries into the JSON with a `jsonb` operator, and it's the only `jsonb` column anywhere in this codebase — a one-off persistence pattern for a single field. It also had a concrete downstream cost: the resulting mapping couldn't be exercised under EF Core's InMemory test provider, forcing the expansion-logic and `isBlocked` tests into real-Postgres integration tests instead of the faster unit-test suite (see `tasks.md` §8.1/8.2's original rationale). A plain child table needs none of that — no converter, no comparer, works fine under InMemory — and it also makes a future "edit one suggested part" UI simple row-level CRUD instead of read-the-whole-array/mutate/write-back.

### Decision: platform templates need a new authorization branch
Every existing authorized resource in this app is either household-scoped (`HouseholdAuthorizationHandler` checks a `HouseholdMembership` row) or bypassed entirely by `PlatformAdmin` (user/role management). A platform template (`HouseholdId = null`) has no household to check membership against, so `Template` needs its own rule: when `HouseholdId` is `null`, mutations require the `PlatformAdmin` role; reads are open to any authenticated user (so households can browse and duplicate). When `HouseholdId` is set, the existing `IHouseholdResource`/`HouseholdAuthorizationHandler` pattern applies unchanged (Owner/Contributor edit, Viewer view, any Active member can use it to create a `MaintenanceItem`). Seeded built-in templates (oil change, tire rotation, etc.) are simply platform templates created by a startup/migration seed — one mechanism for both "ship sensible defaults" and "curated library," not two.

### Decision: one full-page route serves both create and edit
Unlike the earlier `asset-creation-wizard` (full page for create, a trimmed dialog for edit), `MaintenanceItem`'s edit surface is the *same* route for both — `/households/:householdId/assets/:assetId/maintenance/:itemId`. Reasoning: a `MaintenanceItem` is a living document revisited repeatedly across `Planned → InProgress → Done`, not a one-time setup flow the way asset creation is. A separate small dialog (title + optional template picker) handles the initial quick-create, then immediately redirects to this same page.

### Decision: autosave via granular endpoints, not one big `PUT`
Because the full-page editor autosaves field-by-field, the API surface is granular rather than the single-`PUT`-replaces-everything pattern `ServiceRecord`/`MileageLog`/etc. use today: independent `PATCH` for title/description/status/date/cost/odometer/hours/engine, and separate CRUD (+ reorder) endpoints for `ChecklistItem`s and `PartLine`s. This mirrors the existing dashboard widget-layout and engine-status-transition endpoints (small, targeted mutations) more than the generic tracking-log dialog pattern.

### Decision: checklist drag-reorder reuses the existing `WidgetGrid.tsx` pattern
Same `@dnd-kit/core`/`@dnd-kit/sortable` setup already used for dashboard widget reordering: `PointerSensor` + `KeyboardSensor`, a small `GripVertical` drag-handle icon (not the whole row), `touch-none` styling on the handle. A "Move up"/"Move down" pair is added to each checklist item's context menu as a fallback for touch precision and accessibility, since drag can be fiddly inside a scrollable dialog/list on a phone in a way the dashboard-editing screen isn't.

## Risks / Trade-offs

- **[Risk] Removing `ServiceRecord` is a breaking, non-additive change.** → Mitigation: acceptable per explicit product direction — pre-launch, no production data, migrations can be reset freely. `RecentActivity` is repointed in this same change so nothing silently breaks.
- **[Risk] Granular autosave endpoints are more API surface than the single-`PUT` pattern used elsewhere.** → Mitigation: accepted deliberately — it's what autosave requires; scoped tightly to `MaintenanceItem` and its children rather than retrofitted onto the other tracking-log types.
- **[Risk] Platform-template authorization is a genuinely new pattern with no existing precedent to copy exactly.** → Mitigation: keep the rule small and isolated (a single `HouseholdId == null` branch checked before/alongside the existing handler) rather than generalizing the household authorization system prematurely.
- **[Risk] `Part` catalog table with zero behavior could be mistaken for a half-built feature.** → Mitigation: explicitly called out as schema-only in this design and in `tasks.md`; no service/controller/UI is scaffolded for it, so there's nothing partially working to confuse.

## Migration Plan

Single reset migration (consistent with this project's pre-launch convention): drop `ServiceRecords`, add `MaintenanceItems`, `ChecklistItems`, `PartLines`, `Parts`, `Templates`, `TemplateSteps`. Seed built-in platform templates as part of the same migration or a startup seeder (consistent with how `PlatformAdmin` role seeding already works). No data migration — no production data exists. Rollback is reverting the deploy and migration; nothing to reconcile.

## Open Questions

- Exact set of built-in seeded platform templates (oil change, tire rotation, winterization, etc.) and their step/interval content — a product content question, not an architectural one; can be finalized during implementation without blocking the schema/API work.
