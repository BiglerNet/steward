## Context

The app has a `PlatformAdmin` ASP.NET Core Identity role and a `PlatformAdminRoleSeeder` that ensures the role exists in the database on startup. `PlatformAdminController` enforces the role on admin endpoints. What's missing is the bootstrap path: no user can ever *get* the role without direct database manipulation.

Two user-creation paths exist in `AuthService`:
- `RegisterAsync` — direct email/password registration
- `HandleOAuthCallbackAsync` — first-time OAuth sign-in (Google, Facebook, Apple)

Both create an `ApplicationUser` via `UserManager<ApplicationUser>` and return before any role assignment. Both need the auto-grant hook.

The check relies on `IConfiguration`, which is already injected into `AuthService`.

## Goals / Non-Goals

**Goals:**
- Any user registering (direct or OAuth) with a matching email gets `PlatformAdmin` at the moment of account creation
- Safe default: empty config = no auto-grant, no behavioral change
- Idempotent: re-registering an existing admin email (rejected by duplicate check) or re-running the check never breaks
- The role is in the JWT claims returned from the same registration call — admin doesn't need to log out and back in

**Non-Goals:**
- Multiple admin emails (single string only; extend to list later if needed)
- UI-driven admin assignment (that's `platform-admin` role management endpoints, already in scope for another change)
- Revoking or rotating the bootstrap email at runtime
- Any change to `PlatformAdminRoleSeeder` (already correct — role existence is guaranteed before registration can run)

## Decisions

### D1: Check location — in `AuthService`, not a separate service
**Decision:** Add the check directly in both `RegisterAsync` and `HandleOAuthCallbackAsync` immediately after `userManager.CreateAsync` succeeds, before `BuildAuthResponseAsync`. No new service or middleware.

**Why:** The logic is two lines. Extracting it to a helper method within `AuthService` is sufficient. A separate `IPlatformAdminBootstrapService` would be overengineering a config comparison.

**Alternatives considered:** ASP.NET Core Identity event/hook (not supported out of the box, would require custom `UserManager` override — disproportionate complexity); middleware that inspects JWT claims post-login (too late, role must be in the first token).

### D2: Config key — `PlatformAdmin:Email` single string
**Decision:** Read via `configuration["PlatformAdmin:Email"]`. Empty or null = skip. Case-insensitive comparison against the new user's email.

**Why:** Single email covers the self-hosted bootstrap use case. `IConfiguration` is already injected. No options class needed for one property.

**Alternatives considered:** `IOptions<PlatformAdminOptions>` with a list of emails — clean pattern but unnecessary for one value; add it later if multi-admin bootstrap is ever needed.

### D3: Role existence dependency — rely on `PlatformAdminRoleSeeder`
**Decision:** `AuthService` calls `userManager.AddToRoleAsync(user, "PlatformAdmin")` and lets it throw if the role doesn't exist. `PlatformAdminRoleSeeder` (already registered as `IHostedService`) guarantees the role is present before any HTTP requests can be served.

**Why:** The seeder runs at application startup before the Kestrel HTTP pipeline opens. There is no timing window in which a registration request can arrive before the role is seeded. Redundantly checking role existence in `AuthService` would be defensive code without value.

### D4: OAuth path coverage — check in `HandleOAuthCallbackAsync` new-user branch only
**Decision:** The auto-grant check runs only in the `user is null → CreateAsync` branch of `HandleOAuthCallbackAsync`. If the user already exists (returning OAuth user), no role check is done — they already have whatever roles they have.

**Why:** Auto-grant is a first-registration-only event. A returning admin user already has the role. Checking on every OAuth callback would be a no-op for existing users and is unnecessary.

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| Bootstrap email leaked via misconfigured logging | `PlatformAdmin:Email` is not a secret (it's an email), but log output should not include it unnecessarily — avoid logging the config value |
| Admin registers before `PlatformAdminRoleSeeder` runs | Not possible — seeder runs as `IHostedService` before HTTP pipeline opens |
| `AddToRoleAsync` fails silently if role missing | Would throw `InvalidOperationException`; this surfaces as HTTP 500 on registration. Acceptable — it indicates a misconfigured deployment (seeder not running). Could wrap and log, but masking the error is worse |
| Someone else registers with the configured email first (typo, race) | They get admin. The email should be set to an address only the real admin can register with. This is inherent to the pattern and documented as an operator responsibility |

## Migration Plan

1. Deploy the app change (no migrations needed)
2. Set `PlatformAdmin__Email` in the Helm values for the target environment
3. The designated admin registers via the normal UI — they receive a JWT with `PlatformAdmin` in roles
4. To change the bootstrap email after initial setup: update config and redeploy; old admin retains role, new bootstrap email takes effect for future registrations

Rollback: remove `PlatformAdmin__Email` from config and redeploy. Existing admin users keep their role (Identity DB is not modified by rollback). No data loss.
