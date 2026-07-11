## 1. Domain & persistence (Domain, Infrastructure)

- [x] 1.1 Add `RefreshToken` entity in `Steward.Domain` (Id, UserId, TokenHash, ExpiresAt, RememberMe, CreatedAt, RevokedAt, ReplacedByTokenHash).
- [x] 1.2 Add EF configuration (`RefreshTokenConfiguration`) in `Steward.Infrastructure/Persistence` with an index on `TokenHash` and on `UserId`; register on `StewardDbContext`.
- [x] 1.3 Add EF Core migration for the new `RefreshTokens` table (`dotnet ef migrations add AddRefreshTokens --project src/Steward.Infrastructure --startup-project src/Steward.Api`).

## 2. Application contracts (Application)

- [x] 2.1 Add `IRefreshTokenService` in `Steward.Application/Identity` (issue, validate/rotate, revoke-chain operations) alongside the existing `IJwtTokenService`.
- [x] 2.2 Extend `Steward.Application/Auth/Dtos.cs`: add `RefreshToken` to `AuthResponse`; add `RememberMe` to `LoginRequest`; add `RefreshRequest { RefreshToken }` and `LogoutRequest { RefreshToken }`.
- [x] 2.3 Extend `IAuthService` (`Steward.Application/Auth/IAuthService.cs`) with `RefreshAsync(RefreshRequest)` and `LogoutAsync(LogoutRequest)`.
- [x] 2.4 Add FluentValidation rules for `RefreshRequest`/`LogoutRequest` in `Steward.Application/Auth/Validators.cs` (non-empty token).

## 3. Infrastructure implementation (Infrastructure)

- [x] 3.1 Implement `RefreshTokenService : IRefreshTokenService` in `Steward.Infrastructure/Identity` — hashes tokens (SHA-256) before storage/lookup, applies `RememberMeExpiry`/`DefaultExpiry` from config, implements rotation (revoke presented token, insert new row linked via `ReplacedByTokenHash`), implements the reuse grace window check, and implements chain revocation (all non-expired tokens for a `UserId`) for both theft detection and logout.
- [x] 3.2 Update `AuthService.BuildAuthResponseAsync` (`Steward.Infrastructure/Identity/AuthService.cs`) to also issue a refresh token via `IRefreshTokenService` and include it in `AuthResponse`; thread `RememberMe` through `LoginAsync`; registration and OAuth exchange issue refresh tokens with the "remembered" (long) expiry.
- [x] 3.3 Implement `AuthService.RefreshAsync`: validate/rotate via `IRefreshTokenService`, re-derive roles with `userManager.GetRolesAsync`, issue a new JWT via `IJwtTokenService`, return a new `AuthResponse`.
- [x] 3.4 Implement `AuthService.LogoutAsync`: revoke the presented refresh token's chain via `IRefreshTokenService`.
- [x] 3.5 Register `IRefreshTokenService` in `AuthServiceExtensions.AddStewardAuth`.
- [x] 3.6 Add `Jwt:RefreshToken:RememberMeExpiry`, `Jwt:RefreshToken:DefaultExpiry`, `Jwt:RefreshToken:ReuseGracePeriod` to `appsettings.json` and `appsettings.Development.json`; bump `Jwt:ExpiryMinutes` default to 30; add corresponding `Jwt__RefreshToken__*` entries to `.env.example`.

## 4. API surface (Api)

- [x] 4.1 Add `POST /api/auth/refresh` to `AuthController` (`Steward.Api/Controllers/AuthController.cs`), calling `IAuthService.RefreshAsync`; returns 401 on invalid/expired/revoked token per spec.
- [x] 4.2 Add `POST /api/auth/logout` to `AuthController` (requires authentication), calling `IAuthService.LogoutAsync`.
- [x] 4.3 Update `LoginRequest` binding/OpenAPI docs to include `rememberMe` (default `true` when omitted).

## 5. Backend tests

- [x] 5.1 Unit tests for `RefreshTokenService`: issuance hashes correctly, rotation revokes old + links new, reuse within grace window returns the same pair, reuse outside grace window revokes the full chain.
- [x] 5.2 Unit tests for `AuthService.RefreshAsync` re-deriving roles from the database rather than copying prior claims.
- [x] 5.3 Integration tests (`Steward.IntegrationTests`): login issues both tokens; `/api/auth/refresh` happy path rotates and grants access with the new token; expired/revoked refresh token returns 401; `/api/auth/logout` revokes and a subsequent refresh attempt with that token returns 401; a role change between refreshes is reflected in the new access token's claims.

## 6. Frontend session plumbing (Web)

- [x] 6.1 Extend `StoredSession` in `src/Steward.Web/src/lib/session.ts` with `refreshToken`.
- [x] 6.2 Add `refresh` and `logout` API calls in `src/Steward.Web/src/api/auth.ts`; add `rememberMe` to the login call's request type.
- [x] 6.3 Regenerate the typed API client (`npm run generate:api`) after the backend DTO/route changes land.

## 7. Frontend session lifecycle (Web)

- [x] 7.1 In `AuthContext.tsx`, add a proactive refresh timer scheduled relative to `expiresAt` (with a buffer before actual expiry); reschedule on login/refresh/session restore.
- [x] 7.2 In `AuthContext.tsx`, add a `window` `storage` event listener that adopts a rotated session (reschedules the timer) or clears state and redirects to `/login` when the session key is removed by another tab.
- [x] 7.3 Update `api/client.ts`'s response interceptor: on `401`, attempt one refresh-and-retry of the original request before falling back to the existing clear-session-and-redirect behavior.
- [x] 7.4 Update `AuthContext.logout()` to call the new `POST /api/auth/logout` endpoint (best-effort — clear local state regardless of the call's outcome).

## 8. Frontend UI (Web)

- [x] 8.1 Add a "Remember me" checkbox (checked by default) to the login form/page, wired to send `rememberMe` on submit.
- [x] 8.2 Verify the registration and OAuth-callback flows correctly store the refresh token returned in `AuthResponse` (no UI change needed there beyond session shape).

## 9. Frontend tests

- [x] 9.1 `AuthContext` tests: proactive refresh timer fires and updates session; `storage` event from another tab updates in-memory state (both rotation and clear-on-logout cases).
- [x] 9.2 `client.ts` interceptor test: a `401` triggers one refresh-and-retry, and a failed refresh falls back to clearing the session and redirecting.
- [x] 9.3 Login form test: "Remember me" defaults to checked and toggling it changes the submitted `rememberMe` value.

## 10. Docs

- [x] 10.1 Update README/CLAUDE.md if the new config keys or auth flow need explicit mention for local setup (check `.env.example` comments are self-explanatory instead if that's sufficient).
