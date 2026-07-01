## 1. Domain — HouseholdInvitation Entity

- [x] 1.1 Create `HouseholdInvitation` entity in Domain with properties: `Id` (Guid), `HouseholdId` (FK), `InvitedByUserId` (FK), `Email` (string), `Role` (HouseholdMemberRole: Contributor|Viewer), `InviteCode` (string, unique), `ExpiresAt` (DateTimeOffset), `Status` (enum: Pending|Accepted|Revoked|Expired), `AcceptedByUserId` (Guid?, nullable FK), `AcceptedAt` (DateTimeOffset?, nullable), `CreatedAt` (DateTimeOffset) [Domain]
- [x] 1.2 Add `InvitationStatus` enum (Pending | Accepted | Revoked | Expired) to Domain enums [Domain]
- [x] 1.3 Add `DbSet<HouseholdInvitation>` to `StewardDbContext`; configure entity: unique index on `InviteCode`, index on `Email`, FK relationships with appropriate cascade behavior [Infrastructure]
- [x] 1.4 Run `dotnet ef migrations add AddHouseholdInvitations --project src/Steward.Infrastructure --startup-project src/Steward.Api` and verify migration output [Infrastructure]

## 2. Application Layer — DTOs and Service Interfaces

- [x] 2.1 Create auth DTOs: `RegisterRequest` (email, password, displayName), `LoginRequest` (email, password), `OAuthExchangeRequest` (code), `PendingInviteSummary` (inviteCode, householdName, role, expiresAt), `AuthResponse` (token, expiresAt, user, pendingInvites) [Application]
- [x] 2.2 Create FluentValidation validators for `RegisterRequest` (email format, password min 8 chars + at least one non-alphanumeric, displayName required) and `LoginRequest` (email format, password required) [Application]
- [x] 2.3 Define `IAuthService` interface: `RegisterAsync`, `LoginAsync`, `HandleOAuthCallbackAsync`, `ExchangeOAuthCodeAsync` [Application]
- [x] 2.4 Create household DTOs: `CreateHouseholdRequest` (name, publicSlug, isPublicVisible), `UpdateHouseholdRequest`, `HouseholdResponse` (id, name, publicSlug, isPublicVisible, userRole, createdAt) [Application]
- [x] 2.5 Create membership DTOs: `InviteMemberRequest` (email, role), `InvitationResponse` (id, email, role, inviteCode, expiresAt, status), `MembershipResponse` (userId, displayName, email, role, status), `HouseholdMembersResponse` (members: MembershipResponse[], pendingInvites: InvitationResponse[]) [Application]
- [x] 2.6 Add FluentValidation validators for `CreateHouseholdRequest` (name required, publicSlug format `^[a-z0-9-]+$`, length 3–60) and `InviteMemberRequest` (email format, role must not be Owner) [Application]
- [x] 2.7 Define `IHouseholdService` interface: `CreateAsync`, `GetByIdAsync`, `ListForUserAsync`, `UpdateAsync`, `DeleteAsync`, `InviteMemberAsync`, `AcceptInviteAsync`, `RevokeInviteAsync`, `RevokeMemberAsync`, `ListMembersAsync` [Application]
- [x] 2.8 Define `IInvitationExpiryService` interface with `ExpireStaleInvitationsAsync` method [Application]

## 3. Infrastructure — JWT and Auth Services

- [x] 3.1 Implement `JwtTokenService` (registered as `IJwtTokenService`): generate HS256 signed JWT with `sub`, `email`, `name`, `role` claims; read `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` from `IConfiguration` [Infrastructure]
- [x] 3.2 Implement `AuthService.RegisterAsync`: create user via `UserManager`; query `HouseholdInvitations` for Pending, non-expired rows matching the registered email; include summaries in `AuthResponse.pendingInvites`; issue JWT [Infrastructure]
- [x] 3.3 Implement `AuthService.LoginAsync`: validate credentials via `SignInManager`; throw `UnauthorizedException` on failure (same error regardless of which field was wrong — no user enumeration) [Infrastructure]
- [x] 3.4 Define `IOAuthExchangeService`; implement `MemoryCacheOAuthExchangeService`: `GenerateCodeAsync(userId)` stores random Guid → userId in `IMemoryCache` (60s TTL); `RedeemCodeAsync(code)` returns userId and removes from cache [Infrastructure]
- [x] 3.5 Implement `AuthService.HandleOAuthCallbackAsync`: find or create `ApplicationUser` via external login; check pending invitations by email; call `GenerateCodeAsync`; return exchange code + pending invite summaries [Infrastructure]
- [x] 3.6 Implement `AuthService.ExchangeOAuthCodeAsync`: redeem exchange code, load user, issue JWT with pending invites in `AuthResponse` [Infrastructure]

## 4. Infrastructure — Household and Invitation Services

- [x] 4.1 Implement `HouseholdService.CreateAsync`: create household + Owner membership in a transaction; catch unique constraint on slug → throw `ConflictException` [Infrastructure]
- [x] 4.2 Implement `HouseholdService.GetByIdAsync` and `ListForUserAsync` (Active memberships only) [Infrastructure]
- [x] 4.3 Implement `HouseholdService.UpdateAsync` (slug uniqueness check) and `DeleteAsync` (reject if assets exist via `Assets.AnyAsync`) [Infrastructure]
- [x] 4.4 Implement `HouseholdService.InviteMemberAsync`: check for existing Pending non-expired invitation for same email+household → throw `ConflictException`; create `HouseholdInvitation` with `InviteCode = Guid.NewGuid().ToString("N")`, `ExpiresAt = UtcNow + 7 days` [Infrastructure]
- [x] 4.5 Implement `HouseholdService.AcceptInviteAsync`: find `HouseholdInvitation` by code where `Status = Pending AND ExpiresAt > UtcNow`; check calling user not already Active member; create `HouseholdMembership`; set invitation `Status = Accepted`, `AcceptedByUserId`, `AcceptedAt` [Infrastructure]
- [x] 4.6 Implement `HouseholdService.RevokeInviteAsync`: find Pending invitation by code; set `Status = Revoked`; throw `BadRequestException` if invitation is not Pending [Infrastructure]
- [x] 4.7 Implement `HouseholdService.RevokeMemberAsync`: prevent owner self-revoke → throw `BadRequestException`; set membership `Status = Revoked` [Infrastructure]
- [x] 4.8 Implement `HouseholdService.ListMembersAsync`: return Active memberships + non-expired Pending invitations as `HouseholdMembersResponse` [Infrastructure]
- [x] 4.9 Implement `InvitationExpiryService` as `IHostedService`: on startup and every 24 hours, bulk-update `HouseholdInvitations SET Status = Expired WHERE Status = Pending AND ExpiresAt < UtcNow` [Infrastructure]

## 5. Infrastructure — Authorization Handler and DI

- [x] 5.1 Implement `HouseholdAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, IHouseholdResource>`: query live `HouseholdMemberships` for `(householdId, userId)` with `Status = Active`; map role to operations (View/Edit/Delete/Invite); short-circuit succeed for `PlatformAdmin` role [Infrastructure]
- [x] 5.2 Register all services in Infrastructure DI extension: `HouseholdAuthorizationHandler` (scoped `IAuthorizationHandler`), `MemoryCacheOAuthExchangeService` (singleton), `InvitationExpiryService` (hosted service), `IMemoryCache` via `services.AddMemoryCache()` [Infrastructure]
- [x] 5.3 Add startup seed in `Program.cs`: after `app.Build()`, create a scope and use `RoleManager<IdentityRole>` to create `PlatformAdmin` role if absent [Api]

## 6. Api — Auth Controller

- [x] 6.1 Add `AuthController` with `POST /api/auth/register` and `POST /api/auth/login`; map `ConflictException` → 409, `UnauthorizedException` → 401 [Api]
- [x] 6.2 Add `GET /api/auth/oauth/{provider}/login` → `Challenge(provider)` with redirect URI set to callback endpoint; return 400 for unknown provider [Api]
- [x] 6.3 Add `GET /api/auth/oauth/{provider}/callback` → authenticate external login, call `IAuthService.HandleOAuthCallbackAsync`, redirect to `{FrontendBaseUrl}/auth/callback?code={exchangeCode}` [Api]
- [x] 6.4 Add `POST /api/auth/oauth/exchange` → `IAuthService.ExchangeOAuthCodeAsync`; return `AuthResponse` or 400 on invalid/expired code [Api]
- [x] 6.5 Add `GET /api/auth/me` `[Authorize]` → load user from DB by sub claim; return profile DTO [Api]
- [x] 6.6 Add `POST /api/auth/invites/{code}/accept` `[Authorize]` → `IHouseholdService.AcceptInviteAsync`; map `ConflictException` → 409, not-found/expired → 400 [Api]

## 7. Api — Households and Memberships Controllers

- [x] 7.1 Add `HouseholdsController` `[Authorize]`: `GET /api/households`, `POST /api/households` (return 201 + Location), `GET /api/households/{id}`, `PUT /api/households/{id}`, `DELETE /api/households/{id}`; apply `IAuthorizationService` with `HouseholdOperations` on each mutating endpoint [Api]
- [x] 7.2 Add `HouseholdMembershipsController` `[Authorize]`: `GET /api/households/{id}/members` (View), `POST /api/households/{id}/members/invite` (Invite → 201 with invite code), `DELETE /api/households/{id}/invitations/{code}` (Invite, revoke pending invite), `DELETE /api/households/{id}/members/{userId}` (Invite, revoke active membership) [Api]

## 8. Api — Platform Admin Controller

- [x] 8.1 Add `PlatformAdminController` `[Authorize(Roles = "PlatformAdmin")]`: `GET /api/admin/users` with optional `?email=` filter and pagination (`page`, `pageSize`); `POST /api/admin/users/{id}/roles` (assign role); `DELETE /api/admin/users/{id}/roles/{roleName}` (remove role, block self-removal of PlatformAdmin) [Api]
