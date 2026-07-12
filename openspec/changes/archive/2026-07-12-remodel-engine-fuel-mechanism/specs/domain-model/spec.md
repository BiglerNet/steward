## MODIFIED Requirements

### Requirement: Engine entity
An `Engine` SHALL be a standalone entity associated with an `Asset` via `AssetId` (FK). An asset MAY have zero or more engines.

Engine properties: `Id` (Guid), `AssetId` (Guid, FK), `Label` (string, required — e.g., "Port Motor", "Generator"), `Make` (string, nullable), `Model` (string, nullable), `SerialNumber` (string, nullable), `Year` (int, nullable), `EngineType` (enum: `Ice` | `Electric`), `Mechanism` (enum, nullable: `TwoStroke` | `FourStroke` | `Diesel` | `Rotary` — applicable only when `EngineType = Ice`), `FuelType` (enum, nullable: `Gasoline` | `Diesel` | `Propane` — applicable only when `EngineType = Ice`), `IsExternallyChargeable` (bool, nullable — applicable only when `EngineType = Electric`; `true` means the motor/battery is refueled from outside the asset such as a BEV or PHEV, `false` means it is only ever charged internally via regenerative braking or the ICE), `TwoStrokeOilDelivery` (enum, nullable: `Premix` | `OilInjected` — applicable only when `Mechanism = TwoStroke`), `TwoStrokeMixRatio` (string, nullable — applicable only when `Mechanism = TwoStroke`, e.g. `"50:1"`), `Cylinders` (int, nullable), `DisplacementCC` (decimal, nullable), `Status` (enum: `Active` | `Retired` | `Broken`), `InstalledDate` (DateOnly, nullable), `InstalledAtAssetMiles` (decimal, nullable), `InstalledAtAssetHours` (decimal, nullable).

`EngineType`, `Mechanism`, `FuelType`, and `TwoStrokeOilDelivery` SHALL be persisted as strings, not integer ordinals.

`Hybrid` SHALL NOT be a valid `EngineType` value. A hybrid asset SHALL be represented as two `Engine` records sharing the same `AssetId` — one with `EngineType = Ice`, one with `EngineType = Electric` — rather than a single record.

#### Scenario: Engine retirement preserves history
- **WHEN** an engine's `Status` is set to `Retired` and a new engine is created with `Status = Active`
- **THEN** both engine records exist in the database and all `EngineHoursLog` entries for the retired engine remain intact and queryable

#### Scenario: Multiple engines per asset
- **WHEN** a `Boat` asset has two `Engine` records (Port and Starboard)
- **THEN** querying `dbContext.Engines.Where(e => e.AssetId == boatId)` returns both engines

#### Scenario: Two-stroke oil fields round-trip
- **WHEN** an `Engine` with `Mechanism = TwoStroke` is saved with `TwoStrokeOilDelivery = OilInjected` and `TwoStrokeMixRatio = "50:1"`
- **THEN** both values persist and round-trip through EF Core unchanged

#### Scenario: Hybrid vehicle modeled as two engines, not one
- **WHEN** a `Car` asset needs to represent a conventional (non-plug-in) hybrid powertrain
- **THEN** it is represented as two `Engine` records sharing the asset's `AssetId` — one `EngineType = Ice`, one `EngineType = Electric` with `IsExternallyChargeable = false` — and no `Hybrid` `EngineType` value exists anywhere in the schema

### Requirement: Cross-cutting tracking entities
The following entities SHALL reference `AssetId` (FK → Assets) and be queryable for any asset type without knowledge of the asset's concrete type:

**ServiceRecord**: `Id`, `AssetId`, `EngineId` (nullable FK → Engines), `Date` (DateOnly), `Description` (string), `ProviderName` (string, nullable), `Cost` (decimal, nullable), `OdometerMiles` (decimal, nullable), `EngineHours` (decimal, nullable), `Notes` (string, nullable).

**MileageLog**: `Id`, `AssetId`, `Date` (DateOnly), `OdometerReading` (decimal, nullable), `TripMiles` (decimal, nullable), `Notes` (string, nullable). At least one of `OdometerReading` or `TripMiles` SHALL be non-null.

**EngineHoursLog**: `Id`, `EngineId` (FK → Engines), `Date` (DateOnly), `HoursReading` (decimal, nullable), `TripHours` (decimal, nullable), `Notes` (string, nullable). At least one of `HoursReading` or `TripHours` SHALL be non-null.

**FuelLog**: `Id`, `AssetId`, `EngineId` (nullable FK → Engines), `LogType` (enum: Fillup | Consumption), `Date` (DateOnly), `Quantity` (decimal), `Unit` (enum: `Gallons` | `Liters` | `Kwh`), `FuelGrade` (string, nullable), `PricePerUnit` (decimal, nullable), `TotalCost` (decimal, nullable), `MilesAtLog` (decimal, nullable), `HoursAtLog` (decimal, nullable), `Notes` (string, nullable).

**Registration**: `Id`, `AssetId`, `Kind` (enum `RegistrationKind`: Registration | TrailPass | Permit, required), `RegistrationNumber` (string, nullable), `IssuingAuthority` (string, nullable), `RenewedOn` (DateOnly, nullable), `ValidFrom` (DateOnly, nullable), `Cost` (decimal, nullable), `ExpiresOn` (DateOnly, nullable), `DocumentUrl` (string, nullable), `Notes` (string, nullable). Each record represents one purchased/renewed credential period; renewals accumulate as independent rows.

**Warranty**: `Id`, `AssetId`, `Provider` (string), `Description` (string, nullable), `StartsOn` (DateOnly, nullable), `ExpiresOn` (DateOnly, nullable), `DocumentUrl` (string, nullable), `Notes` (string, nullable).

`RegistrationKind` member names SHALL stay in sync with the asset type registry's `typicalPermitKinds` string values (verified by a unit test parsing every registry value into the enum).

#### Scenario: Service record targets specific engine
- **WHEN** a `ServiceRecord` is created with a non-null `EngineId`
- **THEN** it is retrievable by filtering on either `AssetId` alone or `AssetId + EngineId`

#### Scenario: Fuel fillup logged
- **WHEN** a `FuelLog` with `LogType = Fillup` is created for a boat asset with no `EngineId`, `Quantity = 40`, `Unit = Gallons`
- **THEN** it represents a tank-level fillup and is queryable as part of the asset's fuel history

#### Scenario: Electric charging event logged
- **WHEN** a `FuelLog` is created for an asset's `Electric` engine with `Quantity = 62`, `Unit = Kwh`
- **THEN** it persists and round-trips through EF Core, queryable as part of the asset's fuel/energy history alongside any `Gallons`/`Liters` entries for a different engine on the same asset

#### Scenario: Registration expiry tracked
- **WHEN** a `Registration` with `ExpiresOn` set to a past date is queried
- **THEN** it is identifiable as expired by comparing `ExpiresOn` to the current date in application logic

#### Scenario: Trail pass stored as a registration kind
- **WHEN** a `Registration` is created with `Kind = TrailPass`, `ValidFrom = 2026-01-01`, `ExpiresOn = 2026-01-07`, and no `RegistrationNumber`
- **THEN** it persists alongside the asset's `Kind = Registration` rows and both are returned by asset-scoped queries

#### Scenario: Permit kind strings parse to the enum
- **WHEN** the sync unit test parses every `typicalPermitKinds` value present in the asset type registry
- **THEN** each value matches a `RegistrationKind` member name exactly

### Requirement: Initial EF Core migration
The `Infrastructure` project SHALL contain a single initial EF Core migration named `InitialCreate` that produces the complete PostgreSQL schema for all entities defined above, including the revised `Engine` and `FuelLog` shapes. Pre-existing migrations SHALL be deleted and regenerated (the product is pre-launch; no data migration is required).

#### Scenario: Migration applies cleanly
- **WHEN** `dotnet ef database update` is run against a fresh PostgreSQL database
- **THEN** all tables, indexes, and constraints are created without errors and the `__EFMigrationsHistory` table records the migration

#### Scenario: Single migration after reset
- **WHEN** the migrations folder is inspected after this change
- **THEN** it contains only the regenerated `InitialCreate` migration including the `AssetPhotos` table, `Assets.CoverPhotoId`, `Households.StorageUsedBytes`/`StorageQuotaOverrideBytes`, the revised `Engine` columns (`Mechanism`, `FuelType`, `IsExternallyChargeable`, `TwoStrokeOilDelivery`, `TwoStrokeMixRatio`), and the revised `FuelLog` columns (`Quantity`, `Unit`), with no `Assets.PhotoUrl`, `Engine.Hybrid`-valued rows, or `FuelLog.Volume`/`VolumeUnit` columns
