## ADDED Requirements

### Requirement: OAuth provider configuration discovery
The system SHALL expose an unauthenticated `GET /api/auth/oauth/providers` endpoint that reports, for each supported provider (`google`, `facebook`, `apple`), whether that provider's client configuration is populated (non-empty client ID). The response SHALL contain only boolean flags and MUST NOT leak client IDs, secrets, or any other configuration values.

#### Scenario: Provider fully configured
- **WHEN** `Auth:Google:ClientId` is a non-empty value in configuration
- **THEN** `GET /api/auth/oauth/providers` reports `google: true`

#### Scenario: Provider not configured
- **WHEN** `Auth:Facebook:ClientId` is empty or missing from configuration
- **THEN** `GET /api/auth/oauth/providers` reports `facebook: false`

#### Scenario: No provider secrets are exposed
- **WHEN** any client calls `GET /api/auth/oauth/providers`
- **THEN** the response body contains only boolean fields per provider, with no client ID, secret, key ID, or team ID values present
