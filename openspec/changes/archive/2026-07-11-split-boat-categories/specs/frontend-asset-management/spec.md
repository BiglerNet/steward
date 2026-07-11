## MODIFIED Requirements

> Note: this delta is written against the spec text as it reads after `asset-ux-polish` is archived (icon-based registry consumption). Apply that change first.

### Requirement: Asset type registry consumption
The frontend SHALL fetch the asset type registry from `GET /api/asset-types` via a `useAssetTypeRegistry()` TanStack Query hook configured to fetch once per session (`staleTime: Infinity`), and SHALL use it as the single source of truth for category display labels, grouping, applicable type-specific fields, icon identity, and default usage tracking modes. Category icons SHALL be rendered through a shared `AssetTypeIcon` component that maps the registry's `icon` name to a lucide icon (with a neutral fallback for unknown names) on a theme-aware tinted chip whose colors are frontend-owned CSS variables per registry group — the registry SHALL NOT be a source of colors. Type-specific fields backed by a domain enum (`hullType`, `driveType`) SHALL render as selects with the enum's members as options; other fields keep their text/number inputs. Views that depend on the registry SHALL show a loading state until it resolves and a retryable error state if the fetch fails.

#### Scenario: Registry fetched once per session
- **WHEN** a user navigates between the asset list, detail, and form views repeatedly in one session
- **THEN** the asset-types endpoint is called at most once

#### Scenario: Registry drives field applicability
- **WHEN** the registry entry for `Sailboat` lists `hin`, `hullMaterial`, `hullType`, `keelType`, `mastHeightFt`, `mastCount`, `lengthFt`, `beamFt`, `make`, `model`, `color` as applicable fields
- **THEN** the asset form for a Sailboat renders exactly those type-specific inputs, without any frontend-hardcoded per-category field list

#### Scenario: Enum-backed fields render as selects
- **WHEN** the asset form renders `driveType` for a PowerBoat
- **THEN** it renders a select whose options are the `DriveType` enum members with readable labels (e.g. "Stern drive (I/O)"), not a free-text input

#### Scenario: Unknown icon name degrades gracefully
- **WHEN** the registry serves an `icon` name with no entry in the frontend's lucide icon map
- **THEN** the chip renders the neutral fallback icon rather than a letter, a blank, or an error

#### Scenario: Chip is readable in dark mode
- **WHEN** the dark theme is active and an asset-type chip renders (type picker, asset card)
- **THEN** the chip uses the dark values of the group tint variables and the icon remains legible against it
