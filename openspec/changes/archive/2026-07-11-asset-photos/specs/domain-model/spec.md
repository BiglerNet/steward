## MODIFIED Requirements

### Requirement: Asset base properties
Every `Asset` SHALL have: `Id` (Guid), `HouseholdId` (Guid, FK), `Category` (enum `AssetCategory`, required), `Name` (string, required), `Description` (string, nullable), `Year` (int, nullable), `CoverPhotoId` (Guid, nullable FK → AssetPhotos), `UsageTrackingMode` (enum: None | Mileage | Hours | Both), `CreatedAt` (DateTimeOffset), `UpdatedAt` (DateTimeOffset). The former `PhotoUrl` property SHALL be removed.

`AssetCategory` SHALL initially contain: Car, Truck, Suv, Van, Motorcycle, Utv, Atv, Snowmobile, DirtBike, GolfCart, Boat, Pwc, UtilityTrailer, EnclosedTrailer, SnowmobileTrailer, BoatTrailer, RidingMower, PowerWasher, Generator, SmallEngine. Every `AssetCategory` value SHALL map to exactly one structural class via the asset type registry.

#### Scenario: Required fields enforced at DB level
- **WHEN** an attempt is made to insert an `Asset` row with a null `Name`
- **THEN** a database constraint violation is thrown

#### Scenario: Category persisted on the base table
- **WHEN** an asset is created with `Category = Utv`
- **THEN** the `Assets` row stores the category value and it round-trips through EF Core unchanged

#### Scenario: Cover photo pointer round-trips
- **WHEN** an asset's `CoverPhotoId` is set to one of its `AssetPhoto` ids and saved
- **THEN** the value round-trips through EF Core, and no `PhotoUrl` column exists on the `Assets` table

---

### Requirement: Initial EF Core migration
The `Infrastructure` project SHALL contain a single initial EF Core migration named `InitialCreate` that produces the complete PostgreSQL schema for all entities defined above. Pre-existing migrations SHALL be deleted and regenerated (the product is pre-launch; no data migration is required).

#### Scenario: Migration applies cleanly
- **WHEN** `dotnet ef database update` is run against a fresh PostgreSQL database
- **THEN** all tables, indexes, and constraints are created without errors and the `__EFMigrationsHistory` table records the migration

#### Scenario: Single migration after reset
- **WHEN** the migrations folder is inspected after this change
- **THEN** it contains only the regenerated `InitialCreate` migration including the `AssetPhotos` table, `Assets.CoverPhotoId`, and `Households.StorageUsedBytes`/`StorageQuotaOverrideBytes`, with no `Assets.PhotoUrl` column

## ADDED Requirements

### Requirement: AssetPhoto entity
An `AssetPhoto` SHALL be a standalone entity associated with an `Asset` via `AssetId` (FK); an asset MAY have zero or more photos. Properties: `Id` (Guid), `AssetId` (Guid, FK), `ThumbStorageKey` (string, required), `DisplayStorageKey` (string, required), `Width` (int), `Height` (int — display-variant dimensions after orientation), `SizeBytes` (long — total stored bytes across both variants), `CreatedAt` (DateTimeOffset). Deleting an asset SHALL delete its photos.

#### Scenario: Photos cascade with their asset
- **WHEN** an asset with three `AssetPhoto` rows is deleted
- **THEN** all three photo rows are removed with it

#### Scenario: Photo metadata persisted
- **WHEN** an `AssetPhoto` is saved with `Width = 2048`, `Height = 1536`, `SizeBytes = 850000`
- **THEN** all values round-trip through EF Core unchanged
