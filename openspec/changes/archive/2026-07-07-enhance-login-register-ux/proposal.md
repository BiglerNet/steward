## Why

The login/register experience treats OAuth as an afterthought (buttons below the password form, always rendered even when a provider isn't configured on this deployment, no feedback while the redirect is in flight) and the registration form makes it easy to mistype a password with no confirmation step. Since SSO (Google/Facebook/Apple) is meant to be the primary sign-in path going forward, the UI should reflect that, and only advertise providers an operator has actually configured.

## What Changes

- Add a new unauthenticated `GET /api/auth/oauth/providers` endpoint that reports which OAuth providers (`google`, `facebook`, `apple`) have non-empty client configuration.
- `OAuthButtons` only renders providers reported as configured; if none are configured, the entire OAuth block (including the "or continue with" divider) is omitted rather than left as an empty divider.
- Add official light/dark brand icons for Google, Facebook, and Apple to the OAuth buttons (sourced as static SVG assets, not an icon library).
- Clicking a provider button shows a loading/redirecting state on that button and disables the sibling provider buttons until the browser navigates away.
- `LoginPage` and `RegisterPage` reorder their layout so the OAuth block renders above the email/password form (previously below).
- `RegisterPage` adds a confirm-password field that must match the password field.
- `RegisterPage` adds a show/hide toggle (eye icon) on the password and confirm-password fields.
- `RegisterPage` adds a live, per-rule pass/fail hint under the password field reflecting the existing password policy (8+ characters, one non-alphanumeric character) as the user types.

Explicitly not changed: no forgot/reset-password flow, no change to OAuth callback failure messaging granularity, no change to the existing silent OAuth account-linking-by-email behavior, no change to JWT/session/refresh-token behavior.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `identity-and-auth`: adds an OAuth provider configuration-discovery requirement (`GET /api/auth/oauth/providers`) so clients can determine which providers are usable without probing the OAuth flow itself.
- `frontend-auth`: OAuth login requirement extended with configured-provider filtering, zero-provider collapse, and a redirect-pending button state; email/password registration requirement extended with password confirmation, a show/hide toggle, and a live password-strength hint; login/register page layout requirement (new sub-behavior) puts OAuth above the password form.

## Impact

- **Backend**: `Steward.Api/Controllers/AuthController.cs` (new endpoint), likely a small DTO in `Steward.Application/Auth/Dtos.cs`. No domain or persistence changes.
- **Frontend**: `components/auth/OAuthButtons.tsx` (rework), new hook under `hooks/` (e.g. `useOAuthProviders`), `pages/LoginPage.tsx` and `pages/RegisterPage.tsx` (reorder + register form fields), new static assets under `src/assets/oauth/`, regenerated `api/schema.d.ts` (`npm run generate:api`).
- **Tests**: `LoginPage.test.tsx`, `RegisterPage.test.tsx`, and new coverage for `OAuthButtons`/`useOAuthProviders` (hidden-provider rendering, zero-provider collapse, pending-state behavior).
- No breaking changes to existing API contracts; the new endpoint is additive.
