## Why

An OpenDesign session produced a full visual system (`docs/design/tokens.md`, `screen-specs.md`, and HTML mockups) for Steward, but the current frontend still uses the generic shadcn black/white starter theme with no consistent type scale, spacing, or page-level layout. We want the app to look and feel intentional now, without waiting on the larger IA changes (Dashboard, unified New Entry flow) that the same design session also proposed but which need separate backend work or were dropped as out of scope.

## What Changes

- Replace the CSS variable token layer in `index.css` with the warm/green palette, radii, and type scale defined in `docs/design/tokens.md`. Since existing components already consume semantic Tailwind/shadcn tokens (`bg-primary`, `text-muted-foreground`, etc.) rather than hardcoded colors, this cascades through buttons, inputs, cards, and badges with minimal per-component changes.
- Rebuild `AuthenticatedLayout` into a proper app shell: brand mark, persistent primary nav links (Dashboard/My Gear/My Household/Settings-equivalent routes that exist today), and an avatar dropdown menu — reusing existing `UserMenu`, `HouseholdSwitcher`, and `PendingInvitesBanner`.
- Restyle the asset list (`AssetListPage`) into real cards: hover-lift and an asset-type colored icon swatch (per `tokens.md`'s `--light-asset-types`). (No status badge — see Not Included below; `GET /api/households/{id}/assets` doesn't return the service/registration data needed to derive one without N+1 fetches.)
- Restyle the asset detail tab bar (`AssetDetailLayout`) to the underline-tab style from `tokens.md`, and apply detail-card chrome to the header/fields area. The existing 7 tabs (Engines, Service Records, Mileage Logs, Engine Hours Logs, Fuel Logs, Registrations, Warranties) are unchanged in number and order.
- Restyle household pages (`HouseholdSettingsPage`, `HouseholdOverviewPage`/`HouseholdsIndexPage`) into card-based sections matching `household-settings.html` / `households.html`, reusing existing `RenameHouseholdForm`, `MembersPanel`, `CreateHouseholdDialog` components as-is.
- Restyle `LoginPage`/`RegisterPage` to the centered-card layout in `login.html`, including the OAuth divider treatment. Form fields and submit behavior are unchanged.

**Not included** (deliberately excluded, not deferred-but-implied):
- Dashboard page and any stats/upcoming-due aggregation endpoint — needs new backend work, will be its own change.
- A unified "Docs" tab / generic per-asset document entity — the design's assumption of a generic document store doesn't match the current per-registration/per-warranty document model; dropped rather than scoped down.
- Any asset-detail IA change (e.g. moving Registrations/Warranties out of the tab bar into "upcoming renewal" panels) — tabs stay as-is.
- The New Entry unified 2×2 record-type picker and consolidation of per-record-type routes.
- The asset card status badge (Active / Maintenance due / Inactive) from the design mockups — `AssetResponse` (the asset-list response shape) carries no service-history or registration-expiry data, so deriving real status would require N+1 fetches per card. Revisit once the Dashboard/stats change (also out of scope here) adds aggregated data that could double as this badge's source.

## Capabilities

### New Capabilities
- `design-system`: Establishes the token contract (color palette, radii, spacing scale, type scale) that frontend components must consume via CSS variables/Tailwind theme rather than hardcoded values, so future screens stay visually consistent without re-deriving the palette.

### Modified Capabilities
- `frontend-shell`: The authenticated layout gains persistent primary navigation links (not just brand + switcher + user menu) and an avatar-based dropdown for the user menu.

## Impact

- **Affected code**: `src/Steward.Web/src/index.css`, `src/components/layout/AuthenticatedLayout.tsx`, `src/pages/assets/AssetListPage.tsx`, `src/pages/assets/AssetDetailLayout.tsx`, `src/pages/HouseholdSettingsPage.tsx`, `src/pages/HouseholdOverviewPage.tsx`, `src/pages/HouseholdsIndexPage.tsx`, `src/pages/LoginPage.tsx`, `src/pages/RegisterPage.tsx`, and shared `src/components/ui/*` primitives as needed for the new tokens.
- **No backend/API changes.** No new endpoints, no database changes.
- **No routing/IA changes** beyond adding explicit nav links to existing routes.
- **Dependencies**: none beyond what's already in `package.json` (Tailwind 4, shadcn/ui, lucide-react for any new icons needed for nav/status badges).
