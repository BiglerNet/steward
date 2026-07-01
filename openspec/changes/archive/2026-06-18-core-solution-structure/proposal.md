## Why

The repository is a blank slate. Before any features can be built, the project needs a well-structured monorepo skeleton, a settled domain model in code, and working infrastructure wiring (database, identity, auth). Establishing this foundation correctly now prevents painful restructuring later and unblocks all subsequent feature changes.

## What Changes

- Create the .NET 10 monorepo solution with four backend projects: `Api`, `Application`, `Domain`, `Infrastructure`
- Scaffold the React 19 + Vite 8 frontend project (`Steward.Web`)
- Add integration and unit test projects
- Define all core domain entities in `Domain` using EF Core TPH inheritance for the asset hierarchy
- Configure EF Core with Npgsql (PostgreSQL) in `Infrastructure`, including the `DbContext` and all entity configurations
- Set up ASP.NET Core Identity with `ApplicationUser`, JWT Bearer authentication, and OAuth providers (Google, Facebook, Apple)
- Implement household multi-tenancy entities (`Household`, `HouseholdMembership`) and resource-based authorization infrastructure
- Add Scrutor-based DI registration, FluentValidation wiring, and OpenAPI/Scalar configuration
- Create and apply the initial EF Core migration
- Add `docker-compose.yml` for local PostgreSQL development

## Capabilities

### New Capabilities

- `solution-structure`: Monorepo layout, .NET 10 solution file, project references, `global.json`, `Directory.Build.props`, and frontend scaffold matching the my-marina conventions
- `domain-model`: Full EF Core domain entity hierarchy — Asset (TPH: Vehicle/Trailer/Equipment subtypes), Engine, Registration, Warranty, ServiceRecord, MileageLog, EngineHoursLog, FuelLog — with all entity configurations and the initial migration
- `identity-and-auth`: ASP.NET Core Identity with `ApplicationUser`, JWT Bearer token issuance, and OAuth 2.0 social login providers (Google, Facebook, Apple); PlatformAdmin role
- `household-multitenancy`: `Household` and `HouseholdMembership` entities, resource-based authorization handlers scoping all asset access to household membership roles (Owner, Contributor, Viewer)

### Modified Capabilities

*(none — greenfield project)*

## Impact

- **Entire codebase**: This is the initial scaffold; all files are new
- **Database**: PostgreSQL schema created via EF Core migration; requires a running PostgreSQL instance (provided via `docker-compose.yml` for local dev)
- **Dependencies introduced**: Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.AspNetCore.Identity.EntityFrameworkCore, Microsoft.AspNetCore.Authentication.Google/Facebook + AspNet.Security.OAuth.Apple, Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore, FluentValidation.AspNetCore, Scrutor, System.IdentityModel.Tokens.Jwt
