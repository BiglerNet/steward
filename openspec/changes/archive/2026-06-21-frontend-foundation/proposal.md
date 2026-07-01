## Why

The backend now covers auth, households, assets/engines, tracking records, and document storage, but the frontend is still the default Vite scaffold — no routing, no auth screens, no layout. Every subsequent frontend change (assets/tracking UI, documents UI) needs a working shell to plug into: login/register, protected routing, a household switcher (households are the multi-tenancy boundary), and an authenticated layout. This change builds that shell first.

## What Changes

- Add `react-router` route structure: public routes (`/login`, `/register`, `/auth/callback`), protected routes nested under an authenticated layout, redirecting unauthenticated users to `/login`.
- Add a TanStack Query client + provider, and an axios response interceptor that attaches the JWT to outgoing requests and redirects to `/login` on a `401`.
- Add an `AuthContext` (current user, JWT, login/logout/register actions) backed by `localStorage` persistence so a page refresh doesn't lose the session.
- Add Login and Register pages (email/password) using `react-hook-form` + `Zod`, plus a "continue with Google/Facebook/Apple" set of buttons that redirect to the existing `GET /api/auth/oauth/{provider}/login` endpoints.
- Add an `/auth/callback` page that reads the `code` query param, calls `POST /api/auth/oauth/exchange`, stores the resulting session, and redirects into the app — completing the OAuth flow the backend already exposes.
- Add a pending-invites banner/list on first login (using `AuthResponse.pendingInvites`) with accept actions calling `POST /api/auth/invites/{code}/accept`.
- Add an authenticated app shell: top nav with a household switcher (listing `GET /api/households`, persisting the selected household in the URL/local storage), user menu (display name, avatar, logout), and a content outlet for nested routes.
- Add a "Create household" flow and a basic household settings page (rename, members list, invite-by-email, revoke member/invite) using the already-built `households`/`household-memberships` endpoints.
- Add a global toast/error-display pattern for API errors (validation `ProblemDetails`, 403, 404) reused by all future frontend changes.

## Capabilities

### New Capabilities
- `frontend-auth`: Login, registration, OAuth login/callback, session persistence, logout, pending-invite acceptance.
- `frontend-shell`: Authenticated app layout, household switcher, navigation, global error/toast handling.
- `frontend-household-management`: Household creation, settings, member list, invite/revoke UI.

### Modified Capabilities
- (none)

## Impact

- **Web**: New `src/routes/` (or `src/pages/`) tree, `src/context/AuthContext.tsx`, `src/lib/queryClient.ts`, axios interceptor in `src/api/client.ts`, new shadcn/ui components as needed (form, dialog, dropdown-menu, toast/sonner), new generated-types usage from `src/api/schema.d.ts`.
- **Dependencies**: Adds `react-router` route definitions (already a dependency), likely adds a toast library (e.g. `sonner`) and `@radix-ui` primitives for dialog/dropdown if not already present via shadcn.
- **No backend changes** — this change only consumes existing endpoints (`auth`, `households`, `household-memberships`).
