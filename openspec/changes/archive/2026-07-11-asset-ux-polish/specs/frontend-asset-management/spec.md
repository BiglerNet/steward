## MODIFIED Requirements

### Requirement: Asset type registry consumption
The frontend SHALL fetch the asset type registry from `GET /api/asset-types` via a `useAssetTypeRegistry()` TanStack Query hook configured to fetch once per session (`staleTime: Infinity`), and SHALL use it as the single source of truth for category display labels, grouping, applicable type-specific fields, icon identity, and default usage tracking modes. Category icons SHALL be rendered through a shared `AssetTypeIcon` component that maps the registry's `icon` name to a lucide icon (with a neutral fallback for unknown names) on a theme-aware tinted chip whose colors are frontend-owned CSS variables per registry group — the registry SHALL NOT be a source of colors. Views that depend on the registry SHALL show a loading state until it resolves and a retryable error state if the fetch fails.

#### Scenario: Registry fetched once per session
- **WHEN** a user navigates between the asset list, detail, and form views repeatedly in one session
- **THEN** the asset-types endpoint is called at most once

#### Scenario: Registry drives field applicability
- **WHEN** the registry entry for `Boat` lists `hin`, `hullMaterial`, `lengthFt`, `beamFt`, `make`, `model`, `color` as applicable fields
- **THEN** the asset form for a Boat renders exactly those type-specific inputs, without any frontend-hardcoded per-category field list

#### Scenario: Unknown icon name degrades gracefully
- **WHEN** the registry serves an `icon` name with no entry in the frontend's lucide icon map
- **THEN** the chip renders the neutral fallback icon rather than a letter, a blank, or an error

#### Scenario: Chip is readable in dark mode
- **WHEN** the dark theme is active and an asset-type chip renders (type picker, asset card)
- **THEN** the chip uses the dark values of the group tint variables and the icon remains legible against it

---

### Requirement: Asset list
The frontend SHALL list a household's assets via `GET /api/households/{householdId}/assets`, optionally filtered by category, showing name, category display label (from the registry), year, and the cover photo thumbnail when the asset has one (`coverPhotoId` non-null), fetched with the authenticated client and rendered via object URLs. Assets without a cover photo SHALL fall back to the registry icon treatment. Each card SHALL show the category's icon via the shared `AssetTypeIcon` chip (lucide icon on the group-tinted, theme-aware chip) rather than a letter or hardcoded color.

#### Scenario: Viewing assets
- **WHEN** a household member opens `/households/:householdId/assets`
- **THEN** the app lists all assets in that household with registry-provided display labels

#### Scenario: Cover thumbnail on the card
- **WHEN** an asset has a `coverPhotoId`
- **THEN** its card shows that photo's thumbnail variant, while assets without one keep the icon fallback

#### Scenario: Category icon on the card
- **WHEN** an asset card renders in either theme
- **THEN** it shows the category's lucide icon on its group-tinted chip, legible in both light and dark modes

#### Scenario: Filtering by category
- **WHEN** a user selects a category filter
- **THEN** the list shows only assets of that category

#### Scenario: No assets yet
- **WHEN** a household has zero assets
- **THEN** the app shows an empty state prompting asset creation
