## Context

The frontend currently has the bare Vite scaffold from `core-solution-structure`/`containerization`: an axios client reading a runtime-injected API base URL, generated OpenAPI types, one shadcn `Button`, and a placeholder `App.tsx`. The backend exposes JWT bearer auth (15-minute token lifetime, no refresh-token endpoint), OAuth via redirect+code-exchange, and household-scoped resource routes (`/api/households/{householdId}/...`) for everything downstream. This change has to settle the session/routing/layout shape every later frontend change builds on.

## Goals / Non-Goals

**Goals:**
- Working login/register/OAuth flows against the existing `AuthController` endpoints.
- Session persistence across page refresh, with automatic redirect to `/login` on token expiry/401.
- URL-based household scoping (`/households/:householdId/...`) so deep links and refresh both work, with a switcher to change the active household.
- A reusable authenticated layout and error/toast pattern that every subsequent frontend change (assets, tracking, documents) plugs into rather than reinventing.

**Non-Goals:**
- Asset/Engine/tracking-record/document UI — separate future changes.
- Public garage view — separate future change, and explicitly unauthenticated (out of scope for this change's auth-gated shell).
- A refresh-token flow — the backend doesn't have one yet; this change works within the existing 15-minute JWT lifetime and treats expiry as "redirect to login," not as a solvable-here problem.
- Platform admin UI — `PlatformAdminController` exists but has no planned frontend yet; deferred.

## Decisions

### 1. Household scoping lives in the URL, not just in memory/local storage
Routes are nested as `/households/:householdId/...`; switching households navigates to the equivalent path under the new ID. The *last selected* household ID is cached in `localStorage` only to decide where `/` redirects on login.
**Alternative considered**: keep the active household purely in React context/local storage with flat routes (`/assets`, `/assets/:id`) — rejected, it breaks deep-linking/refresh (you'd lose which household you were looking at) and doesn't match the backend's own URL shape, which is already household-scoped everywhere.

### 2. Session: JWT + user profile kept in an `AuthContext`, persisted to `localStorage`, attached via an axios request interceptor
On login/register/OAuth-exchange, the `AuthResponse` (token, expiry, user, pendingInvites) is stored in both React context and `localStorage`. An axios request interceptor adds `Authorization: Bearer <token>`; a response interceptor catches `401`, clears the stored session, and redirects to `/login`.
**Alternative considered**: an httpOnly cookie issued by the API — rejected without a backend change; the API currently returns the JWT in the response body for the client to manage, so the frontend has to work with that shape as given.

### 3. Pending invites surface as a dismissible banner on the authenticated shell, not a blocking modal
`AuthResponse.pendingInvites` populates an `AuthContext` field; the shell renders a banner ("You have 2 pending household invites") with a link to a small invites list where each can be accepted via `POST /api/auth/invites/{code}/accept`.
**Alternative considered**: a blocking "accept your invites before continuing" modal — rejected, a user with zero existing households but a pending invite still needs to be able to look around / create their own household; forcing acceptance is unnecessary friction.

### 4. Form validation: Zod schemas hand-written per form, manually kept in sync with backend FluentValidation rules
There's no automatic generation path from FluentValidation to Zod (the OpenAPI schema doesn't carry validation rules), so each form (login, register, create household, invite member) gets a small hand-written Zod schema mirroring the known backend rules (required fields, email format, password rules).
**Alternative considered**: skip client-side validation and rely entirely on the API's `ValidationProblem` responses — rejected, it's a worse UX (round-trip required to discover a missing field) for zero implementation savings given `react-hook-form` + `zodResolver` is already a project dependency.

### 5. New shadcn/ui components added as needed: `form`, `dialog`, `dropdown-menu`, and a toast library (`sonner`)
These are added via the shadcn CLI / matching Radix primitives as each is needed (form fields, the household-switcher dropdown, the create-household dialog, and global toasts for API errors/success), rather than scaffolding all of shadcn's catalog upfront.
**Alternative considered**: pull in the full shadcn component set now — rejected as unnecessary upfront bulk; add components when a concrete screen needs them, consistent with the project's "don't build for hypothetical future requirements" convention.

## Risks / Trade-offs

- **[Risk]** 15-minute JWT with no refresh token means active users get logged out mid-session — **Mitigation**: out of scope to fix here (Non-Goals); the 401-redirect interceptor makes the failure mode "land back on login," not a silent broken state. Flagged for a future backend+frontend refresh-token change if this proves too disruptive in practice.
- **[Risk]** JWT in `localStorage` is readable by any script on the page (XSS exposure) — **Mitigation**: accepted given the current backend contract (token returned in response body, not a cookie); standard React/JSX escaping limits injection vectors, and this is consistent with the token-in-body design already shipped in `auth-and-households`.
- **[Risk]** Hand-written Zod schemas can drift from backend validators over time — **Mitigation**: client-side validation is a UX nicety, not the source of truth; the API's own `ValidationProblem` responses remain authoritative and are surfaced via the global error/toast pattern (Decision 5) even if a Zod schema is stale.

## Migration Plan

No backend/database changes. Purely additive frontend work; rollback is reverting the frontend commit(s), no data migration involved.

## Open Questions

None — Non-Goals above cover the deferred items (refresh tokens, platform admin UI, public garage view, downstream domain UI).
