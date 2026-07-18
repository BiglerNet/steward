## MODIFIED Requirements

### Requirement: Cross-cutting tracking entities
The following entities SHALL reference `AssetId` (FK → Assets) and be queryable for any asset type without knowledge of the asset's concrete type:

**MaintenanceItem**: `Id`, `AssetId`, `EngineId` (nullable FK → Engines), `TemplateId` (nullable FK → Templates), `Title` (string, required), `Description` (string, nullable, markdown), `ProviderName` (string, nullable), `Status` (enum: `Planned` | `InProgress` | `Done` | `Cancelled`, required, default `Planned`), `Date` (DateOnly, nullable), `Cost` (decimal, nullable), `OdometerMiles` (decimal, nullable), `EngineHours` (decimal, nullable). There is no stored "Blocked" status — it is derived at read time from whether the item has any `PartLine` with `Status` of `Needed` or `Ordered`.

**ChecklistItem**: `Id`, `MaintenanceItemId` (FK → MaintenanceItems), `Text` (string, required), `Status` (enum: `Open` | `Done` | `Skipped`, required, default `Open`), `ResolvedAt` (DateTimeOffset, nullable — set when `Status` becomes `Done` or `Skipped`, cleared when it returns to `Open`), `SortOrder` (int), `EngineId` (nullable FK → Engines, must belong to the same `AssetId` as the parent `MaintenanceItem`), `TemplateStepId` (nullable FK → TemplateSteps).

**PartLine**: `Id`, `MaintenanceItemId` (FK → MaintenanceItems), `Name` (string, required), `PartNumber` (string, nullable), `Vendor` (string, nullable), `TrackingNumber` (string, nullable), `OrderUrl` (string, nullable), `Quantity` (decimal, required, default 1), `Status` (enum: `Needed` | `Ordered` | `Received`, required, default `Needed`), `Cost` (decimal, nullable), `ChecklistItemId` (nullable FK → ChecklistItems, must belong to the same `MaintenanceItem`), `PartId` (nullable FK → Parts).

**Part**: `Id`, `HouseholdId` (FK → Households), `Name` (string, required), `PartNumber` (string, nullable), `DefaultVendor` (string, nullable). This entity has no reads or writes anywhere in the system yet beyond `PartLine.PartId` referencing it — it exists solely as a reserved schema seam for a future parts-inventory capability.

**MileageLog**: `Id`, `AssetId`, `Date` (DateOnly), `OdometerReading` (decimal, nullable), `TripMiles` (decimal, nullable), `Notes` (string, nullable). At least one of `OdometerReading` or `TripMiles` SHALL be non-null.

**EngineHoursLog**: `Id`, `EngineId` (FK → Engines), `Date` (DateOnly), `HoursReading` (decimal, nullable), `TripHours` (decimal, nullable), `Notes` (string, nullable). At least one of `HoursReading` or `TripHours` SHALL be non-null.

**FuelLog**: `Id`, `AssetId`, `EngineId` (nullable FK → Engines), `LogType` (enum: Fillup | Consumption), `Date` (DateOnly), `Quantity` (decimal), `Unit` (enum: `Gallons` | `Liters` | `Kwh`), `FuelGrade` (string, nullable), `PricePerUnit` (decimal, nullable), `TotalCost` (decimal, nullable), `MilesAtLog` (decimal, nullable), `HoursAtLog` (decimal, nullable), `Notes` (string, nullable).

**Registration**: `Id`, `AssetId`, `Kind` (enum `RegistrationKind`: Registration | TrailPass | Permit, required), `RegistrationNumber` (string, nullable), `IssuingAuthority` (string, nullable), `RenewedOn` (DateOnly, nullable), `ValidFrom` (DateOnly, nullable), `Cost` (decimal, nullable), `ExpiresOn` (DateOnly, nullable), `DocumentUrl` (string, nullable), `Notes` (string, nullable). Each record represents one purchased/renewed credential period; renewals accumulate as independent rows.

**Warranty**: `Id`, `AssetId`, `Provider` (string), `Description` (string, nullable), `StartsOn` (DateOnly, nullable), `ExpiresOn` (DateOnly, nullable), `DocumentUrl` (string, nullable), `Notes` (string, nullable).

`RegistrationKind` member names SHALL stay in sync with the asset type registry's `typicalPermitKinds` string values (verified by a unit test parsing every registry value into the enum).

#### Scenario: Maintenance item targets a specific engine
- **WHEN** a `MaintenanceItem` is created with a non-null `EngineId`
- **THEN** it is retrievable by filtering on either `AssetId` alone or `AssetId + EngineId`

#### Scenario: Checklist item scoped to one engine, independent of its sibling
- **WHEN** a `MaintenanceItem` has two `ChecklistItem`s with the same `Text` but different `EngineId` values (e.g. "Change oil" for a port and a starboard engine)
- **THEN** each `ChecklistItem`'s `Status` and `ResolvedAt` can be set independently without affecting the other

#### Scenario: Part line keeps its own descriptive fields regardless of catalog link
- **WHEN** a `PartLine` is created with `PartId` set to an existing `Part` and its own `Name`/`PartNumber`/`Vendor` values
- **THEN** the `PartLine`'s own `Name`/`PartNumber`/`Vendor` persist and remain readable even if the linked `Part` is later renamed or deleted

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

## ADDED Requirements

### Requirement: Template entity
A `Template` SHALL be a standalone entity, scoped either to a household or to the platform. Properties: `Id`, `HouseholdId` (nullable FK → Households — `null` means a platform-owned template), `Title` (string, required), `Description` (string, nullable, markdown), `ApplicableCategories` (array of `AssetCategory`, stored as a native Postgres array column — empty or null means applicable to any category).

A `TemplateStep` SHALL be a standalone entity associated with a `Template` via `TemplateId` (FK); a template MAY have zero or more steps. Properties: `Id`, `TemplateId`, `Text` (string, required), `SortOrder` (int), `EngineScoped` (bool, default false — whether applying the template expands this step into one `ChecklistItem` per active `Engine` on the target asset, versus a single asset-level `ChecklistItem`), `RecurrenceIntervalMonths` (int, nullable), `RecurrenceIntervalMiles` (decimal, nullable), `RecurrenceIntervalHours` (decimal, nullable) — a step is due again at whichever configured threshold is reached first; all null means the step carries no recurrence tracking.

A `TemplateStepSuggestedPart` SHALL be a standalone entity associated with a `TemplateStep` via `TemplateStepId` (FK); a step MAY have zero or more suggested parts. Properties: `Id`, `TemplateStepId`, `Name` (string, required), `Quantity` (decimal, required, default 1), `SortOrder` (int). These are copied into fresh `PartLine`s at `Status = Needed` when a `MaintenanceItem` is created from the template; not a live reference.

#### Scenario: Platform template has no household
- **WHEN** a `Template` is created with `HouseholdId = null`
- **THEN** it persists and is distinguishable from household-owned templates by that null value

#### Scenario: Applicable categories round-trip
- **WHEN** a `Template` is saved with `ApplicableCategories = [Car, Truck, PowerBoat]`
- **THEN** the array persists and round-trips through EF Core unchanged

#### Scenario: Engine-scoped step carries an interval
- **WHEN** a `TemplateStep` is saved with `EngineScoped = true`, `RecurrenceIntervalMonths = 12`, `RecurrenceIntervalHours = 100`
- **THEN** both interval values persist and round-trip through EF Core

#### Scenario: Suggested parts round-trip
- **WHEN** a `TemplateStep` is saved with two `TemplateStepSuggestedPart` rows, `{ Name: "Oil filter", Quantity: 1 }` and `{ Name: "5W-30 oil", Quantity: 5 }`
- **THEN** both rows persist, are retrievable ordered by `SortOrder`, and round-trip through EF Core unchanged
