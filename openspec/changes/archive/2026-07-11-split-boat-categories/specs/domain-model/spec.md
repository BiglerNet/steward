## MODIFIED Requirements

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
