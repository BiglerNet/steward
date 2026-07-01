## 1. Routing and App Shell Skeleton

- [x] 1.1 Add `react-router` route tree: `/login`, `/register`, `/auth/callback` (public); `/households/:householdId/*` (protected, wrapped in the authenticated layout); root `/` redirect logic.
- [x] 1.2 Add `ProtectedRoute`/`PublicOnlyRoute` wrapper components driven by `AuthContext`.
- [x] 1.3 Add `src/lib/queryClient.ts` and wrap the app in a TanStack Query `QueryClientProvider`.
- [x] 1.4 Add a toast provider (`sonner` or equivalent) at the app root.

## 2. Session and Auth Plumbing

- [x] 2.1 Add `src/context/AuthContext.tsx`: stores `user`, `token`, `expiresAt`, `pendingInvites`; exposes `login`, `register`, `exchangeOAuthCode`, `logout`; persists/restores via `localStorage`.
- [x] 2.2 Update `src/api/client.ts` with a request interceptor attaching `Authorization: Bearer <token>` and a response interceptor that clears the session and redirects to `/login` on `401`.
- [x] 2.3 Add typed API calls for `register`, `login`, `oauthExchange`, `me`, `acceptInvite` using the generated `schema.d.ts` types.

## 3. Auth Pages

- [x] 3.1 Add `/login` page: email/password form (`react-hook-form` + Zod), OAuth buttons (Google/Facebook/Apple) linking to `GET /api/auth/oauth/{provider}/login`.
- [x] 3.2 Add `/register` page: email/password/display-name form, same OAuth buttons.
- [x] 3.3 Add `/auth/callback` page: reads `code` from query params, calls `exchangeOAuthCode`, redirects into the app or back to `/login` with an error toast on failure.
- [x] 3.4 Add inline field-error rendering for `ValidationProblem` responses on both forms.

## 4. Authenticated Shell

- [x] 4.1 Add the authenticated layout component: top nav, household switcher, user menu (display name/avatar, logout).
- [x] 4.2 Add household-list query (`GET /api/households`) and the switcher dropdown; persist last-selected household ID to `localStorage`; navigate to the equivalent path under the new household ID on switch.
- [x] 4.3 Add the "no households yet" empty state prompting household creation.
- [x] 4.4 Add the pending-invites banner and invites list page, wired to `AuthContext.pendingInvites` and `POST /api/auth/invites/{code}/accept`.
- [x] 4.5 Add the global API-error-to-toast handling (axios interceptor or TanStack Query error handler) for unhandled `403`/`404`/`5xx`.

## 5. Household Management UI

- [x] 5.1 Add "Create household" dialog/page calling `POST /api/households`.
- [x] 5.2 Add household settings page: rename form (`PUT /api/households/{id}`), gated to Owner/Contributor.
- [x] 5.3 Add member list (`GET /api/households/{id}/members`) with role display.
- [x] 5.4 Add invite-by-email form (`POST /api/households/{id}/members/invite`) and pending-invitations list with revoke (`DELETE /api/households/{id}/invitations/{code}`).
- [x] 5.5 Add remove-member action (`DELETE /api/households/{id}/members/{userId}`).

## 6. Component Library Additions

- [x] 6.1 Add shadcn `form`, `dialog`, `dropdown-menu` components as needed by the above pages.
- [x] 6.2 Add a `sonner` (or chosen) toast component, themed consistently with existing Tailwind setup.

## 7. Tests

- [x] 7.1 `AuthContext` unit tests: login/register/logout/session persistence/restoration.
- [x] 7.2 Login/Register page tests: happy path, validation error rendering.
- [x] 7.3 OAuth callback page test: successful exchange, failed exchange.
- [x] 7.4 Protected-route redirect tests (unauthenticated → `/login`, authenticated → away from `/login`/`/register`).
- [x] 7.5 Household switcher test: switching navigates to the equivalent path under the new household ID.
- [x] 7.6 Household management tests: create, rename, invite, revoke, remove-member happy paths and a forbidden-action toast case.

## 8. Manual Verification

- [x] 8.1 Full flow against the local Docker Compose stack: register → create household → invite a second (test) account by email → log in as that account → accept invite → switch households.
- [x] 8.2 OAuth flow against at least one real provider (Google) in a dev OAuth app, end to end.
- [x] 8.3 Confirm token-expiry redirect by waiting out the 15-minute JWT lifetime (or temporarily shortening `Jwt:ExpiryMinutes` locally) and confirming a stale session redirects to `/login` cleanly.
