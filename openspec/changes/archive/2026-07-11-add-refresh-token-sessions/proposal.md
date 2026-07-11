## Why

Access tokens expire after 15 minutes and there is no refresh mechanism, so users are hard-logged-out (redirected to `/login`) every 15 minutes regardless of activity. This was a deliberate stand-in decision made when JWT auth was first introduced (`openspec/changes/archive/2026-06-18-core-solution-structure/design.md` explicitly deferred "refresh token backend" to a dedicated change) — the short expiry was the only lever available to bound how long a stolen or since-revoked token stays valid. That tradeoff is no longer necessary once real, revocable server-side sessions exist.

## What Changes

- Add a server-side refresh token: a random opaque value, hashed at rest, stored in a new Postgres `RefreshTokens` table — issued alongside every JWT access token on login/register/OAuth exchange.
- Add `POST /api/auth/refresh`: exchanges a valid refresh token for a new access token + rotated refresh token. Re-derives the user's current roles from the database on every refresh, which tightens the window during which a revoked household role or membership can still be exercised down to the access-token lifetime (independent of how long the overall session lasts).
- Rotation includes a short (30–60s, configurable) reuse grace window on the just-rotated token to absorb benign races (multiple browser tabs, retried requests). Reuse of a refresh token *outside* that window is treated as token theft and revokes the entire token chain for that user.
- **BREAKING**: `POST /api/auth/logout` becomes a real endpoint that revokes the presented refresh token server-side. Today logout is client-only (`localStorage` clear); the JWT stays valid until natural expiry. Frontend logout now calls this endpoint.
- Default access token expiry (`Jwt:ExpiryMinutes`) changes from 15 to 30, since expiry no longer has to double as the only revocation mechanism. Two new config values control refresh token lifetime: `Jwt:RefreshToken:RememberMeExpiry` (default 30 days) and `Jwt:RefreshToken:DefaultExpiry` (default 10 hours), both `TimeSpan`-bindable and env-overridable, matching the existing `Jwt:ExpiryMinutes` pattern.
- Frontend: add a "Remember me" checkbox to the login form, checked by default, controlling which refresh-token expiry is requested at login. The choice is stored on the refresh-token row so it survives rotation.
- Frontend: refresh token is persisted in `localStorage` alongside the access token (same session object, same storage mechanism already in use — no new storage mechanism).
- Frontend: replace reactive-only ("wait for a 401, then log out") session handling with proactive silent refresh — a timer scheduled relative to the access token's `expiresAt` fires a refresh call before the token expires, invisible to the user.
- Frontend: add cross-tab session sync via a `window` `storage` event listener, so other open tabs pick up a rotated or cleared session instead of independently attempting a refresh with a now-stale token.

## Capabilities

### Modified Capabilities
- `identity-and-auth`: JWT access token default expiry changes 15m → 30m (configurable); token issuance now always includes a refresh token.
- `user-auth`: new `POST /api/auth/refresh` and `POST /api/auth/logout` endpoints; `AuthResponse` shape gains a refresh token; existing login/register/OAuth-exchange requirements gain refresh-token issuance.
- `frontend-auth`: session persistence, logout, and the "logout on 401" requirement change to reflect refresh-token storage, proactive silent refresh, server-side logout revocation, and cross-tab sync; new "Remember me" requirement on login/register.

## Impact

- **Backend**: `Steward.Domain` (new `RefreshToken` entity), `Steward.Infrastructure/Persistence` (new EF configuration + migration), `Steward.Application/Identity` (`IJwtTokenService`/new `IRefreshTokenService` contracts), `Steward.Infrastructure/Identity` (`JwtTokenService`, `AuthService.BuildAuthResponseAsync`, `AuthServiceExtensions` DI wiring, new EF-backed refresh token store mirroring the existing `MemoryCacheOAuthExchangeService` pattern), `Steward.Api/Controllers` (auth controller gains `/refresh` and `/logout` actions), `appsettings.json` / `appsettings.Development.json` / `.env.example` (new config keys).
- **Frontend**: `src/Steward.Web/src/context/AuthContext.tsx` (proactive refresh timer, storage-event listener, logout now calls the backend), `src/Steward.Web/src/lib/session.ts` (session shape gains refresh token), `src/Steward.Web/src/api/auth.ts` (new `refresh`/`logout` calls), `src/Steward.Web/src/api/client.ts` (interceptor changes), login page (Remember me checkbox).
- **Database**: new migration adding a `RefreshTokens` table.
- No changes to household authorization logic, OAuth provider flows, or the JWT claim shape itself.
