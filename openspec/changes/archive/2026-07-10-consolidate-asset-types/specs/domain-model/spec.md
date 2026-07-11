## MODIFIED Requirements

### Requirement: Asset TPH hierarchy
The system SHALL model assets using EF Core Table-Per-Hierarchy (TPH) inheritance in a single `Assets` table with a `Discriminator` column. The hierarchy SHALL contain exactly four concrete structural classes — classes exist only where the set of columns differs:

```
Asset (abstract)
├── Vehicle    — Vin, Make, Model, Color, TrackLengthIn (all nullable)
├── Boat       — Hin, HullMaterial, LengthFt, BeamFt, Make, Model, Color (all nullable)
├── Trailer    — BallSizeIn, MaxLoadLbs, InteriorHeightFt, InteriorLengthFt (all nullable)
└── Equipment  — CuttingWidthIn, MaxPsi, MaxGpm, EquipmentDescription (all nullable)
```

`Boat` SHALL be a sibling of `Vehicle` (not a subclass) because hull identification (HIN) does not share VIN semantics. Discriminator values SHALL be the structural class names (`Vehicle`, `Boat`, `Trailer`, `Equipment`). The user-facing leaf taxonomy (car vs. truck vs. snowmobile, etc.) SHALL be expressed by the `Category` property on `Asset`, not by subclassing.

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

---

### Requirement: Asset base properties
Every `Asset` SHALL have: `Id` (Guid), `HouseholdId` (Guid, FK), `Category` (enum `AssetCategory`, required), `Name` (string, required), `Description` (string, nullable), `Year` (int, nullable), `PhotoUrl` (string, nullable), `UsageTrackingMode` (enum: None | Mileage | Hours | Both), `CreatedAt` (DateTimeOffset), `UpdatedAt` (DateTimeOffset).

`AssetCategory` SHALL initially contain: Car, Truck, Suv, Van, Motorcycle, Utv, Atv, Snowmobile, DirtBike, GolfCart, Boat, Pwc, UtilityTrailer, EnclosedTrailer, SnowmobileTrailer, BoatTrailer, RidingMower, PowerWasher, Generator, SmallEngine. Every `AssetCategory` value SHALL map to exactly one structural class via the asset type registry.

#### Scenario: Required fields enforced at DB level
- **WHEN** an attempt is made to insert an `Asset` row with a null `Name`
- **THEN** a database constraint violation is thrown

#### Scenario: Category persisted on the base table
- **WHEN** an asset is created with `Category = Utv`
- **THEN** the `Assets` row stores the category value and it round-trips through EF Core unchanged

---

### Requirement: Initial EF Core migration
The `Infrastructure` project SHALL contain a single initial EF Core migration named `InitialCreate` that produces the complete PostgreSQL schema for all entities defined above. Pre-existing migrations from the prior asset hierarchy SHALL be deleted and regenerated (the product is pre-launch; no data migration is required).

#### Scenario: Migration applies cleanly
- **WHEN** `dotnet ef database update` is run against a fresh PostgreSQL database
- **THEN** all tables, indexes, and constraints are created without errors and the `__EFMigrationsHistory` table records the migration

#### Scenario: Single migration after reset
- **WHEN** the migrations folder is inspected after this change
- **THEN** it contains only the regenerated `InitialCreate` migration reflecting the four-class structural hierarchy
