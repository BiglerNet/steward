## Why

There is no way for the first platform administrator to bootstrap themselves. The `PlatformAdmin` Identity role exists and is seeded on startup, but there is no mechanism to assign it to a real user. This change adds a zero-friction bootstrap path: configure an email address in app config, and any user who registers with that email is automatically granted the `PlatformAdmin` role — no Helm-managed passwords, no manual DB surgery.

## What Changes

- **Config**: New `PlatformAdmin__Email` setting (string, default empty). When empty, no auto-grant occurs — safe default for environments where no bootstrap is needed.
- **`AuthService.RegisterAsync`**: After successful user creation, check if the new user's email matches `PlatformAdmin__Email`; if so, call `userManager.AddToRoleAsync(user, "PlatformAdmin")`.
- **`AuthService.HandleOAuthCallbackAsync`**: Same check applied after the new-user branch creates a user via OAuth — so an admin who uses Google/Apple/Facebook sign-in also gets auto-promoted.
- **Helm chart** (`charts/steward/`): Add `platformAdmin.email` to `values.yaml` (default `""`); inject as `PlatformAdmin__Email` env var in the API Deployment and setup Job templates.
- No changes to what `PlatformAdmin` can do — authorization logic and admin endpoints are untouched.
- `PlatformAdminRoleSeeder` (already implemented) ensures the role exists before the check runs — no change needed there.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `platform-admin`: Adding a new requirement — the system auto-grants the `PlatformAdmin` role at registration time when the registering user's email matches the configured bootstrap email.

## Impact

- **Modified files**: `src/Steward.Infrastructure/Identity/AuthService.cs` (two user-creation paths), `charts/steward/values.yaml`, `charts/steward/templates/api-deployment.yaml`, `charts/steward/templates/setup-job.yaml`
- **New config key**: `PlatformAdmin__Email` — must be documented in `.env.example` and Helm values
- **No migrations** — no schema changes
- **No API contract changes** — registration request/response shapes are unchanged; the role simply appears in the JWT claims of the newly registered admin user
