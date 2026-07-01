## Why

The entity model and project skeleton from Change 1 provide the database schema and project wiring, but the application has no runnable API surface yet. Before any garage features can be built, users need to be able to register, authenticate, and organize their assets into households. This change delivers the complete identity and tenancy layer that all subsequent feature changes depend on.

## What Changes

- Implement registration and login endpoints (email/password) that issue JWT access tokens
- Implement OAuth 2.0 social login flow (Google, Facebook, Apple) with callback handling and JWT issuance
- Implement `HouseholdAuthorizationHandler` and register resource-based authorization pipeline
- Add Household CRUD endpoints (create, get, update, delete) — scoped to the authenticated user
- Add `HouseholdInvitation` domain entity (separate from `HouseholdMembership`) to support pre-registration invites with expiry
- Add HouseholdMembership endpoints: invite by email (creates `HouseholdInvitation`, not a membership), accept invite, revoke invite, list members
- Seed the `PlatformAdmin` Identity role on application startup
- Add a PlatformAdmin controller for user lookup and role management
- Wire FluentValidation for all request DTOs in this change

## Capabilities

### New Capabilities

- `user-auth`: Email/password registration and login, JWT access token issuance, OAuth social login (Google, Facebook, Apple), token claims structure
- `household-management`: Household CRUD (create/read/update/delete), ownership rules, public slug management
- `household-membership`: Invite flow (by email, supports pre-registration), `HouseholdInvitation` lifecycle (Pending → Accepted/Revoked/Expired), accept invite, revoke invite, membership listing
- `platform-admin`: PlatformAdmin role seeding, admin endpoints for listing users and managing roles

### Modified Capabilities

*(none — builds on Change 1 entities, no requirement changes)*

## Impact

- **Api project**: New controllers — `AuthController`, `HouseholdsController`, `HouseholdMembershipsController`, `PlatformAdminController`
- **Application project**: Request/response DTOs, FluentValidation validators, `IHouseholdService` and `IAuthService` interfaces
- **Infrastructure project**: `HouseholdAuthorizationHandler`, `JwtTokenService` implementation, service implementations
- **Database**: One new migration adds `HouseholdInvitations` table (domain entity added in this change); startup seed adds `PlatformAdmin` role if absent
- **Dependencies**: No new packages — all auth packages were added in Change 1
