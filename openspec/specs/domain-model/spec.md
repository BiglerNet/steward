# domain-model Specification

## Purpose
Defines the core domain entities (Asset hierarchy, Engine, tracking records) and their EF Core persistence shape.

## Requirements
### Requirement: Asset TPH hierarchy
The system SHALL model assets using EF Core Table-Per-Hierarchy (TPH) inheritance in a single `Assets` table with a `Discriminator` column. The hierarchy SHALL contain exactly four concrete structural classes — classes exist only where the set of columns differs:

```
Asset (abstract)
├── Vehicle    — Vin, LicensePlate, Make, Model, Color, TrackLengthIn (all nullable)
├── Boat       — Hin, HullMaterial, HullType, DriveType, KeelType, MastHeightFt, MastCount,
│                LengthFt, BeamFt, Make, Model, Color (all nullable)
├── Trailer    — LicensePlate, BallSizeIn, MaxLoadLbs, InteriorHeightFt, InteriorLengthFt (all nullable)
└── Equipment  — CuttingWidthIn, MaxPsi, MaxGpm, EquipmentDescription (all nullable)
```

`Boat` SHALL be a sibling of `Vehicle` (not a subclass) because hull identification (HIN) does not share VIN semantics. Discriminator values SHALL be the structural class names (`Vehicle`, `Boat`, `Trailer`, `Equipment`). The user-facing leaf taxonomy (car vs. truck vs. snowmobile, etc.) SHALL be expressed by the `Category` property on `Asset`, not by subclassing.

`HullType` SHALL be an enum (Monohull | Catamaran | Trimaran | Pontoon | Other) and `DriveType` an enum (Inboard | Outboard | SternDrive | JetDrive), both stored as int per the repo convention. `KeelType` SHALL be free text. Boat-specific fields SHALL be registry-gated per category: `driveType` applicable to PowerBoat, `keelType`/`mastHeightFt`/`mastCount` applicable to Sailboat, `hullType` applicable to both, and none of them applicable to Pwc.

`LicensePlate` on `Vehicle` and `Trailer` SHALL be registry-gated like every other type-specific field: applicable initially to the Road-group categories (Car, Truck, Suv, Van, Motorcycle) and all four trailer categories, and not applicable to Boat-structural or Powersport categories.

All `Asset` records SHALL belong to a `Household` via a non-nullable `HouseholdId` foreign key.

#### Scenario: Asset discriminator persisted
- **WHEN** an asset with `Category = Snowmobile` is saved via EF Core
- **THEN** the `Assets` table row has `Discriminator = 'Vehicle'`, `Category = Snowmobile`, and all columns belonging to other structural types are `NULL`

#### Scenario: Cross-type query
- **WHEN** a LINQ query filters `dbContext.Assets.Where(a => a.HouseholdId == id)`
- **THEN** it returns all asset structural types belonging to that household in a single SQL query against the `Assets` table

#### Scenario: New category requires no new class
- **WHEN** a new user-facing asset kind matching an existing structural shape is introduced (e.g. a motorcycle, which is structurally a `Vehicle`)
- **THEN** it is added as an `AssetCategory` enum value plus an asset type registry entry, with no new entity class and no discriminator mapping change

#### Scenario: License plate stored on the asset
- **WHEN** an asset with `Category = Car` is saved with `LicensePlate = "ABC-1234"`
- **THEN** the plate persists on the `Assets` row and round-trips through EF Core, while a `Category = Snowmobile` asset submitting a non-null `licensePlate` is rejected by the existing inapplicable-field validation

#### Scenario: Sailboat rig fields round-trip
- **WHEN** a `Category = Sailboat` asset is saved with `KeelType = "Fin"`, `MastHeightFt = 42`, `MastCount = 1`, `HullType = Monohull`
- **THEN** the values persist on the `Assets` row and round-trip through EF Core

#### Scenario: Drive type rejected on a sailboat
- **WHEN** a `Category = Sailboat` asset submits a non-null `driveType`
- **THEN** the existing registry-driven inapplicable-field validation rejects it

---

### Requirement: Asset base properties
Every `Asset` SHALL have: `Id` (Guid), `HouseholdId` (Guid, FK), `Category` (enum `AssetCategory`, required), `Name` (string, required), `Description` (string, nullable), `Year` (int, nullable), `CoverPhotoId` (Guid, nullable FK → AssetPhotos), `UsageTrackingMode` (enum: None | Mileage | Hours | Both), `CreatedAt` (DateTimeOffset), `UpdatedAt` (DateTimeOffset). The former `PhotoUrl` property SHALL be removed.

`AssetCategory` SHALL contain: Car, Truck, Suv, Van, Motorcycle, Utv, Atv, Snowmobile, DirtBike, GolfCart, PowerBoat, Sailboat, Pwc, UtilityTrailer, EnclosedTrailer, SnowmobileTrailer, BoatTrailer, RidingMower, PowerWasher, Generator, SmallEngine. The former `Boat` value SHALL be removed, replaced by `PowerBoat` and `Sailboat` (both mapping to the `Boat` structural class). Every `AssetCategory` value SHALL map to exactly one structural class via the asset type registry.

#### Scenario: Required fields enforced at DB level
- **WHEN** an attempt is made to insert an `Asset` row with a null `Name`
- **THEN** a database constraint violation is thrown

#### Scenario: Category persisted on the base table
- **WHEN** an asset is created with `Category = Utv`
- **THEN** the `Assets` row stores the category value and it round-trips through EF Core unchanged

#### Scenario: Boat category no longer exists
- **WHEN** the registry completeness test enumerates `AssetCategory`
- **THEN** `PowerBoat` and `Sailboat` each have exactly one registry entry and no `Boat` value or entry exists

#### Scenario: Cover photo pointer round-trips
- **WHEN** an asset's `CoverPhotoId` is set to one of its `AssetPhoto` ids and saved
- **THEN** the value round-trips through EF Core, and no `PhotoUrl` column exists on the `Assets` table

---

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

---

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

---

### Requirement: AssetPhoto entity
An `AssetPhoto` SHALL be a standalone entity associated with an `Asset` via `AssetId` (FK); an asset MAY have zero or more photos. Properties: `Id` (Guid), `AssetId` (Guid, FK), `ThumbStorageKey` (string, required), `DisplayStorageKey` (string, required), `Width` (int), `Height` (int — display-variant dimensions after orientation), `SizeBytes` (long — total stored bytes across both variants), `CreatedAt` (DateTimeOffset). Deleting an asset SHALL delete its photos.

#### Scenario: Photos cascade with their asset
- **WHEN** an asset with three `AssetPhoto` rows is deleted
- **THEN** all three photo rows are removed with it

#### Scenario: Photo metadata persisted
- **WHEN** an `AssetPhoto` is saved with `Width = 2048`, `Height = 1536`, `SizeBytes = 850000`
- **THEN** all values round-trip through EF Core unchanged

---

### Requirement: Initial EF Core migration
The `Infrastructure` project SHALL contain a single initial EF Core migration named `InitialCreate` that produces the complete PostgreSQL schema for all entities defined above, including the revised `Engine` and `FuelLog` shapes. Pre-existing migrations SHALL be deleted and regenerated (the product is pre-launch; no data migration is required).

#### Scenario: Migration applies cleanly
- **WHEN** `dotnet ef database update` is run against a fresh PostgreSQL database
- **THEN** all tables, indexes, and constraints are created without errors and the `__EFMigrationsHistory` table records the migration

#### Scenario: Single migration after reset
- **WHEN** the migrations folder is inspected after this change
- **THEN** it contains only the regenerated `InitialCreate` migration including the `AssetPhotos` table, `Assets.CoverPhotoId`, `Households.StorageUsedBytes`/`StorageQuotaOverrideBytes`, the revised `Engine` columns (`Mechanism`, `FuelType`, `IsExternallyChargeable`, `TwoStrokeOilDelivery`, `TwoStrokeMixRatio`), and the revised `FuelLog` columns (`Quantity`, `Unit`), with no `Assets.PhotoUrl`, `Engine.Hybrid`-valued rows, or `FuelLog.Volume`/`VolumeUnit` columns
