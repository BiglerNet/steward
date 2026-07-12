# solution-structure Specification

## Purpose
Defines the monorepo layout and Clean Architecture project structure.

## Requirements
### Requirement: Monorepo solution layout
The solution SHALL be organized as a monorepo with backend projects under `src/`, test projects under `tests/`, and the React frontend under `src/Steward.Web/`. A single `.sln` file at the repo root SHALL reference all backend and test projects.

#### Scenario: Solution builds cleanly
- **WHEN** a developer runs `dotnet build` from the repo root
- **THEN** all projects compile without errors or warnings

#### Scenario: Frontend scaffold exists
- **WHEN** a developer navigates to `src/Steward.Web/`
- **THEN** a valid Vite + React 19 + TypeScript project is present and `npm install && npm run dev` starts the dev server

---

### Requirement: Backend project structure
The solution SHALL contain exactly four backend source projects with the following references:

- `Steward.Domain` — no project references, no NuGet framework dependencies
- `Steward.Application` — references `Domain`; includes FluentValidation
- `Steward.Infrastructure` — references `Application` and `Domain`; owns EF Core, Npgsql, Identity
- `Steward.Api` — references `Application` and `Infrastructure`; ASP.NET Core entrypoint

#### Scenario: Dependency direction is enforced
- **WHEN** the `Domain` project is inspected
- **THEN** it contains no references to `Application`, `Infrastructure`, `Api`, or any ASP.NET / EF Core packages

#### Scenario: Api is the composition root
- **WHEN** the application starts
- **THEN** all services are registered in `Api/Program.cs` via extension methods defined in `Application` and `Infrastructure`

---

### Requirement: Shared build configuration
A `Directory.Build.props` file at the repo root SHALL define shared MSBuild properties applied to all projects: `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, and `TreatWarningsAsErrors=true`.

A `global.json` SHALL pin the .NET SDK to version `10.0.x` with `rollForward: latestMinor`.

#### Scenario: New project inherits shared config
- **WHEN** a new `.csproj` is added to `src/` without specifying `TargetFramework`
- **THEN** `dotnet build` correctly targets `net10.0` via inherited `Directory.Build.props`

---

### Requirement: Test projects
The solution SHALL contain `Steward.UnitTests` (xUnit, references `Domain` and `Application`) and `Steward.IntegrationTests` (xUnit, references `Api` and `Infrastructure`).

#### Scenario: Test suite runs
- **WHEN** a developer runs `dotnet test` from the repo root
- **THEN** all tests pass (initial scaffold has zero tests; the suite exits with success)

---

### Requirement: Local development database
A `docker-compose.yml` at the repo root SHALL provide a PostgreSQL 16 service with a named volume for data persistence. `appsettings.Development.json` SHALL contain a connection string pointing at this service.

#### Scenario: Local database starts
- **WHEN** a developer runs `docker compose up -d`
- **THEN** a PostgreSQL 16 container is running and accepting connections on the configured port

#### Scenario: EF migrations apply on startup
- **WHEN** the API starts against the local docker-compose database
- **THEN** all EF Core migrations run automatically (or a clear `dotnet ef database update` step is documented) and the schema is created without errors

---

### Requirement: API versioning via URL segment
The system SHALL use `Asp.Versioning.Mvc` for URL segment-based API versioning. All API routes SHALL be prefixed with `/api/v{version}/`. The default API version SHALL be `1.0`. `ReportApiVersions` SHALL be enabled so responses include an `api-supported-versions` header. The OpenAPI document SHALL be generated per version (one Scalar document for `v1`, additional documents added when future versions are introduced).

#### Scenario: Versioned route is reachable
- **WHEN** a client sends a request to `/api/v1/households`
- **THEN** the request is routed to the correct controller action

#### Scenario: Unversioned request uses default
- **WHEN** a client sends a request to `/api/households` without a version segment
- **THEN** the request is handled by the v1 controller action (default version applied)

#### Scenario: Response reports supported versions
- **WHEN** any API response is returned
- **THEN** the response includes an `api-supported-versions: 1.0` header

#### Scenario: Scalar UI shows versioned documents
- **WHEN** a developer navigates to the Scalar API explorer
- **THEN** a `v1` document is available listing all v1 endpoints with JWT auth support
