# household-multitenancy Specification

## Purpose
Defines the Household entity and multi-tenant scoping model.

## Requirements
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

---

### Requirement: Household membership and roles
A `HouseholdMembership` SHALL associate an `ApplicationUser` with a `Household` at a specific role. Membership properties: `Id` (Guid), `HouseholdId` (FK), `UserId` (FK), `Role` (enum: Owner | Contributor | Viewer), `Status` (enum: Pending | Active | Revoked), `InvitedByUserId` (FK, nullable), `InvitedAt` (DateTimeOffset), `AcceptedAt` (DateTimeOffset, nullable).

A user SHALL NOT have more than one active membership per household (enforced via unique index on `HouseholdId + UserId`).

The user who creates a household SHALL automatically receive an `Owner` membership with `Status = Active`.

#### Scenario: Household creator receives Owner membership
- **WHEN** a user creates a new household
- **THEN** a `HouseholdMembership` with `Role = Owner` and `Status = Active` is created for that user

#### Scenario: Duplicate membership prevented
- **WHEN** a second `HouseholdMembership` for the same `UserId` and `HouseholdId` is inserted
- **THEN** a unique constraint violation is thrown

---

### Requirement: Role-based capability enforcement
The system SHALL enforce the following capability matrix via resource-based authorization. Authorization decisions SHALL query the live `HouseholdMembership` table (not cached JWT claims).

| Operation | Viewer | Contributor | Owner |
|-----------|--------|-------------|-------|
| View all household assets and records | ✓ | ✓ | ✓ |
| Add or edit assets and tracking records | ✗ | ✓ | ✓ |
| Delete assets | ✗ | ✗ | ✓ |
| Invite or remove members | ✗ | ✗ | ✓ |
| Delete the household | ✗ | ✗ | ✓ |

#### Scenario: Viewer cannot edit an asset
- **WHEN** a user with `Role = Viewer` sends a PUT request to update an asset in their household
- **THEN** the API returns HTTP 403

#### Scenario: Contributor can add a service record
- **WHEN** a user with `Role = Contributor` sends a POST request to create a service record for a household asset
- **THEN** the request succeeds with HTTP 201

#### Scenario: Owner can invite a new member
- **WHEN** a user with `Role = Owner` sends an invite request for a new user email
- **THEN** a `HouseholdMembership` with `Status = Pending` is created for the invited user

#### Scenario: Revoked member cannot access household
- **WHEN** a user whose membership `Status = Revoked` sends any request to a household-scoped endpoint
- **THEN** the API returns HTTP 403

---

### Requirement: PlatformAdmin bypasses household authorization
Users with the `PlatformAdmin` role SHALL bypass all household-level authorization checks and MAY access any household's resources.

#### Scenario: PlatformAdmin views any household
- **WHEN** a user with `PlatformAdmin` role requests assets for a household they are not a member of
- **THEN** the request succeeds and returns the household's assets

#### Scenario: Regular user cannot cross household boundary
- **WHEN** a user who has no membership in Household B sends a request scoped to Household B
- **THEN** the API returns HTTP 403 (not 404, to avoid leaking existence)
