## 1. Repo & Solution Scaffolding

- [x] 1.1 Create `Steward.sln` and stub all four backend `.csproj` files (`Domain`, `Application`, `Infrastructure`, `Api`) with correct project references; add all to the solution [repo root]
- [x] 1.2 Add `global.json` (pin .NET 10 SDK, `rollForward: latestMinor`) and `Directory.Build.props` (`TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`) [repo root]
- [x] 1.3 Add `tests/Steward.UnitTests` and `tests/Steward.IntegrationTests` xUnit projects; add both to the solution [tests/]
- [x] 1.4 Add `docker-compose.yml` at repo root with a PostgreSQL 16 service and named volume; add `appsettings.Development.json` to `Api` project with docker-compose connection string [repo root + Api]
- [x] 1.5 Add NuGet package references to each project: Domain (none), Application (FluentValidation), Infrastructure (Npgsql.EFCore.PostgreSQL, Identity.EFCore, Auth.JwtBearer, Auth.Google, Auth.Facebook, AspNet.Security.OAuth.Apple, Scrutor, System.IdentityModel.Tokens.Jwt), Api (Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore, EFCore.Design, Asp.Versioning.Mvc, Asp.Versioning.Mvc.ApiExplorer) [all projects]

## 2. Domain — Asset Hierarchy

- [x] 2.1 Create `Asset` abstract base class with all base properties (`Id`, `HouseholdId`, `Name`, `Description`, `Year`, `PhotoUrl`, `UsageTrackingMode`, `CreatedAt`, `UpdatedAt`) and `UsageTrackingMode` enum (None | Mileage | Hours | Both) [Domain]
- [x] 2.2 Create abstract `Vehicle` class extending `Asset` (adds `Vin`, `Color`, `Make`, `Model`); add concrete leaf types: `Snowmobile` (TrackLengthIn), `Utv`, `Boat` (HIN replaces VIN, HullMaterial, LengthFt, BeamFt), `Car`, `Truck` [Domain]
- [x] 2.3 Create abstract `Trailer` class extending `Asset`; add `SnowmobileTrailer` (BallSizeIn, MaxLoadLbs) and `EnclosedTrailer` (InteriorHeightFt, InteriorLengthFt) [Domain]
- [x] 2.4 Create abstract `Equipment` class extending `Asset`; add `RidingMower` (CuttingWidthIn), `PowerWasher` (MaxPSI, MaxGPM), `SmallEngine` (EquipmentDescription) [Domain]
- [x] 2.5 Create `Engine` entity with all properties and enums: `EngineType` (ICE | Electric | Hybrid), `FuelType` (Gasoline | Diesel | TwoStroke | FourStroke | Electric | None), `EngineStatus` (Active | Retired) [Domain]

## 3. Domain — Cross-Cutting Tracking Entities

- [x] 3.1 Create `ServiceRecord` entity (`Id`, `AssetId`, `EngineId?`, `Date`, `Description`, `ProviderName?`, `Cost?`, `OdometerMiles?`, `EngineHours?`, `Notes?`) [Domain]
- [x] 3.2 Create `MileageLog` (`Id`, `AssetId`, `Date`, `OdometerReading?`, `TripMiles?`, `Notes?`) and `EngineHoursLog` (`Id`, `EngineId`, `Date`, `HoursReading?`, `TripHours?`, `Notes?`) [Domain]
- [x] 3.3 Create `FuelLog` with `FuelLogType` enum (Fillup | Consumption) and `VolumeUnit` enum (Gallons | Liters); all fuel log properties per spec [Domain]
- [x] 3.4 Create `Registration` (`Id`, `AssetId`, `RegistrationNumber`, `IssuingAuthority?`, `ExpiresOn?`, `DocumentUrl?`, `Notes?`) and `Warranty` (`Id`, `AssetId`, `Provider`, `Description?`, `StartsOn?`, `ExpiresOn?`, `DocumentUrl?`, `Notes?`) [Domain]

## 4. Domain — Household & Identity Entities

- [x] 4.1 Create `Household` entity (`Id`, `Name`, `PublicSlug`, `IsPublicVisible`, `CreatedAt`, `CreatedByUserId`) and `HouseholdMembership` entity with `HouseholdMemberRole` (Owner | Contributor | Viewer) and `HouseholdMemberStatus` (Pending | Active | Revoked) enums [Domain]
- [x] 4.2 Create `ApplicationUser` extending `IdentityUser` with `DisplayName` (string?) and `AvatarUrl` (string?) [Infrastructure]

## 5. Infrastructure — EF Core DbContext & Configuration

- [x] 5.1 Create `StewardDbContext` extending `IdentityDbContext<ApplicationUser>` with `DbSet<>` properties for all domain entities [Infrastructure]
- [x] 5.2 Configure TPH asset hierarchy in `OnModelCreating`: set discriminator column name, configure each concrete subtype's nullable-column mappings and any subtype-specific constraints [Infrastructure]
- [x] 5.3 Configure all entity relationships and FK cascade behavior: Engine → Asset (cascade delete), cross-cutting entities → Asset (restrict), HouseholdMembership → Household + ApplicationUser [Infrastructure]
- [x] 5.4 Add unique indexes: `Households.PublicSlug`, composite `(HouseholdId, UserId)` on `HouseholdMemberships`; add required-field constraints for `Asset.Name`, `Household.Name`, `Household.PublicSlug` [Infrastructure]
- [x] 5.5 Add `IServiceCollection` extension method `AddStewardDatabase(IConfiguration)` that registers `StewardDbContext` with Npgsql, reading the connection string from `"ConnectionStrings:DefaultConnection"` [Infrastructure]

## 6. Infrastructure — Authentication

- [x] 6.1 Configure ASP.NET Core Identity (`AddIdentity<ApplicationUser, IdentityRole>`) with password complexity options and lockout settings; wire to EF Core stores [Infrastructure]
- [x] 6.2 Implement `IJwtTokenService` interface (in Application) and `JwtTokenService` (in Infrastructure): generate signed HS256 JWT with `sub`, `email`, `name`, `role` claims; read signing key, issuer, audience, and expiry from `IConfiguration` [Application + Infrastructure]
- [x] 6.3 Register JWT Bearer middleware in `AddStewardAuth(IConfiguration)` extension method; configure `TokenValidationParameters` (issuer, audience, signing key, clock skew) [Infrastructure]
- [x] 6.4 Register Google, Facebook, and Apple OAuth providers in the same auth extension method; load `ClientId`/`ClientSecret` from configuration sections `Auth:Google`, `Auth:Facebook`, `Auth:Apple` [Infrastructure]

## 7. Infrastructure — Household Authorization

- [x] 7.1 Define `HouseholdOperations` static class in Application with `OperationAuthorizationRequirement` constants: `View`, `Edit`, `Delete`, `Invite` [Application]
- [x] 7.2 Define `IHouseholdResource` marker interface in Application (requires `HouseholdId` property); implement `HouseholdAuthorizationHandler` in Infrastructure that queries live `HouseholdMembership`, maps roles to operations, and short-circuits for `PlatformAdmin` role [Application + Infrastructure]
- [x] 7.3 Register `HouseholdAuthorizationHandler` as a scoped `IAuthorizationHandler` in the Infrastructure DI extension method [Infrastructure]

## 8. Api — Composition & Configuration

- [x] 8.1 Wire all service registrations in `Program.cs`: call `AddStewardDatabase`, `AddStewardAuth`, `AddApplication` (Scrutor scan + FluentValidation), add controllers, CORS policy (allow Web origin in dev), and auth/authorization middleware in correct pipeline order [Api]
- [x] 8.2 Configure API versioning via `AddApiVersioning` (DefaultApiVersion=1.0, AssumeDefaultVersionWhenUnspecified=true, ReportApiVersions=true) + `.AddMvc()` + `.AddApiExplorer(GroupNameFormat="'v'VVV", SubstituteApiVersionInUrl=true)`; all future controllers SHALL use `[ApiVersion("1.0")]` and `[Route("api/v{version:apiVersion}/[controller]")]` [Api]
- [x] 8.3 Configure `Microsoft.AspNetCore.OpenApi` with one document per API version (`AddOpenApi("v1")`); configure `Scalar.AspNetCore` to expose the v1 document with JWT Bearer security scheme; verify Scalar UI is reachable at `/scalar/v1` [Api]
- [x] 8.4 Add `appsettings.json` with configuration schema stubs for `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`, `Auth:Google`, `Auth:Facebook`, `Auth:Apple`; document required values in a root-level `README.md` [Api + repo root]

## 9. EF Core Migration

- [x] 9.1 Run `dotnet ef migrations add InitialCreate --project src/Steward.Infrastructure --startup-project src/Steward.Api` and verify the generated migration covers all tables, indexes, and Identity schema [Infrastructure]
- [x] 9.2 Add `PlatformAdmin` role seed: either in the migration's `Up()` method or as an `IHostedService` startup seed that inserts the role if absent [Infrastructure]
- [x] 9.3 Verify `dotnet ef database update` applies cleanly against the docker-compose PostgreSQL instance; confirm `__EFMigrationsHistory` records `InitialCreate` [Infrastructure]

## 10. Frontend Scaffold

- [x] 10.1 Initialize `src/Steward.Web` with Vite (React + TypeScript template); configure Tailwind CSS 4 and install shadcn/ui; commit baseline with `npm run dev` working [Web]
- [x] 10.2 Install runtime dependencies: `react-hook-form`, `zod`, `@tanstack/react-query`, `react-router`, `axios`; install dev dependencies: `vitest`, `@testing-library/react` [Web]
- [x] 10.3 Add `src/api/client.ts` (axios instance reading `VITE_API_BASE_URL` from env), placeholder `src/api/schema.d.ts`, and an npm script `generate:api` for future OpenAPI type generation (using `openapi-typescript`); add `.env.development` with `VITE_API_BASE_URL=http://localhost:5000` [Web]
