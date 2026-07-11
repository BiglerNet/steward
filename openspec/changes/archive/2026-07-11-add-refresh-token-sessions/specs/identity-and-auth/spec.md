## MODIFIED Requirements

### Requirement: JWT Bearer authentication
The system SHALL issue signed JWT access tokens upon successful authentication, paired with a server-tracked refresh token. All protected API endpoints SHALL require a valid `Authorization: Bearer <token>` header. Unauthenticated requests SHALL receive HTTP 401.

Token claims SHALL include: `sub` (user ID), `email`, `name` (DisplayName), and `role` (ASP.NET Identity roles).

Access tokens SHALL have a configurable expiry defaulting to 30 minutes (`Jwt:ExpiryMinutes`).

#### Scenario: Valid token grants access
- **WHEN** a request is sent to a protected endpoint with a valid, non-expired JWT
- **THEN** the endpoint returns HTTP 200 (or appropriate success code)

#### Scenario: Missing token is rejected
- **WHEN** a request is sent to a protected endpoint with no `Authorization` header
- **THEN** the API returns HTTP 401 with no response body leaking internal details

#### Scenario: Expired token is rejected
- **WHEN** a request is sent with a JWT whose `exp` claim is in the past
- **THEN** the API returns HTTP 401
