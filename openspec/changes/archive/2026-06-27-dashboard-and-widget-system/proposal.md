## Why

The current household overview is a placeholder — a single link to the asset list — leaving users with no at-a-glance view of their garage's health, upcoming obligations, or fleet metrics. At the same time, the engine entity captures basic specs but lacks the performance and service-prep data (horsepower, torque, oil capacity, recommended oil type) needed to make maintenance meaningful. This change delivers the real homepage the app needs: a dynamic, per-household dashboard built from composable widgets, powered by a new set of aggregate stats derived from properly spec'd engine records.

## What Changes

- **`EngineStatus` gains a `Broken` state** (`Active | Broken | Retired`) so the system can distinguish a non-functional installed engine from one that has been removed from service. Only `Active` engines count toward Garage Logic metrics.
- **`Engine` gains six new optional spec fields**: `HorsepowerHp`, `TorqueNm` (stored Nm, displayed ft-lbs), `OilCapacityL` (stored liters, displayed quarts), `RecommendedOilType`, `CoolantCapacityL`, and `RecommendedOctane`.
- **Four Garage Logic aggregate stats** are computable from engine data: Cylinder Index (traditional GL formula — active ICE cylinders only), Total Displacement, Total Horsepower, and Total Torque.
- **Two new domain entities** — `HouseholdDashboard` and `DashboardWidget` — allow each household to maintain one or more named dashboards composed from a predefined widget catalog.
- **New API endpoints** for dashboard CRUD, widget layout management (atomic PUT), and a performance-critical snapshot endpoint that computes all widget data in a single server round-trip using SQL aggregations only.
- **Default dashboard seeding** — when a household has no dashboards, the system auto-creates an "Overview" dashboard with AssetCount, CylinderIndex, DueSoon, and RecentActivity widgets.
- **`HouseholdOverviewPage` replaced** with a real dashboard page: dashboard selector (tabs/dropdown), widget grid (size-aware CSS grid), and widget catalog for Owner/Contributors to customize layouts.
- **Engine create/update forms updated** in the frontend to expose the new spec fields with appropriate unit labels (e.g., "Nm / ft-lbs").
- **`localStorage` persistence** of the last-selected dashboard ID per household, enabling per-user dashboard preference without a server-side user preference store.

## Capabilities

### New Capabilities

- `engine-specs`: Extended engine specification fields (HP, torque, oil capacity, oil type, coolant capacity, octane) and the `Broken` status state. Establishes the data foundation for Garage Logic aggregate metrics.
- `dashboard-widgets`: Per-household customizable dashboards composed from a predefined widget catalog. Covers the dashboard domain model, CRUD APIs, widget layout management, and the snapshot endpoint. Includes default dashboard seeding and frontend rendering.

### Modified Capabilities

- `engine-management`: The `EngineStatus` enum gains a `Broken` value, and the create/update contract gains six new optional fields. The retire/reactivate endpoints must account for the `Broken` state (only `Active` engines can be marked `Broken`; a `Broken` engine can be reactivated to `Active` or retired). Existing `Active | Retired` behavior is preserved.
- `frontend-shell`: The authenticated layout gains a "Dashboard" primary nav link pointing to the household overview route. The existing "My Gear" and "Household Settings" links are preserved.

## Impact

- **Domain**: `Engine` entity updated; two new entities (`HouseholdDashboard`, `DashboardWidget`); two new enums (`WidgetType`, `WidgetSize`); `EngineStatus` expanded.
- **Application**: Updated `Engine` DTOs and validators; new `IDashboardService` with DTOs and validators for dashboards, widget layouts, and the snapshot response.
- **Infrastructure**: Updated `EngineService` and `EngineConfiguration`; new `DashboardService` and EF configurations for `HouseholdDashboard`/`DashboardWidget`; EF Core migration for all schema changes; default dashboard seeding logic.
- **API**: Updated `EnginesController` (retire/reactivate action semantics); new `DashboardsController` with endpoints for CRUD, widget layout, and snapshot.
- **Frontend**: New `DashboardPage` replacing `HouseholdOverviewPage`; per-widget React components (one per `WidgetType`); widget size-aware grid renderer; dashboard selector; widget catalog sheet (Owner/Contributor only); updated engine create/update forms with new fields and unit labels; `localStorage` helper for last-selected dashboard.
- **Database**: One new EF Core migration covering `Engine` column additions, `EngineStatus` enum value addition, and two new tables (`HouseholdDashboards`, `DashboardWidgets`).
