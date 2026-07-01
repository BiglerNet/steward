## 1. Domain — Engine Entity Changes

- [x] 1.1 [Domain] Add `Broken = 2` to `EngineStatus` enum in `Steward.Domain/Enums/EngineStatus.cs`
- [x] 1.2 [Domain] Add six new nullable spec fields to `Engine` entity: `HorsepowerHp` (decimal?), `TorqueNm` (decimal?), `OilCapacityL` (decimal?), `RecommendedOilType` (string?), `CoolantCapacityL` (decimal?), `RecommendedOctane` (int?)
- [x] 1.3 [Domain/Infrastructure] Audit all `switch` and pattern-match expressions on `EngineStatus` across the solution and add `Broken` case handling where missing (search for `EngineStatus` usages)

## 2. Domain — Dashboard Entities

- [x] 2.1 [Domain] Create `WidgetType` enum: `AssetCount`, `CylinderIndex`, `TotalDisplacement`, `TotalHorsepower`, `TotalTorque`, `DueSoon`, `RecentActivity`, `FuelCostYtd`, `MileageMtd`
- [x] 2.2 [Domain] Create `WidgetSize` enum: `Small`, `Wide`, `Full`
- [x] 2.3 [Domain] Create `HouseholdDashboard` entity: `Id` (Guid), `HouseholdId` (Guid), `Name` (string), `IsDefault` (bool), `Position` (int), and navigation `ICollection<DashboardWidget> Widgets`
- [x] 2.4 [Domain] Create `DashboardWidget` entity: `Id` (Guid), `DashboardId` (Guid), `WidgetType` (WidgetType), `WidgetSize` (WidgetSize), `Position` (int), `Config` (string? — JSON), and navigation `HouseholdDashboard Dashboard`

## 3. Infrastructure — EF Core Configuration & Migration

- [x] 3.1 [Infrastructure] Update `EngineConfiguration` to map the six new nullable spec columns; store `EngineStatus` as int (existing convention — verify `Broken = 2` doesn't conflict)
- [x] 3.2 [Infrastructure] Create `HouseholdDashboardConfiguration`: table `HouseholdDashboards`, FK to `Households`, `Name` max 100 chars, index on `HouseholdId`, unique index on `(HouseholdId, Name)` (case-insensitive via `lower()` expression index or collation)
- [x] 3.3 [Infrastructure] Create `DashboardWidgetConfiguration`: table `DashboardWidgets`, FK to `HouseholdDashboards` (cascade delete), `WidgetType` and `WidgetSize` stored as int, `Config` as `varchar(2000)`
- [x] 3.4 [Infrastructure] Add EF Core migration `AddDashboardsAndEngineSpecs` covering all schema changes; verify `dotnet ef migrations add` produces expected Up/Down
- [x] 3.5 [Infrastructure] Register `HouseholdDashboard` and `DashboardWidget` `DbSet`s on `StewardDbContext`

## 4. Application — Engine Service Updates

- [x] 4.1 [Application] Update `EngineResponse`, `CreateEngineRequest`, and `UpdateEngineRequest` records in `Engines/Dtos.cs` to include the six new spec fields
- [x] 4.2 [Application] Update `CreateEngineValidator` and `UpdateEngineValidator` in `Engines/Validators.cs`: `RecommendedOctane` must be one of `{87, 89, 91, 93}` when provided; all other new fields have no additional constraints beyond nullable
- [x] 4.3 [Application] Add `MarkBrokenAsync(Guid householdId, Guid assetId, Guid engineId)` to `IEngineService`
- [x] 4.4 [Infrastructure] Update `EngineService.CreateAsync` and `UpdateAsync` to map and persist the six new spec fields from request to entity
- [x] 4.5 [Infrastructure] Update `EngineService.RetireAsync`: accept `Active` or `Broken` source state; reject `Retired` source with `InvalidOperationException`
- [x] 4.6 [Infrastructure] Update `EngineService.ReactivateAsync`: accept `Retired` or `Broken` source state; reject `Active` source with `InvalidOperationException`
- [x] 4.7 [Infrastructure] Implement `EngineService.MarkBrokenAsync`: load engine, verify membership + Contributor role, assert `Status == Active`, set `Status = Broken`, save

## 5. API — Engine Controller Update

- [x] 5.1 [Api] Add `POST .../engines/{engineId}/mark-broken` action to `EnginesController` calling `IEngineService.MarkBrokenAsync`; return `200 EngineResponse` on success, `400` for invalid state, `403` for Viewer, `404` for not found

## 6. Application — Dashboard Service Interface & DTOs

- [x] 6.1 [Application] Create `Dashboards/Dtos.cs` with records: `DashboardSummaryResponse` (Id, Name, IsDefault, Position), `DashboardDetailResponse` (Id, Name, IsDefault, Position, IReadOnlyList<WidgetResponse>), `WidgetResponse` (Id, WidgetType, WidgetSize, Position, Config), `CreateDashboardRequest` (Name, IsDefault?), `UpdateDashboardRequest` (Name, IsDefault, Position), `ReplaceWidgetLayoutRequest` (IReadOnlyList<WidgetDefinition>), `WidgetDefinition` (WidgetType, WidgetSize, Config?)
- [x] 6.2 [Application] Create snapshot DTOs in `Dashboards/SnapshotDtos.cs`: `DashboardSnapshotResponse` (Dictionary<string, object>), `AssetCountData` (Count), `CylinderIndexData` (TotalCylinders, EngineCount), `TotalDisplacementData` (TotalCc, EngineCount), `TotalHorsepowerData` (TotalHp, EngineCount), `TotalTorqueData` (TotalNm, EngineCount), `DueSoonData` (IReadOnlyList<DueItem>), `DueItem` (AssetId, AssetName, RecordType, ExpiresOn, Urgency), `RecentActivityData` (IReadOnlyList<ActivityItem>), `ActivityItem` (AssetId, AssetName, Description, PerformedOn, Cost), `FuelCostYtdData` (TotalCost, LogCount), `MileageMtdData` (TotalMiles, LogCount)
- [x] 6.3 [Application] Create `IDashboardService` interface with methods: `ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ReplaceWidgetLayoutAsync`, `GetSnapshotAsync`
- [x] 6.4 [Application] Create `Dashboards/Validators.cs`: validate `CreateDashboardRequest` (Name required, max 100), `UpdateDashboardRequest` (Name required, max 100), `ReplaceWidgetLayoutRequest` (each WidgetType must be a valid enum value, each WidgetSize must be valid; Config must be valid JSON when provided)

## 7. Infrastructure — Dashboard Service Implementation

- [x] 7.1 [Infrastructure] Implement `DashboardService.ListAsync`: `SELECT id, name, is_default, position FROM household_dashboards WHERE household_id = @id ORDER BY position` via EF projection; if result is empty, call private `SeedDefaultDashboardAsync` and return the seeded dashboard
- [x] 7.2 [Infrastructure] Implement `SeedDefaultDashboardAsync`: create "Overview" dashboard (`IsDefault = true`, `Position = 0`) with four widgets — `AssetCount Small` (pos 0), `CylinderIndex Small` (pos 1), `DueSoon Full` (pos 2, config `{"daysAhead":30}`), `RecentActivity Full` (pos 3, config `{"limit":5}`) — within a single transaction
- [x] 7.3 [Infrastructure] Implement `DashboardService.GetAsync`: fetch dashboard + widgets ordered by position via EF `.Select()` projection; return 404-equivalent if not found or wrong household
- [x] 7.4 [Infrastructure] Implement `DashboardService.CreateAsync`: validate name uniqueness (case-insensitive within household), demote existing default if `IsDefault = true`, insert new dashboard; return `DashboardSummaryResponse`
- [x] 7.5 [Infrastructure] Implement `DashboardService.UpdateAsync`: validate name uniqueness excluding self, demote prior default if needed, update `Name`/`IsDefault`/`Position`
- [x] 7.6 [Infrastructure] Implement `DashboardService.DeleteAsync`: assert at least 2 dashboards exist (reject if only one), delete the dashboard (cascade deletes widgets via FK)
- [x] 7.7 [Infrastructure] Implement `DashboardService.ReplaceWidgetLayoutAsync`: within a single transaction, delete all existing `DashboardWidget` rows for the dashboard, insert new rows from request array (derive `Position` from array index)
- [x] 7.8 [Infrastructure] Implement snapshot query — Garage Logic stats: run four targeted EF aggregate queries (CountAsync, SumAsync) for AssetCount, CylinderIndex (Active + ICE only), TotalDisplacement (Active only), TotalHorsepower (Active only), TotalTorque (Active only); only execute a query if the corresponding WidgetType is in the dashboard layout
- [x] 7.9 [Infrastructure] Implement snapshot query — DueSoon: run two EF `.Select()` projections (one for `Registrations`, one for `Warranties`) filtering by `household_id` join via `Asset`, `ExpiresOn` within `daysAhead` days (or already expired); union results in C#, sort by `ExpiresOn` ascending, classify urgency (`Overdue`/`DueSoon`/`Upcoming`)
- [x] 7.10 [Infrastructure] Implement snapshot query — RecentActivity: EF `.Select()` projection on `ServiceRecords` joined to `Asset`, ordered by `PerformedOn` descending, `Take(config.Limit)` (max 20)
- [x] 7.11 [Infrastructure] Implement snapshot query — FuelCostYtd: `SumAsync(fl => fl.TotalCost)` on `FuelLogs` where `Asset.HouseholdId == id AND LoggedOn.Year == DateTime.UtcNow.Year`
- [x] 7.12 [Infrastructure] Implement snapshot query — MileageMtd: `SumAsync(ml => ml.TripMiles ?? ml.OdometerReading)` on `MileageLogs` where `Asset.HouseholdId == id AND LoggedOn >= firstOfMonth`; use the correct mileage field based on log type
- [x] 7.13 [Infrastructure] Create `DashboardServiceExtensions.cs` registering `IDashboardService → DashboardService` via `AddScoped`; call from `Program.cs`

## 8. API — Dashboards Controller

- [x] 8.1 [Api] Create `DashboardsController` (versioned, route `api/v{version}/households/{householdId}/dashboards`): implement `GET /` → `ListAsync`, `POST /` → `CreateAsync`, `GET /{id}` → `GetAsync`, `PUT /{id}` → `UpdateAsync`, `DELETE /{id}` → `DeleteAsync`
- [x] 8.2 [Api] Add `PUT /{dashboardId}/widgets` action to `DashboardsController` → `ReplaceWidgetLayoutAsync`; return `200` on success, `400` for invalid widget types/sizes, `403` for Viewer
- [x] 8.3 [Api] Add `GET /{dashboardId}/snapshot` action to `DashboardsController` → `GetSnapshotAsync`; return `200 DashboardSnapshotResponse`, `403` for non-members
- [x] 8.4 [Api] Ensure all dashboard endpoints use `HouseholdAuthorizationHandler` via `IHouseholdResource` wrapper (consistent with the rest of the app); write/delete actions check Contributor/Owner role

## 9. Integration Tests — Engine Updates

- [x] 9.1 [Tests] Update existing engine integration tests to include new spec fields in request/response assertions
- [x] 9.2 [Tests] Add integration test: `MarkBroken` transitions `Active → Broken`; reactivate from `Broken → Active`; retire from `Broken → Retired`
- [x] 9.3 [Tests] Add integration test: double mark-broken returns 400; marking retired engine broken returns 400
- [x] 9.4 [Tests] Add integration test: `RecommendedOctane: 94` returns 400

## 10. Integration Tests — Dashboards

- [x] 10.1 [Tests] Add integration test: `GET /dashboards` on household with no dashboards auto-creates default "Overview" and returns it
- [x] 10.2 [Tests] Add integration tests for full dashboard CRUD: create, rename, set-default demotes prior, delete, delete-last returns 400
- [x] 10.3 [Tests] Add integration tests for `PUT /widgets`: replaces layout atomically, empty array clears, invalid WidgetType returns 400, Viewer returns 403
- [x] 10.4 [Tests] Add integration tests for `GET /snapshot`: AssetCount correct, CylinderIndex excludes Broken/Retired engines, DueSoon urgency classification, RecentActivity respects limit, FuelCostYtd year-scoping, MileageMtd month-scoping
- [x] 10.5 [Tests] Add integration test: snapshot for empty-widget dashboard returns `{}` 

## 11. Frontend — API Regeneration & Client Functions

- [x] 11.1 [Web] Start the API (`dotnet run --project src/Steward.Api`) and run `npm run generate:api` to regenerate `src/api/schema.d.ts` with new engine fields and dashboard endpoints
- [x] 11.2 [Web] Update engine API functions in `src/api/` to include new spec fields in create/update calls
- [x] 11.3 [Web] Create `src/api/dashboards.ts` with functions: `listDashboards`, `getDashboard`, `createDashboard`, `updateDashboard`, `deleteDashboard`, `replaceWidgetLayout`, `getDashboardSnapshot`

## 12. Frontend — Unit Conversion Helpers

- [x] 12.1 [Web] Create `src/lib/units.ts` with pure conversion functions: `nmToFtLbs(nm)`, `ftLbsToNm(ftLbs)`, `litresToQt(l)`, `qtToLitres(qt)` and display helpers `formatTorque(nm)`, `formatVolume(litres)` returning localised strings with units

## 13. Frontend — Engine Form Updates

- [x] 13.1 [Web] Update engine create/update forms (in `EnginesSection` or `AssetFormDialog`) to include the six new spec fields: HP (numeric, label "HP"), Torque (numeric, input in ft-lbs, label "ft-lbs", convert to Nm on submit), Oil Capacity (numeric, input in qt, label "qt", convert to L on submit), Recommended Oil Type (text), Coolant Capacity (numeric, input in qt, label "qt", convert to L on submit), Recommended Octane (select: 87/89/91/93)
- [x] 13.2 [Web] Update engine detail view to display stored values with converted units: torque as ft-lbs, oil/coolant as quarts; show label suffixes consistently

## 14. Frontend — TanStack Query Hooks

- [x] 14.1 [Web] Create `src/hooks/useDashboards.ts` with `useDashboards(householdId)` and `useDashboard(householdId, dashboardId)` hooks (GET queries, appropriate stale times)
- [x] 14.2 [Web] Create `src/hooks/useDashboardSnapshot.ts` with `useDashboardSnapshot(householdId, dashboardId)` hook; stale time ~30s (dashboard data is near-real-time)
- [x] 14.3 [Web] Create mutation hooks in `src/hooks/useDashboardMutations.ts`: `useCreateDashboard`, `useUpdateDashboard`, `useDeleteDashboard`, `useReplaceWidgetLayout` (each invalidates `useDashboards` and `useDashboard` on success)

## 15. Frontend — Widget Components

- [x] 15.1 [Web] Create `src/components/dashboard/StatWidget.tsx`: accepts `label` (string), `value` (string | number), optional `subLabel` (string), renders a stat card matching the design system card style
- [x] 15.2 [Web] Create `src/components/dashboard/DueSoonWidget.tsx`: renders a list of due items with urgency-colour dot (`Overdue` = red, `DueSoon` = amber, `Upcoming` = green), asset name, record type, and formatted date
- [x] 15.3 [Web] Create `src/components/dashboard/RecentActivityWidget.tsx`: renders a list of service record items with asset name, description, date, and optional cost
- [x] 15.4 [Web] Create widget index barrel `src/components/dashboard/index.ts` exporting all widget components

## 16. Frontend — Dashboard Page & Grid

- [x] 16.1 [Web] Create `src/lib/dashboardStorage.ts` with `readLastDashboardId(householdId)` and `writeLastDashboardId(householdId, dashboardId)` using `localStorage` key `dashboard:${householdId}`
- [x] 16.2 [Web] Create `src/components/dashboard/WidgetGrid.tsx`: CSS grid layout where `Small` widgets span 3 columns (of 12), `Wide` spans 6, `Full` spans 12; renders the correct widget component per `WidgetType`, passing its data slice from the snapshot
- [x] 16.3 [Web] Create `src/components/dashboard/DashboardSelector.tsx`: tab strip (shadcn `Tabs` or custom) showing all dashboards by name; active tab = current dashboard; clicking a tab writes to `localStorage` and updates selected state
- [x] 16.4 [Web] Create `src/pages/DashboardPage.tsx`: fetches `useDashboards`, resolves active dashboard (localStorage → isDefault → first), fetches `useDashboardSnapshot` for active dashboard, renders `DashboardSelector` + `WidgetGrid`; shows loading skeleton during fetch; shows empty-state when no widgets
- [x] 16.5 [Web] Update `src/App.tsx` (or router config) to point `/households/:householdId` to `DashboardPage` instead of `HouseholdOverviewPage`
- [x] 16.6 [Web] Add "Dashboard" `NavLink` to `AuthenticatedLayout` primary nav pointing to `/households/:householdId` (before "My Gear")

## 17. Frontend — Widget Catalog (Customization)

- [x] 17.1 [Web] Create `src/components/dashboard/WidgetCatalogSheet.tsx`: shadcn `Sheet` (slide-over panel) listing all `WidgetType` values with name, description, default size, and add/remove toggle; only renders the trigger button for Owner/Contributor (check `useHouseholdPermissions` or equivalent)
- [x] 17.2 [Web] Add reorder controls to `WidgetCatalogSheet` (up/down buttons or drag handles via `@dnd-kit/core` if already in deps; otherwise use simple up/down buttons to avoid adding a dependency)
- [x] 17.3 [Web] Wire catalog "Save" button to call `useReplaceWidgetLayout` with the edited layout array, then close the sheet; invalidate `useDashboardSnapshot` to refresh widget data
- [x] 17.4 [Web] Add "Edit Dashboard" button to `DashboardPage` that opens `WidgetCatalogSheet`; only visible to Owner/Contributor

## 18. Final Verification

- [x] 18.1 [Tests] Run `dotnet test` — all existing and new integration/unit tests pass
- [x] 18.2 [Web] Run `npm run lint` and `npm test` — no type errors, no lint violations, Vitest tests pass
- [x] 18.3 [Manual] Smoke test: create household → default "Overview" dashboard appears with 4 widgets → add engine with full spec data (HP, torque, oil) → snapshot shows correct Cylinder Index and HP → mark engine broken → Cylinder Index drops → mark-broken an already-broken engine returns error
- [x] 18.4 [Manual] Smoke test: create second dashboard → rename it → make it default → original default demoted → delete second dashboard → first is only remaining → delete blocked with error
- [x] 18.5 [Manual] Smoke test: widget catalog — add TotalHorsepower Small widget → save → grid shows it → remove it → save → grid no longer shows it
