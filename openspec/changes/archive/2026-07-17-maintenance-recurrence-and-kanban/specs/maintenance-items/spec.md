## ADDED Requirements

### Requirement: List maintenance items across a household
The system SHALL provide `GET /api/households/{householdId}/maintenance-items` (any Active member or PlatformAdmin) returning maintenance items across every asset in the household, with optional `?status=` (one or more values) and `?assetId=` filters, each item including its `assetId` and `assetName` alongside the fields already returned by the per-asset list endpoint.

#### Scenario: Listing across the whole household
- **WHEN** a member calls the household-wide list endpoint for a household with maintenance items on three different assets
- **THEN** HTTP 200 is returned with items from all three assets, each annotated with its `assetId` and `assetName`

#### Scenario: Filtering by status and asset together
- **WHEN** a member calls the endpoint with `?status=Planned&status=InProgress&assetId={id}`
- **THEN** only that asset's `Planned`/`InProgress` items are returned

#### Scenario: Non-member cannot list
- **WHEN** a user who is not a member of the household calls this endpoint
- **THEN** HTTP 403 is returned
