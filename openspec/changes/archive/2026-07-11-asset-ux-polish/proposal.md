# Proposal: asset-ux-polish

## Why

The asset creation wizard and asset list shipped functional but rough: the type picker is a scrolling wall of 20 cards, the "icon" on type cards and asset cards is a placeholder first-letter on an API-served light-theme pastel — unreadable in dark mode — the VIN step neither explains its value nor confirms that a decode did anything, and the Engine step omits horsepower/torque even though the dashboard has `TotalHorsepower`/`TotalTorque` widgets that aggregate them.

## What Changes

- **BREAKING** (API contract): the asset-type registry's `iconColor` (hex string) is replaced by `icon` — a lucide icon name. Theming leaves the API entirely; the frontend owns chip colors per theme.
- Shared asset-type icon chip: real lucide icon on a theme-aware tinted chip (CSS-variable-backed light/dark values per registry group), used by the wizard Type step and the asset list cards. Fixes the dark-mode contrast bug.
- Wizard Type step becomes compact selectable list rows (icon + label, grouped, two columns on desktop) so all categories fit without scrolling.
- VIN step: explainer copy stating what the VIN unlocks (prefilled year/make/model/engine specs); the separate Decode button is removed — decode runs automatically on Continue when a valid VIN is entered; the result is confirmed visibly ("Found: 2015 Ford F-150") or a couldn't-decode notice shown. Still optional and never blocking.
- Engine step gains horsepower and torque inputs (fields already exist on `Engine` and in the engine endpoints — this is purely a form gap).

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `asset-type-registry`: registry entries carry `icon` (lucide icon name) instead of `iconColor`.
- `frontend-asset-management`: registry consumption + asset list requirements — icon chips replace letter/pastel treatment, theme-aware.
- `frontend-asset-creation-wizard`: Type step layout, VIN step behavior/feedback, Engine step performance fields.

## Impact

- **Backend**: `AssetTypeDefinition` + `AssetTypeRegistry` field swap (`iconColor` → `icon`); registry unit tests; no domain, endpoint, or migration changes.
- **Frontend**: regenerated `schema.d.ts`; new shared `AssetTypeIcon` chip component + lucide icon map with fallback; group tint CSS variables (light + dark) in `index.css` and `docs/design/tokens.md`; reworked `TypeStep`, `VinStep`, `EngineStep`; asset list card chip; test fixtures updated.
- **Note**: `split-boat-categories` (proposed alongside) assumes this change is applied and archived first — its registry entries and spec deltas are written against the post-`asset-ux-polish` contract.
