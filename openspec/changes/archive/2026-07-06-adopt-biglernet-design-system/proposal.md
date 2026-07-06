## Why

The frontend's visual system is currently a one-off set of hand-authored hex tokens (`docs/design/tokens.md` / `index.css`) with no dark mode. A sibling project, `../biglernet-design-system`, is Patrick's first attempt at a shared, OKLCH-based design system meant to give BiglerNet products a consistent, professional look — and its own roadmap names "internal product adoption" as its next milestone. Adopting it here gives maintenance-tracker a more polished look, gives the design system its first real production consumer (surfacing gaps to fix upstream), and closes a real gap — the app currently ships a `.dark` Tailwind variant that is fully wired in CSS but has no way for a user to ever trigger it.

## What Changes

- Re-theme the frontend by remapping shadcn/Tailwind's CSS variables in `index.css` to resolve to `biglernet-design-system`'s OKLCH tokens (`tokens/light.css` / `tokens/dark.css`), replacing `docs/design/tokens.md` as the source of truth for color, radius, and type-scale values. All existing shadcn/ui + Radix components (Dialog, Select, DropdownMenu, Tabs, Avatar, Sonner) keep their current markup and interaction behavior — only the token values they resolve to change.
- Load the Inter and IBM Plex Mono webfonts the design system's tokens reference but does not bundle.
- **Prerequisite fix in `../biglernet-design-system` itself** (separate repo, done first): rename the `.theme-dark` class to `.dark` in `tokens/dark.css` (and `prototype.html`) so it matches Tailwind v4's default dark-mode convention, and remove a dead duplicate `--border`/`--border-subtle` declaration in the same file.
- Add a three-state (`Light` / `Dark` / `System`) theme preference that is:
  - resolved from `localStorage` pre-authentication and before `AuthContext` hydrates (avoids flash of wrong theme on `/login`, `/register`),
  - resolved from the OS `prefers-color-scheme` when no explicit choice has ever been saved,
  - persisted server-side once a user is authenticated, taking over as the source of truth, and synced back to `localStorage` on change.
- Add the first user-profile-mutation endpoint in the API (`ApplicationUser` currently only supports reads via `GET /api/auth/me`) to update the stored theme preference.
- Add the theme control (Light/Dark/System) to the existing `UserMenu` dropdown, above "Log out".

**Not in scope:** replacing shadcn/Radix markup with the design system's own `ds-btn`/`ds-input`/etc. CSS classes (the design system has no accessible Dialog/Select/Dropdown/Avatar/Toast components yet — that's its own unstarted Phase 4); Style Dictionary / JSON token source-of-truth tooling (the design system's own deferred Phase 1).

## Capabilities

### New Capabilities
- `theme-preference`: three-state (Light/Dark/System) user theme preference, its resolution order across anonymous/pre-auth and authenticated states, server-side persistence, and the UserMenu control that sets it.

### Modified Capabilities
- `design-system`: token source of truth moves from `docs/design/tokens.md`'s hand-authored hex values to `biglernet-design-system`'s OKLCH tokens; adds a dark theme (previously the spec had no dark-mode requirement at all); adds a webfont-loading requirement.
- `identity-and-auth`: `ApplicationUser` gains a `ThemePreference` field alongside `DisplayName`/`AvatarUrl`.

## Impact

- **Repo: `../biglernet-design-system`** — `tokens/dark.css`, `prototype.html` (class rename + dead-code cleanup). Small, done before the rest of this change starts.
- **`src/Steward.Web/src/index.css`** — token remap (light + dark), font `@import`/`@font-face`.
- **`src/Steward.Web/src/context/AuthContext.tsx`** — expose theme preference + mutation.
- **New: a `ThemeProvider`/theme context** — resolution order across localStorage / OS preference / server value.
- **`src/Steward.Web/src/components/auth/UserMenu.tsx`** — add Light/Dark/System items.
- **`src/Steward.Infrastructure/Identity/ApplicationUser.cs`** — new `ThemePreference` column + EF Core migration.
- **`src/Steward.Application/Auth/Dtos.cs`** — extend `AuthenticatedUser`, `UserProfileResponse`.
- **`src/Steward.Api/Controllers/AuthController.cs`** — new endpoint to update the preference.
- **`package.json`** (frontend) — add `@biglernet/design-tokens` dependency (GitHub Packages), plus `.npmrc` registry scoping.
