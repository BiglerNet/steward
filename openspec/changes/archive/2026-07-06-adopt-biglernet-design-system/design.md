## Context

The frontend today gets its look from two layers: shadcn/ui components (Radix primitives + Tailwind utility classes) styled by CSS variables hand-authored in `src/Steward.Web/src/index.css`, sourced from `docs/design/tokens.md` (hex colors, no OKLCH, no dark mode). `../biglernet-design-system` is a sibling repo — Patrick's first design system — that ships OKLCH color tokens, a spacing/radius/type scale, and plain-CSS component classes (`ds-btn`, `ds-input`, `ds-table`, etc.) as an npm package (`@biglernet/design-tokens`, distributed via GitHub Packages). Its own `ROADMAP.md` names "Phase 3 — Internal Product Adoption" as the next step and explicitly expects the first consumer to surface gaps.

The design system covers visual tokens and inert CSS component classes only; it has no Dialog, Select, DropdownMenu, Avatar, or Toast component (that's its own unstarted "Phase 4 — Framework Components"). It also doesn't bundle the fonts (`Inter`, `IBM Plex Mono`) its tokens reference, and its dark-theme file keys off a `.theme-dark` class while this app's Tailwind config expects `.dark`.

Separately, the app has zero mechanism today for a user to enter dark mode — `.dark` exists only as an unused Tailwind variant target.

## Goals / Non-Goals

**Goals:**
- Re-theme the app to the design system's OKLCH palette without touching shadcn/Radix behavior (accessibility, focus management, keyboard nav stay exactly as they are).
- Ship dark mode as a real, persisted, three-state user preference (Light/Dark/System) — not a CSS variant nobody can reach.
- Fix the one cross-repo blocker (`.theme-dark` vs `.dark`) upstream, in the design system itself, so future consumers don't hit it.

**Non-Goals:**
- Replacing shadcn/Radix markup with the design system's own `ds-*` classes ("Option B — full replace"). The design system doesn't have accessible equivalents for Dialog/Select/DropdownMenu/Avatar/Toast yet; building those is out of scope here and belongs in the design system's own Phase 4.
- Style Dictionary / JSON token source-of-truth tooling — the design system's own deferred Phase 1.
- Any new admin/settings page. The theme control lives in the existing `UserMenu` dropdown only.

## Decisions

### 1. Reskin via CSS variable remap, not markup replacement

Keep every existing shadcn component (`button.tsx`, `dialog.tsx`, `select.tsx`, `dropdown-menu.tsx`, `tabs.tsx`, `avatar.tsx`, `table.tsx`, `sonner.tsx`, `form.tsx`, `label.tsx`, `input.tsx`, `card.tsx`) untouched. Only `index.css`'s `:root`/`.dark` variable *values* change, from hand-authored hex to `var()` references into the design system's tokens.

**Alternative considered:** swap to `ds-btn`/`ds-input`/etc. directly. Rejected — would require hand-building accessible Dialog/Select/Dropdown/Avatar/Toast equivalents (a Phase-4-sized effort) just to keep the app functional, for a purely cosmetic goal.

### 2. Token mapping — resolve the `--accent`/`--muted` semantic collision

shadcn's `--accent` and `--muted` are **background roles** for neutral hover/highlight surfaces (dropdown item hover, outline/ghost button hover). The design system's `--accent-primary` is the **brand color** (red), governed by its own rule: *"BiglerNet red appears at most twice on any surface."* Mapping shadcn's `--accent` straight to `--accent-primary` would tint every menu-item hover and outline-button hover red, violating that rule on sight.

| shadcn variable | role | → design-system token |
|---|---|---|
| `--background` | page bg | `--bg` |
| `--foreground` | body text | `--fg` |
| `--card`, `--popover` | surface bg | `--surface` |
| `--card-foreground`, `--popover-foreground` | surface text | `--fg` |
| `--border`, `--input` | hairline borders | `--border` |
| `--primary` | brand action | `--accent-primary` |
| `--primary-foreground` | on-brand text | `#fff` |
| `--secondary` | secondary button bg | `--surface` |
| `--secondary-foreground` | secondary button text | `--fg` |
| `--muted` (bg role) | neutral hover bg | `--surface-hover` |
| `--muted-foreground` (text role) | de-emphasized text | `--muted` *(design system's `--muted` is itself a text-color token, not a background — this is the naming collision, not a mapping error)* |
| `--accent` (bg role) | neutral hover bg | `--surface-hover` — **not** `--accent-primary` |
| `--accent-foreground` | text on hover bg | `--fg` |
| `--destructive` / `--destructive-foreground` | error action | `--danger` / `#fff` |
| `--warning` / `--warning-bg` | warning state | `--warn` / `--warn-bg` *(name mismatch only)* |
| `--success` / `--success-bg` | success state | `--success` / `--success-bg` *(exact match)* |
| `--ring` | focus ring | not a flat color — apply the design system's `--shadow-focus` (`0 0 0 3px var(--accent-light)`) as a `box-shadow` on `:focus-visible`, matching how `.ds-input:focus` already behaves |
| `--radius` | corner radius | `--radius-md` (8px) — matches the current 0.5rem exactly |

### 3. Fonts must be loaded explicitly

`--font-display: 'Inter', ...` and `--font-mono: 'IBM Plex Mono', ...` are declared in `tokens/light.css` but no `@font-face`/webfont link ships with the package. Load both via self-hosted `@font-face` (preferred over a Google Fonts runtime request, consistent with this being a self-hosted product) in `index.css`, with the existing system-font stacks as fallback exactly as already declared.

### 4. Upstream fix lands first, in the design system repo

Rename `.theme-dark` → `.dark` in `../biglernet-design-system/tokens/dark.css` and `prototype.html`, and delete the dead duplicate `--border`/`--border-subtle` declaration in the same block (an oklch value immediately shadowed by an rgba value a few lines later). This is a prerequisite step, done and tag/version-bumped in that repo before maintenance-tracker consumes it, so the app never needs a class-name shim.

### 5. Theme preference: three states, layered resolution, server as source of truth once authenticated

`Light` / `Dark` / `System` (not a binary toggle) — a two-state toggle can't express "go back to following the OS," which is explicitly wanted.

Resolution order, evaluated at app boot and re-evaluated whenever its inputs change:
1. **Authenticated user with a stored preference** → server value (`AuthenticatedUser.themePreference` from `/api/auth/me` or the login/register response) wins.
2. **No authenticated user yet** (first paint, `/login`, `/register`, before `AuthContext` hydrates) → `localStorage` value if present.
3. **No stored value anywhere** → OS `prefers-color-scheme` media query.
4. Resolves `System` (states 1 or 2) → OS `prefers-color-scheme` at render time, live-updating if the OS setting changes mid-session.

Writes: changing the selection in `UserMenu` updates local React state immediately (instant repaint), writes to `localStorage` unconditionally (keeps pre-auth/offline-first paint correct), and — only if authenticated — fires the update endpoint. A logged-out user can still set a preference; it just isn't synced anywhere but this device until they log in, at which point the existing server value (if any) wins on their **next** login, since this change does not implement a merge/prompt flow for that conflict (see Open Questions).

### 6. New endpoint: `PATCH /api/auth/me/theme`

Scoped narrowly (not a general profile-update endpoint — no other profile fields are editable yet, and inventing that surface is out of scope). Body: `{ "themePreference": "Light" | "Dark" | "System" }`. Authorized to the calling user only (`[Authorize]`, acts on `User.GetUserId()`, mirrors the existing `Me()` action's identity resolution). Returns the updated `UserProfileResponse`.

`ThemePreference` is stored as a nullable string-backed enum column on `ApplicationUser` (`Light`/`Dark`/`System`, nullable = never explicitly set = fall through to OS preference per the resolution order above). EF Core migration adds the column with a default of `NULL` for existing rows.

## Risks / Trade-offs

- **[Risk]** A user sets a preference while logged out (e.g. on `/login` before signing in), then logs into an account that already has a different stored preference. The server value silently wins with no merge/notice. → **Mitigation**: acceptable for v1 — this only affects the moment of first login on a new device where a user happened to change the toggle pre-login, which is rare. Flagged as an open question below if it needs revisiting.
- **[Risk]** Hand-picking `--surface-hover` for shadcn's `--accent`/`--muted` roles is a judgment call, not something either source document states explicitly. → **Mitigation**: it directly follows the design system's own stated rule (accent color scarcity); visually verify hover states on outline/ghost buttons and dropdown menu items in both themes before merging.
- **[Risk]** Self-hosting Inter + IBM Plex Mono adds font files to the frontend bundle/static assets that don't exist today. → **Mitigation**: use `font-display: swap` and subset if bundle size becomes a concern; not blocking for v1.
- **[Trade-off]** Keeping shadcn/Radix markup means the design system's own component CSS (`ds-btn`, `ds-table`, etc.) goes unused in this app for now — the "adoption" is tokens-only, not full visual parity with the design system's own style-guide.html. Acceptable given Non-Goals above; can be revisited once the design system grows accessible component equivalents.

## Migration Plan

1. Land the `.theme-dark` → `.dark` rename + dead-code cleanup in `../biglernet-design-system`, bump its version, and consume the updated package in `maintenance-tracker` (`package.json` + `.npmrc` GitHub Packages registry scoping).
2. Remap `index.css` tokens per the table above (light first, verify visually, then dark).
3. Add self-hosted font loading.
4. Ship the backend: migration, DTO fields, `PATCH /api/auth/me/theme` endpoint — independently testable/deployable before the frontend consumes it.
5. Add the frontend `ThemeProvider` (resolution order + persistence) and wire `AuthContext`.
6. Add the `UserMenu` control.
7. No rollback complexity beyond a standard revert — the new column is nullable and additive; no data backfill needed.

## Open Questions

- Should a pre-login local preference ever prompt/merge against a conflicting server value on first login, or is silent server-wins acceptable indefinitely?
- Should the webfonts be self-hosted (bundled with the frontend) or does Patrick want them added to the design system package itself as a follow-up (so every consumer gets them, not just this app)? Assumed self-hosted-here for this change; worth raising with the design system's own backlog separately.
