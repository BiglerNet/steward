## Context

Auth today: `AuthService.BuildAuthResponseAsync` issues a single JWT access token (`JwtTokenService`, HS256, 15-minute default expiry) on login/register/OAuth exchange. There is no refresh token. The frontend (`AuthContext` + `lib/session.ts`) stores `{ token, expiresAt, user, pendingInvites }` in `localStorage`; `api/client.ts`'s axios response interceptor clears the session and hard-redirects to `/login` on any `401`. Logout (`AuthContext.logout()`) only clears `localStorage` — the JWT itself remains valid until it naturally expires.

This 15-minute expiry was a deliberate stand-in, not an oversight: `openspec/changes/archive/2026-06-18-core-solution-structure/design.md` chose stateless JWTs over server-side sessions/cookies (SPA is cross-origin from the API, mobile clients anticipated later) and explicitly noted the tradeoff — "JWT stateless — revoked memberships not immediately reflected; short 15-minute access token expiry limits window" — deferring "refresh token backend" to a dedicated change. This is that change.

Related precedent already in the codebase: `MemoryCacheOAuthExchangeService` issues a single-use, TTL'd, server-tracked opaque code (`IMemoryCache`-backed) for the OAuth callback → SPA handoff. The refresh token mechanism below follows the same shape — opaque server-tracked token — but needs to survive process restarts and be explicitly revocable, so it is EF/Postgres-backed rather than in-memory.

## Goals / Non-Goals

**Goals:**
- Users stay logged in across normal usage gaps (browser restarts, days between visits) without re-entering credentials, while keeping the ability to actually revoke a session (logout, theft detection).
- Keep the access-token/claims/authorization pipeline (`HouseholdAuthorizationHandler`, `RoleClaimType`, JWT validation in `AuthServiceExtensions`) completely unchanged — only how a token gets renewed changes.
- All new lifetimes are configuration-driven (12-factor), following the existing `Jwt:ExpiryMinutes` / `Jwt__ExpiryMinutes` env-override convention.
- No new infrastructure dependency (no Redis) — Postgres only, consistent with "no Redis yet" and the rest of the stack.

**Non-Goals:**
- Not adopting ASP.NET Core Identity's built-in `AddIdentityApiEndpoints`/`AddBearerToken` refresh mechanism (see Decisions — rejected).
- Not moving to cookie-based transport for either token. Bearer stays for the access token; the refresh token is also handled by the SPA directly (request body / `localStorage`), not a cookie.
- Not building a mobile client or its token-refresh flow in this change — bearer is chosen partly to keep that door open later, but nothing mobile-specific is implemented now.
- Not changing OAuth provider flows, household authorization logic, or JWT claim contents.

## Decisions

### 1. Hand-rolled refresh token, not ASP.NET Core Identity's built-in bearer/refresh scheme
Identity ships `AddIdentityApiEndpoints`/`AddBearerToken` (+ `MapIdentityApi`) with a working `/refresh` endpoint out of the box. Rejected because:
- Its tokens are opaque, data-protection-encrypted strings, not real JWTs — no inspectable `exp`/`iss`/`aud`, and no natural extension point for the custom claims (`role`, household context) `JwtTokenService` already produces.
- It bundles its own `/login`, `/register`, `/confirmEmail`, etc. endpoints, which would collide with or duplicate the existing custom `IAuthService`/`AuthController` — OAuth exchange-code flow, pending-invite surfacing, `PlatformAdmin` role seeding on register — all of which have no equivalent in the built-in surface.

Instead: keep `JwtTokenService` exactly as-is for access tokens. Add a hand-rolled refresh token — a cryptographically random opaque value, hashed (SHA-256) before storage — persisted in a new `RefreshTokens` table via EF Core. This mirrors `MemoryCacheOAuthExchangeService`'s shape (issue opaque value, track server-side, single meaningful use) but is DB-backed since it must survive restarts and support explicit revocation queries.

### 2. Refresh token store: Postgres table, not Redis
No Redis dependency exists in the stack today (confirmed: OAuth exchange codes use `IMemoryCache`, which is itself an acceptable single-instance simplification for a 60-second TTL value, but a multi-week-lived refresh token needs to survive restarts). Postgres is already the source of truth for everything else. A `RefreshTokens` table sits behind `IRefreshTokenService` in `Steward.Application/Identity`, so a future move to Redis (if the "probably eventually" materializes) is a swap of the `Steward.Infrastructure` implementation, not a contract change — same pattern `IOAuthExchangeService` already establishes.

`RefreshTokens` columns: `Id` (Guid), `UserId` (FK), `TokenHash` (the only thing stored — never the raw token), `ExpiresAt`, `RememberMe` (bool), `CreatedAt`, `RevokedAt` (nullable), `ReplacedByTokenHash` (nullable, set on rotation).

### 3. Rotation with a reuse grace window, not strict one-shot rotation
Every `/api/auth/refresh` call rotates: the presented token is marked revoked (`RevokedAt` set, `ReplacedByTokenHash` pointing at the new token), and a new access + refresh token pair is issued. Strict rotation (reject any reuse of a revoked token outright) is the textbook approach, but it breaks under two benign conditions this app will hit in practice: multiple browser tabs both holding the same refresh token and independently deciding to refresh near-simultaneously, and ordinary network retries.

Mitigation: a revoked token remains honorable for a short grace window (config-driven, default 45s) after rotation — presenting it within that window returns the *same* resulting token pair rather than erroring, rather than treating it as new rotation. Presenting a revoked token *outside* the grace window is treated as compromise: revoke the entire token chain for that user (walk `ReplacedByTokenHash` links, or simpler — revoke all non-expired tokens for that `UserId`), forcing re-login everywhere. This is the standard mitigation described in refresh-token-rotation guidance (e.g. Auth0's rotation docs) for exactly this multi-client race.

### 4. Every refresh re-derives roles from the database
`BuildAuthResponseAsync`-equivalent logic at refresh time re-runs `userManager.GetRolesAsync` (and anything else baked into JWT claims) rather than copying claims forward from the old access token. This means a revoked household role or a demoted `PlatformAdmin` is reflected within one access-token lifetime (30 min) of the change, regardless of how long the refresh token / overall session lives — actually *tighter* staleness bound than the current 15-minute all-in-one token, since today staleness is bounded by access-token expiry too, but there's no long-lived session sitting on stale claims in the interim.

### 5. Two configurable refresh-token lifetimes, selected by "Remember me"
```json
"Jwt": {
  "ExpiryMinutes": 30,
  "RefreshToken": {
    "RememberMeExpiry": "30.00:00:00",
    "DefaultExpiry": "10:00:00",
    "ReuseGracePeriod": "00:00:45"
  }
}
```
`TimeSpan` values bind natively from `"d.hh:mm:ss"` config strings — no custom parsing — and follow the same `Jwt__RefreshToken__DefaultExpiry`-style env-override path already used for `Jwt:Key`/`Jwt:ExpiryMinutes`. The login/register/OAuth-exchange request path accepts a `rememberMe` flag; the issued refresh token's `ExpiresAt` and stored `RememberMe` reflect that choice. On rotation, the new token's expiry policy is derived from the row being rotated (`RememberMe` carried forward), not re-asked — the user does not re-check the box on every silent refresh.

Access token default moves 15m → 30m. It no longer needs to double as the sole revocation mechanism (see Decision 4), so it can sit closer to common industry defaults (Okta: 1h, Entra ID: 60–90m, Google OAuth: ~1h) while still being materially tighter than any of those.

### 6. Refresh token transport and storage: `localStorage`, same as access token, not an httpOnly cookie
Considered putting the refresh token in an httpOnly cookie scoped to `/api/auth/refresh` (JS can't read it, meaningfully reducing XSS blast radius given its long lifetime). Rejected in favor of consistency and simplicity: it introduces a second transport mechanism alongside bearer, complicates the "bearer only" mental model the team wants to keep for a potential future mobile client, and the team's explicit call was to accept the XSS tradeoff for a simpler, uniform implementation. Both tokens live in the same `mt.session` `localStorage` object, extended with a `refreshToken` field.

### 7. Frontend: proactive silent refresh + cross-tab sync via `storage` event
Reactive-only refresh (wait for a 401, then refresh-and-retry) is simpler but produces a visible stall on the first request after expiry and doesn't naturally coordinate multiple tabs. Instead: `AuthContext` schedules a timer relative to `expiresAt` (fires with a buffer before actual expiry) that proactively calls `/api/auth/refresh` and updates the stored session.

Multi-tab coordination: the browser's `storage` event fires in every tab *except* the one that wrote the change, so a `window.addEventListener("storage", ...)` handler in `AuthContext` lets other tabs pick up a rotated session (reschedule their own timer against the new `expiresAt`) or a cleared session (session key removed → logout) without independently hitting `/api/auth/refresh`. This narrows, but does not by itself eliminate, the cross-tab race — the remaining edge (two tabs' timers firing within the same tick, both requests in flight before either resolves) is absorbed by the backend's reuse grace window (Decision 3), not solved on the frontend. Cross-tab sync is a UX/efficiency improvement (avoid redundant refresh calls and unnecessary rotation churn); the grace window is the actual correctness backstop.

### 8. Logout becomes a real server-side revoke
`POST /api/auth/logout` (new) accepts the current refresh token and marks it (and, for symmetry with the theft-detection path, its whole chain) revoked. Frontend `AuthContext.logout()` calls this before clearing local state. This closes a real gap: today, "logging out" leaves the JWT valid until it naturally expires — tolerable at 15 minutes, not once sessions can be encouraged to last weeks via "Remember me".

## Risks / Trade-offs

- **Refresh token in `localStorage` is readable by any successful XSS** → Accepted trade-off (Decision 6); mitigated by keeping the access token short (30 min) so a stolen access token alone has bounded value, and by revocation-on-logout/theft-detection limiting how long a stolen refresh token stays usable if discovered.
- **Reuse grace window is a deliberate weakening of strict rotation** → Bounded to a short, configurable window (default 45s); reuse outside it still triggers full chain revocation. This trades a small theft-detection blind spot for not logging out legitimate multi-tab users.
- **New table + hot path (`/api/auth/refresh` hit far more often than login)** → Index `RefreshTokens.TokenHash` (lookup key) and `RefreshTokens.UserId` (chain revocation); expired/revoked rows need periodic cleanup (mirror the existing `InvitationExpiryService` hosted-service pattern for a lightweight sweep, or leave for a follow-up if row volume is negligible at self-hosted scale).
- **Existing sessions at deploy time have no refresh token** → Users currently logged in simply fall back to today's behavior (hard logout at their existing token's expiry) and log in again once, at which point they get a refresh token and "Remember me" going forward. No migration of live sessions needed.

## Migration Plan

1. Add `RefreshTokens` table via EF Core migration (additive, no impact on existing tables/data).
2. Ship backend changes (issuance, `/refresh`, `/logout`, config defaults) — backward compatible, since `AuthResponse` gaining a `refreshToken` field is additive and old clients simply ignore it.
3. Ship frontend changes (Remember me checkbox, session shape, proactive refresh, cross-tab sync, logout call).
4. No rollback complexity beyond normal deploy rollback — the new table and fields are additive; reverting the frontend/backend leaves the table unused, not broken.

## Open Questions

- Cleanup strategy for expired/revoked `RefreshTokens` rows: scheduled sweep now, or defer until row volume is actually a concern? Leaning toward deferring given self-hosted scale, revisit if it becomes noisy.
