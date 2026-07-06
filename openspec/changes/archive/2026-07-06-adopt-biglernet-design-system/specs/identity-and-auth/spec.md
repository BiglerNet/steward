## MODIFIED Requirements

### Requirement: ASP.NET Core Identity with extended user
The system SHALL use ASP.NET Core Identity for user management. The `ApplicationUser` class SHALL extend `IdentityUser` and add: `DisplayName` (string, nullable), `AvatarUrl` (string, nullable), and `ThemePreference` (nullable, one of `Light`/`Dark`/`System`; `null` means no explicit preference has been set and the client SHALL fall back to the OS `prefers-color-scheme` setting). Identity tables SHALL be created by the initial EF Core migration; `ThemePreference` SHALL be added by a subsequent migration with a `NULL` default for existing rows.

#### Scenario: User record created via Identity
- **WHEN** a new user registers via email/password or completes an OAuth flow
- **THEN** an `ApplicationUsers` row is created with a valid `Id` (Guid), `Email`, `NormalizedEmail`, and a `NULL` `ThemePreference`

#### Scenario: Existing users unaffected by the new column
- **WHEN** the `ThemePreference` migration runs against a database with existing `ApplicationUsers` rows
- **THEN** every existing row gets a `NULL` `ThemePreference` and no other column is affected
