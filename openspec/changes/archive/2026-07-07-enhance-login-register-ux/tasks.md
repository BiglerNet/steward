## 1. Backend: OAuth provider configuration discovery (Application/Api)

- [x] 1.1 [Application] Add `OAuthProvidersResponse(bool Google, bool Facebook, bool Apple)` DTO to `Steward.Application/Auth/Dtos.cs`.
- [x] 1.2 [Api] Add `GET /api/auth/oauth/providers` to `AuthController`, computing each flag from non-empty `Auth:Google:ClientId` / `Auth:Facebook:ClientId` / `Auth:Apple:ClientId` via `IConfiguration` (already injected into the controller). No authorization required.
- [x] 1.3 [Api] Add/extend a unit or integration test asserting: all-configured, none-configured, and mixed-configuration responses return the correct booleans and no secret values.
- [x] 1.4 [Web] Run `npm run generate:api` against the running API to regenerate `src/api/schema.d.ts` with the new endpoint.

## 2. Frontend: provider-availability data layer (Web)

- [x] 2.1 [Web] Add an `api/auth.ts` (or extend the existing auth API module) function to call `GET /api/auth/oauth/providers` using the typed client.
- [x] 2.2 [Web] Add a `useOAuthProviders` TanStack Query hook under `src/hooks/`, following the conventions of `useAssets`/`useEngines`/`useHouseholds`.

## 3. Frontend: OAuth assets and button rework (Web)

- [x] 3.1 [Web] Confirm the 6 brand SVG assets exist at `src/assets/oauth/{google,facebook,apple}-{light,dark}.svg`; if any are missing, stop and flag it rather than substituting placeholder art. **Scope reduced**: only `google-light.svg`/`google-dark.svg` exist; Facebook/Apple assets are not yet supplied. Per product owner direction (2026-07-07), scope for this change is limited to Google only — Facebook/Apple remain wired on the backend (`GET /api/auth/oauth/providers` still reports all three) but the frontend button list only includes Google until brand assets are provided in a follow-up change.
- [x] 3.2 [Web] Rework `OAuthButtons.tsx` to consume `useOAuthProviders`, filtering the rendered provider list to only those flagged `true`; render each button with its light/dark brand icon based on the app's current color scheme (match the existing dark-mode detection pattern used elsewhere in the frontend rather than introducing a new one). **Scope reduced to Google only** (see 3.1).
- [x] 3.3 [Web] Add pending/redirecting state: on click, show a spinner on the clicked button and disable sibling provider buttons until navigation occurs.
- [x] 3.4 [Web] Ensure `OAuthButtons` renders `null` when no providers are enabled, and update `LoginPage`/`RegisterPage` so the "or continue with" divider is only rendered when at least one provider is enabled (hoist the "any enabled" check via the same hook rather than leaving an orphaned divider).
- [x] 3.5 [Web] Update/add tests covering: only-configured-providers-render, zero-providers-collapses-section (including the divider), and pending-state-on-click.

## 4. Frontend: page layout reorder (Web)

- [x] 4.1 [Web] Reorder `LoginPage.tsx` so the OAuth block (when rendered) appears above the email/password form.
- [x] 4.2 [Web] Reorder `RegisterPage.tsx` the same way.
- [x] 4.3 [Web] Update `LoginPage.test.tsx` / `RegisterPage.test.tsx` for the new layout order if any tests assert DOM order.

## 5. Frontend: registration password UX (Web)

- [x] 5.1 [Web] Add `confirmPassword` to `RegisterPage`'s zod schema with a `.refine()`/`superRefine` check that it matches `password`, and render the field with its own `FormMessage`.
- [x] 5.2 [Web] Add a show/hide (`Eye`/`EyeOff` from `lucide-react`) visibility toggle to both the `password` and `confirmPassword` inputs.
- [x] 5.3 [Web] Add a live per-rule requirements checklist under the password field (8+ characters; at least one non-alphanumeric character), driven by `form.watch("password")`, updating as the user types — matching only the rules already enforced by the schema.
- [x] 5.4 [Web] Update `RegisterPage.test.tsx` to cover: mismatched confirmation blocks submission, visibility toggle switches input type without clearing value, and live requirement indicators flip as the user types.

## 6. Verification

- [x] 6.1 Run `dotnet build` and `dotnet test tests/Steward.UnitTests` (and integration tests if the new endpoint gets integration coverage).
- [x] 6.2 Run `npm run lint`, `npm test`, and `npm run build` in `src/Steward.Web`.
- [x] 6.3 Manually exercise `/login` and `/register` with zero, one, and all three providers configured (via local `appsettings`/env overrides) to confirm hide/collapse behavior end-to-end. **Deferred to user** (2026-07-07): no browser automation tool available in this session; automated coverage (14 dedicated tests) already exercises the same scenarios via jsdom, but a real-browser pass is still worth doing before merge.
