## Context

The frontend (`src/Steward.Web`) is built on Tailwind CSS 4 + shadcn/ui, with every primitive (`button.tsx`, `input.tsx`, `table.tsx`, `tabs.tsx`, etc.) consuming semantic CSS variables defined in `index.css` (`--background`, `--primary`, `--accent`, `--muted-foreground`, `--radius`, ...) rather than hardcoded colors. An OpenDesign session produced `docs/design/tokens.md` (palette, type scale, spacing, radii) and `docs/design/screen-specs.md` plus static HTML mockups for several screens. That session predates several already-shipped frontend capabilities (registrations/warranties as asset-detail tabs, document attachment per registration/warranty, household invites) and also proposed IA changes (Dashboard, unified New Entry picker, generic Docs tab) that are explicitly out of scope here — see proposal.md.

This design covers only the visual/layout retrofit: re-pointing the existing token layer at the new palette/type-scale, and reshaping the handful of screens whose current markup is too minimal to take the new tokens well (bare `<li>` asset cards, a header with no nav links).

## Goals / Non-Goals

**Goals:**
- One source of truth for color/radius/type-scale values (`index.css`), matching `docs/design/tokens.md`, that all current and future components inherit automatically through Tailwind's `@theme inline` mapping.
- Visually align `AuthenticatedLayout`, `AssetListPage`, `AssetDetailLayout`, household pages, and auth pages with their corresponding `docs/design/*.html` mockups, without changing the routes, data fetching, or component public APIs (props) any more than necessary.

**Non-Goals:**
- No new backend endpoints or schema changes (Dashboard stats are a separate future change).
- No asset card status badge — `AssetResponse` (the list-endpoint shape) carries no service-history or registration-expiry data, and deriving one would require N+1 fetches per card, which we're avoiding in this change. Revisit once the Dashboard/stats change exists.
- No change to the asset-detail tab set, order, or routing.
- No generic document model (Docs tab dropped).
- No change to form validation, submission, or business logic on any restyled page — this is template/className-level work only.

## Decisions

**1. Token swap lives entirely in `index.css`, mapped through the existing `@theme inline` block.**
Current variables (`--background`, `--accent`, etc.) keep their names; only their values change to match `tokens.md` (e.g. `--background: #f7f7f5`, `--primary`/new `--color-accent-DEFAULT: #2f9e44`). This means no component file needs to change just to pick up the new palette — only components whose current markup doesn't yet express a "surface card" or "stat" concept need structural edits.
*Alternative considered*: introduce a second, parallel set of design-system variables (`--ds-bg`, `--ds-accent`, ...) and migrate components incrementally. Rejected — doubles the token surface for no benefit since there's only one active theme today (dark mode classes exist but aren't wired to a toggle).

**2. No status badge in this change.** Confirmed during design that `GET /api/households/{id}/assets` returns plain `AssetResponse[]` (no service-history or registration-expiry fields), so deriving Active/Maintenance-due/Inactive would require an N+1 fetch per card. Rather than add hidden N+1 calls or new backend aggregation here, the badge is dropped from scope entirely (see proposal.md "Not included"). The asset card restyle ships with the icon swatch and layout/hover polish only.

**3. `AuthenticatedLayout` nav links point at routes that exist today**, not the spec's hypothetical `/entries/new`. The link set is: Assets list (`/households/:id/assets`), Household overview/settings (existing routes), and the household switcher + user menu stay where they are. No "New Entry" link is added since that unified flow doesn't exist.

**4. Tab bar restyle is CSS-only.** `AssetDetailLayout`'s `<NavLink>` list keeps its current 7 entries, `to` values, and `Outlet` structure; only the `className` logic changes from the segmented-control look to the underline-tab look (`border-bottom` active state) from `tokens.md`.

**5. Asset-type color swatches use a static lookup map** (`assetTypeIconColors: Record<AssetType, string>`) seeded from `tokens.md`'s `--light-asset-types`, colocated with `ASSET_TYPE_LABELS` in `assetTypeFieldConfig.ts`, since that's the existing pattern for per-type metadata.

## Risks / Trade-offs

- **[Risk]** Swapping CSS variable values globally could visually break a page nobody reviewed (e.g. `dialog.tsx`, `sonner.tsx` toast styling) since the change touches every consumer at once. → **Mitigation**: do a manual pass through every page/dialog after the token swap (login, register, asset list/detail/create/edit dialogs, household pages, toasts) before calling the change done; this is a visual change, so verification is manual review, not unit tests.
- **[Risk]** Tailwind 4's `@theme inline` + arbitrary CSS variables can behave differently than utility classes for things like hover shadow values from `tokens.md` (`box-shadow: 0 4px 16px rgba(0,0,0,0.1)`) which aren't simple color tokens. → **Mitigation**: where Tailwind's default shadow utilities don't match, add the literal value as an arbitrary-value utility (`shadow-[0_4px_16px_rgba(0,0,0,0.1)]`) rather than inventing new theme tokens for one-off values.

## Migration Plan

Purely additive/visual — no data migration. Roll out as a single frontend deploy; rollback is reverting the commit/PR, since there's no persisted state involved.

## Open Questions

None outstanding — the only open question (asset-list data shape for a status badge) was resolved during design: the data isn't available without N+1 fetches, so the badge is out of scope (see Decision 2).
