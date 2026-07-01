### Requirement: Asset TPH hierarchy
The system SHALL model assets using EF Core Table-Per-Hierarchy (TPH) inheritance in a single `Assets` table with a `Discriminator` column. The hierarchy SHALL be:

```
Asset (abstract)
├── Vehicle (abstract) — VIN/HIN, UsageTrackingMode
│   ├── Snowmobile
│   ├── Utv
│   ├── Boat          — uses HIN instead of VIN
│   ├── Car
│   └── Truck
├── Trailer (abstract)
│   ├── SnowmobileTrailer
│   └── EnclosedTrailer
└── Equipment (abstract)
    ├── RidingMower
    ├── PowerWasher
    └── SmallEngine
```

All `Asset` records SHALL belong to a `Household` via a non-nullable `HouseholdId` foreign key.

#### Scenario: Asset discriminator persisted
- **WHEN** a `Snowmobile` entity is saved via EF Core
- **THEN** the `Assets` table row has `Discriminator = 'Snowmobile'` and all nullable columns for other subtypes are `NULL`

#### Scenario: Cross-type query
- **WHEN** a LINQ query filters `dbContext.Assets.Where(a => a.HouseholdId == id)`
- **THEN** it returns all asset subtypes belonging to that household in a single SQL query against the `Assets` table

---

### Requirement: Asset base properties
Every `Asset` SHALL have: `Id` (Guid), `HouseholdId` (Guid, FK), `Name` (string, required), `Description` (string, nullable), `Year` (int, nullable), `PhotoUrl` (string, nullable), `UsageTrackingMode` (enum: None | Mileage | Hours | Both), `CreatedAt` (DateTimeOffset), `UpdatedAt` (DateTimeOffset).

#### Scenario: Required fields enforced at DB level
- **WHEN** an attempt is made to insert an `Asset` row with a null `Name`
- **THEN** a database constraint violation is thrown

---

### Requirement: Engine entity
An `Engine` SHALL be a standalone entity associated with an `Asset` via `AssetId` (FK). An asset MAY have zero or more engines.

Engine properties: `Id` (Guid), `AssetId` (Guid, FK), `Label` (string, required — e.g., "Port Motor", "Generator"), `Make` (string, nullable), `Model` (string, nullable), `SerialNumber` (string, nullable), `Year` (int, nullable), `EngineType` (enum: ICE | Electric | Hybrid), `FuelType` (enum: Gasoline | Diesel | TwoStroke | FourStroke | Electric | None), `Cylinders` (int, nullable), `DisplacementCC` (decimal, nullable), `Status` (enum: Active | Retired), `InstalledDate` (DateOnly, nullable), `InstalledAtAssetMiles` (decimal, nullable), `InstalledAtAssetHours` (decimal, nullable).

#### Scenario: Engine retirement preserves history
- **WHEN** an engine's `Status` is set to `Retired` and a new engine is created with `Status = Active`
- **THEN** both engine records exist in the database and all `EngineHoursLog` entries for the retired engine remain intact and queryable

#### Scenario: Multiple engines per asset
- **WHEN** a `Boat` asset has two `Engine` records (Port and Starboard)
- **THEN** querying `dbContext.Engines.Where(e => e.AssetId == boatId)` returns both engines

---

### Requirement: Cross-cutting tracking entities
The following entities SHALL reference `AssetId` (FK → Assets) and be queryable for any asset type without knowledge of the asset's concrete type:

**ServiceRecord**: `Id`, `AssetId`, `EngineId` (nullable FK → Engines), `Date` (DateOnly), `Description` (string), `ProviderName` (string, nullable), `Cost` (decimal, nullable), `OdometerMiles` (decimal, nullable), `EngineHours` (decimal, nullable), `Notes` (string, nullable).

**MileageLog**: `Id`, `AssetId`, `Date` (DateOnly), `OdometerReading` (decimal, nullable), `TripMiles` (decimal, nullable), `Notes` (string, nullable). At least one of `OdometerReading` or `TripMiles` SHALL be non-null.

**EngineHoursLog**: `Id`, `EngineId` (FK → Engines), `Date` (DateOnly), `HoursReading` (decimal, nullable), `TripHours` (decimal, nullable), `Notes` (string, nullable). At least one of `HoursReading` or `TripHours` SHALL be non-null.

**FuelLog**: `Id`, `AssetId`, `EngineId` (nullable FK → Engines), `LogType` (enum: Fillup | Consumption), `Date` (DateOnly), `Volume` (decimal), `VolumeUnit` (enum: Gallons | Liters), `FuelGrade` (string, nullable), `PricePerUnit` (decimal, nullable), `TotalCost` (decimal, nullable), `MilesAtLog` (decimal, nullable), `HoursAtLog` (decimal, nullable), `Notes` (string, nullable).

**Registration**: `Id`, `AssetId`, `RegistrationNumber` (string), `IssuingAuthority` (string, nullable), `ExpiresOn` (DateOnly, nullable), `DocumentUrl` (string, nullable), `Notes` (string, nullable).

**Warranty**: `Id`, `AssetId`, `Provider` (string), `Description` (string, nullable), `StartsOn` (DateOnly, nullable), `ExpiresOn` (DateOnly, nullable), `DocumentUrl` (string, nullable), `Notes` (string, nullable).

#### Scenario: Service record targets specific engine
- **WHEN** a `ServiceRecord` is created with a non-null `EngineId`
- **THEN** it is retrievable by filtering on either `AssetId` alone or `AssetId + EngineId`

#### Scenario: Fuel fillup logged
- **WHEN** a `FuelLog` with `LogType = Fillup` is created for a boat asset with no `EngineId`
- **THEN** it represents a tank-level fillup and is queryable as part of the asset's fuel history

#### Scenario: Registration expiry tracked
- **WHEN** a `Registration` with `ExpiresOn` set to a past date is queried
- **THEN** it is identifiable as expired by comparing `ExpiresOn` to the current date in application logic

---

### Requirement: Initial EF Core migration
The `Infrastructure` project SHALL contain an initial EF Core migration named `InitialCreate` that produces the complete PostgreSQL schema for all entities defined above.

#### Scenario: Migration applies cleanly
- **WHEN** `dotnet ef database update` is run against a fresh PostgreSQL database
- **THEN** all tables, indexes, and constraints are created without errors and the `__EFMigrationsHistory` table records the migration
