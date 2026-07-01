## 1. Design tokens & shared primitives (Web)

- [x] 1.1 Replace color/radius CSS variables in `src/index.css` with `docs/design/tokens.md` values (`--bg`, `--surface`, `--fg`, `--fg-soft`, `--border`, `--accent`, `--accent-hover`, `--danger`(+bg), `--warn`(+bg), `--success`(+bg)), remapped onto the existing semantic names (`--background`, `--card`, `--foreground`, `--muted-foreground`, `--border`, `--primary`, `--destructive`, etc.) consumed by `@theme inline`.
- [x] 1.2 Update `--radius` and any radius-consuming utilities to match the 12px/8px/6px/999px scale from `tokens.md`.
- [x] 1.3 Add type-scale utility classes (or Tailwind `@theme` font-size tokens) for H1/H2/H3/body/small/caption/stat-value per `tokens.md`, and apply them to existing page `<h1>`/`<h2>` headings across all pages touched in this change.
- [x] 1.4 Add a `Card` shadcn/ui primitive (`src/components/ui/card.tsx`) since none exists yet — needed for asset cards, detail cards, household cards, and auth-page cards.
- [x] 1.5 Add an `Avatar` shadcn/ui primitive (`src/components/ui/avatar.tsx`) for the nav user menu.
- [x] 1.6 Add the asset-type icon color lookup (`assetTypeIconColors`) to `src/lib/assetTypeFieldConfig.ts` per `tokens.md`'s `--light-asset-types`.
- [x] 1.7 Manually review every dialog/toast component (`dialog.tsx`, `sonner.tsx`, `select.tsx`, `dropdown-menu.tsx`) after the token swap to confirm nothing regressed visually (per design.md risk: global variable swap touches every consumer at once).

## 2. App shell & navigation (Web)

- [x] 2.1 Rebuild `src/components/layout/AuthenticatedLayout.tsx`: brand mark, primary nav links (assets list, household settings — routes that exist today), keeping `HouseholdSwitcher` and `PendingInvitesBanner` in place.
- [x] 2.2 Add active-link styling (accent underline) to the new nav links, matching `tokens.md`'s nav link spec.
- [x] 2.3 Update `UserMenu` to use the new `Avatar` primitive (initials, accent background) instead of a plain text button.
- [x] 2.4 Verify responsive collapse behavior below 768px (icon-only or stacked nav) matches `tokens.md`'s responsive note.

## 3. Asset list (Web)

- [x] 3.1 Restyle `AssetListPage.tsx` asset entries from bare `<li><Link>` blocks to `Card`-based asset cards with hover-lift (`translateY(-2px)` + shadow per `tokens.md`).
- [x] 3.2 Apply the asset-type colored icon swatch (from task 1.6) to each card.
- [x] 3.3 Restyle the "Add asset" affordance and empty state to match `assets.html`'s dashed-border add card / empty state treatment.
- [x] 3.4 Restyle the type filter (currently a `Select`) toward the chip-filter look from `assets.html` if feasible without changing its underlying behavior; otherwise leave the `Select` as-is and only restyle its container spacing. (No status badge — out of scope per proposal.md/design.md.)

## 4. Asset detail (Web)

- [x] 4.1 Restyle the `AssetDetailLayout.tsx` tab bar from the current segmented-control look to the underline-tab style (`.tabs`/`.tab`/`.tab.active`) from `tokens.md`, keeping the same 7 `NavLink` entries, `to` values, and `Outlet`.
- [x] 4.2 Apply `Card`/detail-card chrome (header, body padding, border) to the asset header/fields block at the top of `AssetDetailLayout.tsx`.
- [x] 4.3 Spot-check each tab's content page (`EnginesSection`, `RegistrationsSection`, `WarrantiesSection`, `TrackingLogSection` instances) for spacing/heading consistency with the new type scale; restyle table/list chrome to match `.record-table`/`.engine-item` from `tokens.md` where it doesn't require behavior changes.

## 5. Household pages (Web)

- [x] 5.1 Restyle `HouseholdSettingsPage.tsx` into card-based sections (rename-household card, members card) per `household-settings.html`, reusing `RenameHouseholdForm` and `MembersPanel` as-is internally.
- [x] 5.2 Restyle `HouseholdsIndexPage.tsx`'s "create your first household" empty state into a centered card per `households.html`.
- [x] 5.3 Restyle `HouseholdOverviewPage.tsx`'s minimal content into a card-based layout consistent with the new type scale (this page is currently a near-stub; keep scope to visual polish of what exists, no new sections).

## 6. Auth pages (Web)

- [x] 6.1 Restyle `LoginPage.tsx` into the centered-card layout from `login.html`, including the OAuth divider treatment; keep form fields, validation, and submit behavior unchanged.
- [x] 6.2 Apply the same restyle to `RegisterPage.tsx` for consistency.

## 7. Verification

- [x] 7.1 Manually walk every restyled page (login, register, asset list, asset detail + all 7 tabs, household settings, household overview/index, create-household dialog, asset create/edit dialog) in a browser to confirm visual consistency and no regressions, per the `/verify` skill.
- [x] 7.2 Run `npm run lint` and `npm run test` in `src/Steward.Web` to confirm no behavioral regressions from markup/className changes.
