## MODIFIED Requirements

### Requirement: Household entity
A `Household` SHALL represent a named group of assets (a "garage") owned and shared by one or more users. A Household SHALL have: `Id` (Guid), `Name` (string, required), `PublicSlug` (string, unique, URL-safe), `IsPublicVisible` (bool, default false), `Country` (string, nullable — ISO 3166-1 alpha-2, e.g. `US`), `Region` (string, nullable — full ISO 3166-2, e.g. `US-WI`), `StorageUsedBytes` (long, default 0), `StorageQuotaOverrideBytes` (long, nullable — null means the configured default quota applies), `CreatedAt` (DateTimeOffset), `CreatedByUserId` (FK → ApplicationUser).

`PublicSlug` SHALL be enforced unique at the database level via a unique index. `Country` and `Region` values, when set, SHALL be codes present in the region registry.

#### Scenario: Household created with unique slug
- **WHEN** a user creates a household named "Bigler Garage" with slug "bigler-garage"
- **THEN** the household is persisted and a second attempt to create a household with slug "bigler-garage" fails with a unique constraint violation

#### Scenario: Public visibility defaults to false
- **WHEN** a household is created without specifying `IsPublicVisible`
- **THEN** `IsPublicVisible` is stored as `false` and the household is not accessible via the public garage endpoint

#### Scenario: Household location persisted
- **WHEN** a household is saved with `Country = "US"` and `Region = "US-WI"`
- **THEN** both values round-trip through EF Core, and a household saved with neither remains valid with both `NULL`

#### Scenario: Storage counters default sensibly
- **WHEN** a new household is created
- **THEN** `StorageUsedBytes` is 0 and `StorageQuotaOverrideBytes` is `NULL`
