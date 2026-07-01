## Context

The app currently has a `HouseholdOverviewPage` that renders a single "View assets" link — a stub left from the initial frontend foundation. The `Engine` entity has basic spec fields (`Cylinders`, `DisplacementCC`) but is missing the performance and service-prep data that makes an engine record useful day-to-day: horsepower, torque, oil capacity, and recommended oil type. There is also no household-level aggregate view — no Garage Logic metrics, no upcoming-obligation summary, no recent activity feed.

This design adds a customizable dashboard system on top of a clean stats API, and expands the Engine entity to carry the data those stats need.

## Goals / Non-Goals

**Goals:**
- Add six engine spec fields and a `Broken` status state, fully backward-compatible with existing engine records.
- Expose four Garage Logic aggregate metrics (Cylinder Index, Total Displacement, Total HP, Total Torque) computed server-side via SQL aggregation.
- Introduce a per-household multi-dashboard model with a predefined widget catalog and per-widget size and configuration.
- Implement a single `snapshot` endpoint that computes all widget data for a given dashboard in one round trip, using only SQL-level projections — never loading entity trees.
- Replace `HouseholdOverviewPage` with a real dashboard UI: selector, size-aware widget grid, and an Owner/Contributor-only widget catalog for customization.
- Remember the last-selected dashboard per household in `localStorage` so users get their preferred view on return without server-side user preference storage.

**Non-Goals:**
- Real-time dashboard updates (no WebSockets or SSE — this is a pull model).
- User-defined widget types (catalog is fixed server-side; users choose from it, not create their own).
- Chart/graph widgets (initial widget set is numeric stats and ordered lists; charting can be added later via new `WidgetType` values).
- Per-user server-side dashboard preferences (localStorage is sufficient for the first pass).
- Dashboard sharing or embedding (scoped to authenticated household members only).
- Per-widget independent data fetching (the snapshot endpoint returns all widget data in one call).

## Decisions

### 1. Snapshot endpoint design: bundled, server-driven

The `GET /api/v1/households/{hid}/dashboards/{did}/snapshot` endpoint reads the dashboard's widget layout from the database, determines which widget types are active, runs only the corresponding queries, and returns a single JSON object keyed by widget type.

**Why bundled over per-widget endpoints:** A dashboard with 6 widgets would require 6 parallel client requests. Bundling eliminates the round trips, and the server already knows the widget layout (it owns the truth). Per-widget endpoints would expose widget data routes that have no meaning outside a dashboard context.

**Why server-driven over client-sending-widget-list:** The client doesn't need to repeat the widget list on every snapshot fetch — the server reads it from the stored layout. This prevents the client from requesting data for widgets not in the layout, and keeps the snapshot call simple: `GET` with no body.

**Snapshot response shape:**

```json
{
  "AssetCount":        { "count": 5 },
  "CylinderIndex":     { "totalCylinders": 15, "engineCount": 4 },
  "TotalDisplacement": { "totalCc": 7234.5, "engineCount": 4 },
  "TotalHorsepower":   { "totalHp": 642.0, "engineCount": 5 },
  "TotalTorque":       { "totalNm": 1204.0, "engineCount": 5 },
  "DueSoon": {
    "items": [
      {
        "assetId": "...", "assetName": "Mazda 3",
        "recordType": "Registration",
        "expiresOn": "2026-01-15",
        "urgency": "Overdue"
      }
    ]
  },
  "RecentActivity": {
    "items": [
      {
        "assetId": "...", "assetName": "Sea Ray 250",
        "description": "Annual service", "performedOn": "2026-06-20",
        "cost": 1200.00
      }
    ]
  },
  "FuelCostYtd": { "totalCost": 2340.00, "logCount": 47 },
  "MileageMtd":  { "totalMiles": 1204.0, "logCount": 23 }
}
```

The response only includes keys for widgets present in the dashboard layout. A dashboard with only `AssetCount` and `DueSoon` returns only those two keys.

### 2. All snapshot queries are SQL projections — no entity loading

Every widget metric is a targeted EF Core query using `.Where()` + `.Select()` + terminal aggregation (`.CountAsync()`, `.SumAsync()`) or `.Select(projection).ToListAsync()` with explicit column selection. No `.Include()`, no navigation property traversal, no loading of entity graphs.

**Cylinder Index query pattern:**
```csharp
await context.Engines
    .Where(e => e.Asset.HouseholdId == householdId
             && e.Status == EngineStatus.Active
             && e.EngineType == EngineType.Ice
             && e.Cylinders != null)
    .SumAsync(e => (int?)e.Cylinders) ?? 0;
```

**DueSoon query pattern (registrations + warranties joined in-app):**
Run two separate aggregation queries (one for registrations, one for warranties), project to a shared `DueItem` record, union and sort. Avoids a complex cross-table SQL UNION (harder to compose with EF); both sets are small enough that unioning in C# after projection is acceptable.

**RecentActivity query pattern:**
```csharp
await context.ServiceRecords
    .Where(sr => sr.Asset.HouseholdId == householdId)
    .OrderByDescending(sr => sr.PerformedOn)
    .Take(config.Limit)
    .Select(sr => new RecentActivityItem(sr.AssetId, sr.Asset.Name, sr.Description, sr.PerformedOn, sr.Cost))
    .ToListAsync();
```

### 3. Atomic widget layout replacement (PUT replaces, no per-widget CRUD)

`PUT /api/v1/households/{hid}/dashboards/{did}/widgets` accepts the full ordered array of widget definitions and atomically replaces the existing layout (delete all + insert new within a single transaction).

**Why not per-widget POST/DELETE/PATCH:** Dashboard customization is holistic — drag-and-drop reorder, add/remove, resize — and treating it as individual resource mutations creates partial-state hazards and ordering conflicts. A full replace is idempotent, trivial to implement, and matches how a drag-and-drop UI naturally produces its output.

**Why not PUT on the dashboard root:** Separating layout management (`PUT /widgets`) from dashboard metadata (`PUT /dashboards/{id}`) keeps the contracts clean. Name changes don't require re-sending the full widget list.

### 4. EngineStatus gains `Broken` as a third enum value

Current: `Active = 0`, `Retired = 1`  
New: `Active = 0`, `Retired = 1`, `Broken = 2`

`Broken` means installed but non-functional. A broken engine does NOT count toward any Garage Logic metric. Only `Active` engines count.

**State machine:**
```
Active ──→ Broken    (via POST .../mark-broken)
Active ──→ Retired   (via POST .../retire)
Broken ──→ Active    (via POST .../reactivate)
Broken ──→ Retired   (via POST .../retire)
Retired ──→ Active   (via POST .../reactivate)
```

**Why not a boolean `IsOperational` flag:** A flag would allow `Retired + IsOperational = true`, which is nonsensical. An enum enforces the state machine. Adding `Broken = 2` at the end preserves existing integer-mapped values in the database.

The existing `/retire` and `/reactivate` endpoints are extended: retire now accepts `Active` or `Broken` source state; reactivate sets status to `Active` regardless of prior state. A new `/mark-broken` endpoint is added.

### 5. Unit storage is SI; display is a presentation concern

`TorqueNm` (Newton-metres), `OilCapacityL` and `CoolantCapacityL` (litres) are stored as SI units. The frontend converts to ft-lbs and quarts for display. `HorsepowerHp` is stored as SAE HP — HP is universally used in the automotive domain and the metric equivalent (PS) differs by only ~1.4%.

**Why SI for torque/volume:** SI units are the lossless canonical form. Storing in display units (ft-lbs, quarts) would require re-rounding on every unit-preference change. Conversion constants are exact or near-exact: 1 Nm = 0.7376 ft-lbs; 1 litre = 1.0567 quarts.

### 6. Default dashboard seeding is lazy (on first `GET /dashboards`)

When `GET /api/v1/households/{hid}/dashboards` is called and no dashboards exist for the household, the service auto-creates the "Overview" default dashboard (AssetCount Small, CylinderIndex Small, DueSoon Full with 30-day window, RecentActivity Full with 5-item limit) within the same request before returning.

**Why lazy over eager (household-creation hook):** Avoids coupling the household creation path to dashboard logic. Households created before this change exists are handled transparently. The lazy check is a single `COUNT` query with negligible overhead.

### 7. Per-household dashboards; localStorage for last-selected

Dashboards belong to the household and are visible to all active members. Any member can view any dashboard. Owner/Contributors can create, edit, and delete dashboards.

The frontend stores `dashboard:${householdId}` in `localStorage` to remember the last-selected dashboard ID per household. On page load: try stored ID → fall back to `isDefault = true` → fall back to first dashboard.

**Why not per-user server-side preference:** Adds a database table and API endpoint for what is fundamentally a UI-state concern. `localStorage` is immediate, requires no auth, and survives sessions. The tradeoff is that it's device-specific (switching devices loses the preference), which is acceptable for a first pass.

### 8. Widget catalog is server-defined; Config is a JSON column

`WidgetType` is a C# enum with a fixed set of values. Users select from the catalog; they cannot define custom widget types.

Per-widget configuration that varies by instance (e.g., `DueSoon.daysAhead`, `RecentActivity.limit`) is stored as a JSON string in `DashboardWidget.Config`. EF Core maps this as `varchar`; the application layer deserializes to a typed record per widget type.

**Why JSON config over per-widget config tables:** Config is small (2–3 fields max per widget), never queried with a WHERE clause, and schema varies by widget type. A dedicated table per widget type would require schema changes for every new widget; JSON keeps the model open for extension.

## Risks / Trade-offs

**[DueSoon query complexity]** → Joining across registrations and warranties via the household requires traversing `Asset.HouseholdId`. Mitigation: ensure `Assets.HouseholdId` and `Registrations.ExpiresOn` / `Warranties.ExpiresOn` are indexed. The two-query union approach (one per record type) keeps each query simple.

**[EngineStatus.Broken — exhaustive switch gaps]** → Any existing `switch (engine.Status)` that doesn't handle `Broken` will miss the new case. C# doesn't enforce exhaustive enum switches at compile time. Mitigation: audit all `switch` / pattern-match on `EngineStatus` during implementation; add a test that constructs a `Broken` engine and asserts it's excluded from metric queries.

**[Snapshot endpoint under large households]** → A household with many assets and thousands of fuel/mileage logs will make the `FuelCostYtd` and `MileageMtd` aggregations heavier over time. Mitigation: both are simple `SUM` aggregations on indexed date + household join paths. Add DB indexes on `FuelLogs.LoggedOn` and `MileageLogs.LoggedOn` if not already present.

**[Widget layout atomic replace on concurrent edits]** → Two Owner/Contributor users editing the dashboard layout simultaneously will cause the second PUT to silently win. Mitigation: acceptable for the first pass — simultaneous dashboard editing is an edge case for a household app. Optimistic concurrency (`ETag`/`If-Match`) can be added later if needed.

**[localStorage dashboard preference is device-local]** → A user switching from desktop to mobile will land on the default dashboard, not their remembered preference. Mitigation: the default dashboard (`isDefault = true`) is a reasonable fallback. Server-side per-user preference storage is a future option.

## Migration Plan

1. Add EF Core migration: `Engine` column additions (all nullable, no default needed), `HouseholdDashboards` and `DashboardWidgets` tables.
2. `EngineStatus.Broken = 2` is additive — existing `Active = 0` and `Retired = 1` integer values are unchanged in the database.
3. Deploy backend first; frontend can be deployed after without coordination issues (new dashboard routes replace the existing stub page).
4. Rollback: migration is reversible (drop new columns and tables, remove `Broken` from enum — no data loss since no existing rows will have `Broken` status or dashboard records immediately after deploy).

## Open Questions

- **DueSoon scope**: Currently planned to cover `Registration` and `Warranty` expiry only (both have explicit `ExpiresOn` dates). Service records don't have a "next due date" field — should they? Deferred to a future change.
- **FuelCostYtd period**: Calendar year (Jan 1 to Dec 31). Not fiscal year. Rolling-12-months is a possible future option.
- **MileageMtd period**: Calendar month (first of month to today). Not rolling 30 days.
- **DueSoon urgency thresholds**: `Overdue` = expired (expiry < today). `DueSoon` = expiring within 7 days. `Upcoming` = expiring within `daysAhead` days (default 30). These thresholds are hard-coded server-side for now; configurable per-widget is possible later via `Config` JSON.
