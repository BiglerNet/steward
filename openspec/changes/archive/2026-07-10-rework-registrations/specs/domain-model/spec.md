## MODIFIED Requirements

### Requirement: Asset TPH hierarchy
The system SHALL model assets using EF Core Table-Per-Hierarchy (TPH) inheritance in a single `Assets` table with a `Discriminator` column. The hierarchy SHALL contain exactly four concrete structural classes — classes exist only where the set of columns differs:

```
Asset (abstract)
├── Vehicle    — Vin, LicensePlate, Make, Model, Color, TrackLengthIn (all nullable)
├── Boat       — Hin, HullMaterial, LengthFt, BeamFt, Make, Model, Color (all nullable)
├── Trailer    — LicensePlate, BallSizeIn, MaxLoadLbs, InteriorHeightFt, InteriorLengthFt (all nullable)
└── Equipment  — CuttingWidthIn, MaxPsi, MaxGpm, EquipmentDescription (all nullable)
```

`Boat` SHALL be a sibling of `Vehicle` (not a subclass) because hull identification (HIN) does not share VIN semantics. Discriminator values SHALL be the structural class names (`Vehicle`, `Boat`, `Trailer`, `Equipment`). The user-facing leaf taxonomy (car vs. truck vs. snowmobile, etc.) SHALL be expressed by the `Category` property on `Asset`, not by subclassing.

`LicensePlate` on `Vehicle` and `Trailer` SHALL be registry-gated like every other type-specific field: applicable initially to the Road-group categories (Car, Truck, Suv, Van, Motorcycle) and all four trailer categories, and not applicable to Boat or Powersport categories.

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

---

### Requirement: Cross-cutting tracking entities
The following entities SHALL reference `AssetId` (FK → Assets) and be queryable for any asset type without knowledge of the asset's concrete type:

**ServiceRecord**: `Id`, `AssetId`, `EngineId` (nullable FK → Engines), `Date` (DateOnly), `Description` (string), `ProviderName` (string, nullable), `Cost` (decimal, nullable), `OdometerMiles` (decimal, nullable), `EngineHours` (decimal, nullable), `Notes` (string, nullable).

**MileageLog**: `Id`, `AssetId`, `Date` (DateOnly), `OdometerReading` (decimal, nullable), `TripMiles` (decimal, nullable), `Notes` (string, nullable). At least one of `OdometerReading` or `TripMiles` SHALL be non-null.

**EngineHoursLog**: `Id`, `EngineId` (FK → Engines), `Date` (DateOnly), `HoursReading` (decimal, nullable), `TripHours` (decimal, nullable), `Notes` (string, nullable). At least one of `HoursReading` or `TripHours` SHALL be non-null.

**FuelLog**: `Id`, `AssetId`, `EngineId` (nullable FK → Engines), `LogType` (enum: Fillup | Consumption), `Date` (DateOnly), `Volume` (decimal), `VolumeUnit` (enum: Gallons | Liters), `FuelGrade` (string, nullable), `PricePerUnit` (decimal, nullable), `TotalCost` (decimal, nullable), `MilesAtLog` (decimal, nullable), `HoursAtLog` (decimal, nullable), `Notes` (string, nullable).

**Registration**: `Id`, `AssetId`, `Kind` (enum `RegistrationKind`: Registration | TrailPass | Permit, required), `RegistrationNumber` (string, nullable), `IssuingAuthority` (string, nullable), `RenewedOn` (DateOnly, nullable), `ValidFrom` (DateOnly, nullable), `Cost` (decimal, nullable), `ExpiresOn` (DateOnly, nullable), `DocumentUrl` (string, nullable), `Notes` (string, nullable). Each record represents one purchased/renewed credential period; renewals accumulate as independent rows.

**Warranty**: `Id`, `AssetId`, `Provider` (string), `Description` (string, nullable), `StartsOn` (DateOnly, nullable), `ExpiresOn` (DateOnly, nullable), `DocumentUrl` (string, nullable), `Notes` (string, nullable).

`RegistrationKind` member names SHALL stay in sync with the asset type registry's `typicalPermitKinds` string values (verified by a unit test parsing every registry value into the enum).

#### Scenario: Service record targets specific engine
- **WHEN** a `ServiceRecord` is created with a non-null `EngineId`
- **THEN** it is retrievable by filtering on either `AssetId` alone or `AssetId + EngineId`

#### Scenario: Fuel fillup logged
- **WHEN** a `FuelLog` with `LogType = Fillup` is created for a boat asset with no `EngineId`
- **THEN** it represents a tank-level fillup and is queryable as part of the asset's fuel history

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

### Requirement: Initial EF Core migration
The `Infrastructure` project SHALL contain a single initial EF Core migration named `InitialCreate` that produces the complete PostgreSQL schema for all entities defined above. Pre-existing migrations SHALL be deleted and regenerated (the product is pre-launch; no data migration is required).

#### Scenario: Migration applies cleanly
- **WHEN** `dotnet ef database update` is run against a fresh PostgreSQL database
- **THEN** all tables, indexes, and constraints are created without errors and the `__EFMigrationsHistory` table records the migration

#### Scenario: Single migration after reset
- **WHEN** the migrations folder is inspected after this change
- **THEN** it contains only the regenerated `InitialCreate` migration including `Assets.LicensePlate`, `Registrations.Kind`/`ValidFrom` (with nullable `RegistrationNumber`), and `Households.Country`/`Region`
