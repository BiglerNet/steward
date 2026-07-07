## Context

Today `OAuthButtons.tsx` hardcodes `google`/`facebook`/`apple` and always renders all three as plain `<a href>` anchors below the email/password form on both `LoginPage` and `RegisterPage`. The backend (`AuthController`, `Auth:Google/Facebook/Apple` config in `appsettings.json`) has no endpoint exposing which providers are actually configured — a self-hosted instance that hasn't set up, say, Facebook OAuth still shows a "Continue with Facebook" button that will fail. Registration's password field has no confirmation, no visibility toggle, and no live feedback on the (already-enforced) password policy — only a post-submit regex error.

Product direction: SSO is the primary intended sign-in path, so the OAuth block moves above the password form, and only providers an operator has actually configured should be visible at all.

This change was scoped end-to-end in an explore-mode conversation; all decisions below were confirmed with the product owner rather than being open design choices.

## Goals / Non-Goals

**Goals:**
- Frontend can determine, before rendering, which OAuth providers are usable on this deployment.
- Unconfigured providers are hidden entirely (not shown disabled); if zero providers are configured, the whole OAuth section (buttons + divider) is omitted.
- OAuth buttons carry official brand iconography (light/dark variants) and a visible pending state during redirect.
- Login/Register pages present OAuth above the password form.
- Registration guards against password typos via a confirm field, and improves the authoring experience with a show/hide toggle and a live requirements checklist.

**Non-Goals:**
- No forgot/reset-password flow (explicitly deferred — SSO is primary, password recovery is a future change).
- No change to `AuthCallbackPage`'s generic OAuth-failure toast/messaging.
- No change to the existing silent OAuth account-linking-by-email behavior in `AuthService.HandleOAuthCallbackAsync` — linking multiple providers to one email by matching address is intentional and stays as-is.
- No change to JWT/session/refresh-token behavior.
- No expansion of the password policy itself (still min 8 chars + 1 non-alphanumeric) — the strength hint reflects existing rules, it doesn't add new ones.
- Does not source or vet the actual brand icon assets — those are supplied manually by the repo owner from each provider's official brand resource pages (Google Identity branding guidelines, Apple Design Resources, Meta's Brand Resource Center) and dropped into the repo; this change only wires up consumption of those files.

## Decisions

### 1. Provider availability via a new backend endpoint, not static config injection
Add `GET /api/auth/oauth/providers` (unauthenticated, alongside the existing `oauth/{provider}/login` and `oauth/{provider}/callback` routes on `AuthController`) returning a small DTO, e.g.:
```json
{ "google": true, "facebook": false, "apple": false }
```
Each flag is `true` when that provider's client ID configuration key (`Auth:Google:ClientId`, `Auth:Facebook:ClientId`, `Auth:Apple:ClientId`) is non-empty in `IConfiguration`.

**Alternative considered**: extend `window.__APP_CONFIG__` (the existing runtime-injected config used today for `apiBaseUrl`) with provider flags at container-entrypoint time. Rejected because the backend already owns this configuration natively; a second static injection point would require whatever writes `__APP_CONFIG__` to independently know which OAuth secrets exist, duplicating knowledge that only the API process should need.

Add the DTO to `Steward.Application/Auth/Dtos.cs` (e.g. `OAuthProvidersResponse(bool Google, bool Facebook, bool Apple)`) per existing convention of DTOs living in Application, not Api. Run `npm run generate:api` after adding the endpoint so `api/schema.d.ts` picks it up, then add a typed frontend hook (e.g. `useOAuthProviders`, TanStack Query, following the pattern of `useAssets`/`useEngines`/`useHouseholds` in `src/Steward.Web/src/hooks/`).

### 2. Hide unconfigured providers; collapse the whole block when none are configured
`OAuthButtons` filters its provider list down to only those flagged `true` by `useOAuthProviders`. If the resulting list is empty, the component renders `null` — and the parent pages (`LoginPage`/`RegisterPage`) must also omit the "or continue with" divider in that case, not just the buttons, to avoid a dangling divider with nothing under it. Simplest approach: `OAuthButtons` itself returns `null` when no providers are enabled, and the divider is rendered as a sibling only when `OAuthButtons` will render something — i.e. the page hoists the "any providers enabled" check (from the same `useOAuthProviders` hook) rather than `OAuthButtons` conditionally hiding just itself while a stray divider remains above/below it in the page.

### 3. Icons: static SVG assets, light/dark variant per provider
No npm icon library is used — `lucide-react` (already a dependency) dropped brand logos, and generic brand-icon packages (e.g. react-icons/simple-icons) recolor logos in ways that violate Google's and Apple's brand guidelines. Assets are plain SVG files at `src/Steward.Web/src/assets/oauth/{provider}-{light,dark}.svg` (6 files: google, facebook, apple × light, dark), sourced manually by the repo owner from each provider's official brand pages. `OAuthButtons` picks the light/dark variant based on the app's current theme (mirroring however theme is currently read — check `ThemePreference`/`AuthContext` or existing dark-mode CSS handling for the established pattern rather than inventing a new one).

### 4. Loading/pending state on click
On click, the clicked provider button swaps its label/icon for a spinner (or spinner + "Redirecting…"), and sibling provider buttons become disabled, until the full-page navigation to `GET /api/auth/oauth/{provider}/login` actually occurs. Since the anchor navigation itself will unmount the component shortly after, this state only needs to survive the moment between click and browser navigation — no need to reset it (there's nothing to reset once the page navigates away).

### 5. Register form: confirm password, show/hide toggle, live strength hint
- Add `confirmPassword` to the Zod schema, validated via `.refine()` (or `superRefine`) that `confirmPassword === password`, surfaced via its own `FormMessage`.
- Add an eye/eye-off toggle (`lucide-react` `Eye`/`EyeOff`, matching the existing icon usage like `Wrench` on these pages) to both `password` and `confirmPassword` inputs, toggling between `type="password"` and `type="text"`.
- Add a live checklist under the password field reflecting the two existing rules (8+ characters; at least one non-alphanumeric character), each showing pass/fail as the user types (driven off `form.watch("password")`, not off submit/blur state). Do not add rules beyond what the schema already enforces.
- Login's password field is left untouched (single field, no confirm, no strength hint) — it's authenticating against an existing password, not authoring one. Whether Login should also get the show/hide toggle for consistency is left as an open question below rather than silently decided either way.

### 6. Page layout: OAuth above password form
`LoginPage` and `RegisterPage` swap section order: OAuth block (or nothing, if collapsed) renders first, divider, then the email/password form. Applies symmetrically to both pages.

## Risks / Trade-offs

- **Asset availability blocks frontend work** → the 6 SVG files are manually sourced by the repo owner and may not exist yet when implementation starts. Mitigation: implement `OAuthButtons` to reference the expected file paths and treat missing files as a build/lint failure to surface immediately, rather than substituting placeholder art.
  - **Resolution (2026-07-07)**: only the Google assets were supplied by implementation time. Product owner decided to limit this change's frontend scope to Google only rather than block on Facebook/Apple assets. `OAuthButtons` only lists Google in its provider/icon table; the backend `GET /api/auth/oauth/providers` endpoint still reports all three providers unchanged, so adding Facebook/Apple to the frontend later is just adding entries to that table once their SVGs land — no backend change needed.
- **Config-check is a simple non-empty string check** → doesn't validate the credentials actually work (e.g. wrong secret), only that something was entered. Acceptable: this endpoint answers "is this provider set up at all," not "is this provider working," which matches the stated goal (don't show a button with literally no way to work).
- **Un-authenticated new endpoint** → `GET /api/auth/oauth/providers` must not leak the actual client ID/secret values, only booleans. Keep the DTO strictly boolean-shaped.

## Open Questions

- Should Login's password field also get the show/hide toggle for consistency with Register, even though it wasn't explicitly requested? Default (per proposal scope) is Register-only unless implementation judges the inconsistency is worse than the extra scope.
- Confirm whether the theme (light/dark) used to pick the icon variant should follow the app's `ThemePreference` setting/system preference, or something simpler (e.g. CSS `prefers-color-scheme` media query swapping `<img>` `src` via CSS rather than JS) — whichever matches how dark mode is already implemented elsewhere in the frontend.
