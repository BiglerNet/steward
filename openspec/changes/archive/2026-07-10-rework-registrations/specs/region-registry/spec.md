## ADDED Requirements

### Requirement: Backend-owned region registry
The system SHALL define a static region registry in the Application layer covering exactly two countries: United States (`US`) and Canada (`CA`). Each country entry SHALL contain its ISO 3166-1 alpha-2 `code`, a display `name`, and a list of regions; each region SHALL contain its full ISO 3166-2 `code` (e.g. `US-WI`, `CA-ON`) and display `name`. The US entry SHALL contain the 50 states plus the District of Columbia; the CA entry SHALL contain the 10 provinces and 3 territories.

#### Scenario: Registry contents complete
- **WHEN** a unit test enumerates the region registry
- **THEN** it contains exactly `US` with 51 regions and `CA` with 13 regions, every region code prefixed by its country code

#### Scenario: Region lookup by code
- **WHEN** application code asks the registry whether `US-WI` is a valid region of country `US`
- **THEN** the registry answers affirmatively, and answers negatively for `US-XX` or for `CA-ON` under country `US`

### Requirement: Regions endpoint
The system SHALL provide `GET /api/regions` returning all region registry entries. The endpoint SHALL allow anonymous access (static, non-sensitive reference data) and its response DTOs SHALL appear in the OpenAPI document so generated frontend types include them.

#### Scenario: All countries and regions returned
- **WHEN** any caller (authenticated or not) sends `GET /api/regions`
- **THEN** HTTP 200 is returned with both countries and their full region lists, each entry carrying `code` and `name`

#### Scenario: Region types available to the frontend
- **WHEN** the frontend API types are regenerated against the running API
- **THEN** `schema.d.ts` contains the region registry response types
