# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Self-hosted, multi-tenant garage/vehicle maintenance tracker. Tracks registration, warranties, service history, usage (mileage/engine hours), and fuel across diverse asset types (vehicles, trailers, equipment).

Backend: ASP.NET Core (.NET 10) Web API, Clean Architecture. Frontend: React 19 + TypeScript + Vite, served separately.

Change management uses OpenSpec (`openspec/`) — specs in `openspec/specs/`, in-progress proposals in `openspec/changes/`, completed ones in `openspec/changes/archive/`. Check there for the current domain model and conventions before assuming behavior.

## Commands

### Backend (.NET)

```
dotnet build                                                    # build whole solution
dotnet test                                                      # run all tests (unit + integration)
dotnet test tests/Steward.UnitTests                   # unit tests only (no DB needed)
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"   # single test
dotnet run --project src/Steward.Api                  # run the API
```

Integration tests (`tests/Steward.IntegrationTests`) require a running Postgres (`docker compose up -d postgres`) and migrate `steward_test` automatically via `DatabaseFixture`/`IntegrationTestFactory` — no manual setup needed beyond having Postgres up.

EF Core migrations:
```
dotnet ef migrations add <Name> --project src/Steward.Infrastructure --startup-project src/Steward.Api
dotnet ef database update --project src/Steward.Infrastructure --startup-project src/Steward.Api
```

### Frontend (`src/Steward.Web`)

```
npm run dev          # Vite dev server
npm run build         # tsc -b && vite build
npm run lint          # eslint
npm test              # vitest run
npm run generate:api  # regenerate src/api/schema.d.ts from the running API's OpenAPI doc (needs API on :5000)
```

Run `generate:api` after changing any API contract (DTOs, routes) so the frontend's typed client stays in sync.

### Local stack

See README.md for the two local dev options (native vs fully containerized via `docker compose up --build`). The API exposes Scalar API docs at `/scalar/v1` in Development.

## Architecture

### Backend: Clean Architecture, strict dependency direction

`Domain` → `Application` → `Infrastructure` → `Api`. Domain has zero framework dependencies. Each feature area (Assets, Tracking/*, Households, Identity) follows the same split:

- **Application**: `I<X>Service` interface + `Dtos.cs` + FluentValidation `Validators.cs` — no EF Core, no framework types.
- **Infrastructure**: `<X>Service` implementation (EF Core, Identity, Npgsql), plus an `<Area>ServiceExtensions.cs` static class that registers services via `AddScoped` (e.g. `AddStewardAssets`, `AddStewardTracking`). `Program.cs` composes these `Add*` extension methods rather than registering services directly.
- **Api**: thin controllers per resource, versioned via Asp.Versioning.

When adding a new tracked resource, mirror an existing one end-to-end (e.g. `Tracking/MileageLogs`: `Dtos.cs`, `Validators.cs`, `I*Service.cs` in Application; `*Service.cs` + EF `*Configuration.cs` in Infrastructure; `*Controller.cs` in Api) rather than inventing a new shape.

### Domain model: TPH asset hierarchy

`Asset` is abstract with EF Core Table-Per-Hierarchy (single `Assets` table, `Discriminator` column set in `AssetConfiguration`):

```
Asset
 ├─ Vehicle (abstract) — VIN, registration, usage tracking
 │   ├─ Snowmobile, Utv, Car, Truck
 │   └─ Boat (HIN instead of VIN)
 ├─ Trailer (abstract) — plate/registration, no engine hours
 │   ├─ SnowmobileTrailer, EnclosedTrailer
 └─ Equipment (abstract) — no registration
     ├─ RidingMower, PowerWasher, SmallEngine
```

`Engine` (0..N per Asset) is tracked separately with installation history (`InstalledAtAssetMiles`/`Hours`). Cross-cutting records attach to an Asset (and optionally an Engine): `Registration`, `Warranty`, `ServiceRecord`, `MileageLog`, `EngineHoursLog`, `FuelLog`. Adding a new asset subtype means adding the EF discriminator mapping in `AssetConfiguration.cs` and the corresponding entity/mapper logic in `AssetMapper.cs`.

### Multi-tenancy and authorization

Household-scoped, **resource-based authorization** — not per-household dynamic roles. Every household-scoped resource implements `IHouseholdResource` (exposes `HouseholdId`); `HouseholdAuthorizationHandler` (Infrastructure/Authorization) checks the caller's `HouseholdMembership` row and role (`Owner`/`Contributor`/`Viewer`) against `HouseholdOperations` (`View`/`Edit`/`Delete`/`Invite`). `PlatformAdmin` is an ASP.NET Core Identity role that bypasses household checks entirely. When adding a new authorized endpoint, build an `IHouseholdResource` wrapper for the target entity and call the existing handler — don't write new authorization logic per-controller.

Auth is stateless JWT Bearer (15 min expiry, no refresh tokens yet) plus OAuth (Google/Facebook/Apple) exchanged via `IOAuthExchangeService`.

### Frontend structure (`src/Steward.Web/src`)

- `api/` — axios client (`client.ts`) + per-resource functions, typed against `api/schema.d.ts` (generated, do not hand-edit — run `generate:api`).
- `hooks/` — TanStack Query hooks per resource (`useAssets`, `useEngines`, `useHouseholds`).
- `context/AuthContext.tsx` + `routes/ProtectedRoute.tsx` / `PublicOnlyRoute.tsx` — auth/session gating.
- `components/ui/` — shadcn/ui primitives (generated via `components.json`, follow existing patterns rather than hand-rolling).
- `components/<feature>/` + `pages/` — feature UI, mirrors backend feature areas (assets, households, registrations, warranties, tracking, documents).
- `lib/permissions.ts` — frontend mirror of household role checks; keep in sync with backend `HouseholdOperations` semantics.

### Testing conventions

- Integration tests use a shared xUnit collection fixture (`DatabaseFixture`, `[Collection("Database collection")]`) wrapping `IntegrationTestFactory : WebApplicationFactory<Program>` — one Postgres test database (`steward_test`) migrated once per test run, not per-test.
- `Program.cs` reads `Jwt:Key` eagerly during host build, so test config (connection string, JWT key, storage path) is injected via `Environment.SetEnvironmentVariable` in `IntegrationTestFactory`'s static constructor — before the host builds — rather than through `ConfigureAppConfiguration`.
- Frontend tests (Vitest + Testing Library) live alongside source as `*.test.ts(x)`.
