# Design: asset-ux-polish

## Context

The registry serves `iconColor` — light-theme pastel hexes from `docs/design/tokens.md` `--light-asset-types` — which the frontend applies via inline style in both themes while rendering `displayLabel[0]` as a stand-in icon. In dark mode the letter inherits the near-white theme foreground on a light pastel chip and is unreadable; this also sidesteps the design-system rule that components consume tokens, not literal hexes. `lucide-react` is already a dependency. Engine `HorsepowerHp`/`TorqueNm` and the dashboard widgets that sum them already exist; the wizard just never asks for them.

## Goals / Non-Goals

**Goals**
- Real icons with a chip treatment readable in both themes, shared everywhere a category is shown.
- Type picker that fits on one desktop screen.
- VIN step that sells itself and visibly confirms what decoding found.
- Wizard engine step captures the specs the dashboard aggregates.

**Non-Goals**
- No new categories or domain changes (that's `split-boat-categories`).
- No icon picker or per-household customization — icons are registry data.
- No redesign of the Details/Photos steps or the asset detail page.

## Decisions

### D1: Registry serves an `icon` name; the frontend owns all color

`AssetTypeDefinition.IconColor` is replaced by `Icon` (kebab-case lucide icon name, e.g. `"car"`, `"truck"`, `"snowflake"`, `"sailboat"`). Rationale: the hex-in-API approach baked light-theme presentation into a contract that can't know the viewer's theme; an icon *identity* is stable product metadata, a chip *color* is theming. The backend cannot validate lucide names, so the frontend keeps an explicit `Record<string, LucideIcon>` map with a neutral fallback icon (e.g. `box`) for unknown names, and a component test asserts every icon name in the registry fixtures resolves to a real icon — the fixtures are updated in this change to mirror the backend registry, which keeps drift visible in review.

### D2: Chip colors are per-group CSS variables with light and dark values

Five registry groups → five tint tokens (`--asset-chip-road`, `-powersport`, `-water`, `-trailer`, `-equipment`), each defined for light and dark themes in `index.css` alongside the existing design-system token overrides, with icon foreground pinned to a matching readable tone (not the theme foreground — that was the dark-mode bug). Per-group rather than per-category keeps the palette to five deliberate pairs instead of twenty, and the group is already the visual organizing unit in the picker. `docs/design/tokens.md` replaces `--light-asset-types` with the new group tokens. A shared `AssetTypeIcon` component (icon map + chip styling, size variants for list rows vs. cards) is the single consumer; `TypeStep` and `AssetListPage` drop their hand-rolled chips.

### D3: Type step is a grouped list of compact rows

Rows (icon chip + label, radio-group semantics, ~40px tall) under the five group headings, `sm:grid-cols-2`, replacing the 4-per-row tall cards. Twenty categories in two columns with five headings fits a typical desktop viewport without scrolling; mobile falls back to one column where scrolling is expected and fine. Selection/continue behavior is unchanged.

### D4: Decode runs on Continue, and the result is shown, not implied

The VIN step gets explainer copy ("Enter the VIN and we'll prefill year, make, model, and engine specs — you can change anything") and loses the Decode button. On Continue: if the field holds a well-formed 17-char VIN, the decode fires with an inline pending state, then the wizard advances regardless of outcome — success carries a confirmation banner into the Details step ("Found: 2015 Ford F-150" built from decoded year/make/model, shown alongside the prefilled fields), failure or empty-decode carries a "couldn't decode this VIN — enter details manually" notice. A malformed non-empty VIN blocks Continue with inline validation (same 17-char rule as the API), and an explicit Skip remains for users without the VIN handy. This keeps the never-blocking guarantee while making the decode's effect visible at the moment the prefilled fields appear.

### D5: Engine step adds horsepower and torque inputs

Two optional numeric inputs matching `EnginesSection`'s existing conventions and unit handling (`lib/units.ts`; storage stays `HorsepowerHp`/`TorqueNm`). No backend change — create-engine DTOs already accept both.

## Risks / Trade-offs

- **[Backend icon names can't be validated against lucide]** → frontend map-with-fallback plus fixture-driven resolution test; an unknown name degrades to a neutral icon, never a crash.
- **[Registry fixtures drifting from the backend registry]** → fixtures are the frontend's contract snapshot; the existing generate:api flow plus the resolution test surface drift at build/test time.
- **[Auto-decode on Continue adds latency to a navigation]** → inline pending state on the Continue button; vPIC's 8s server timeout bounds the wait, and failure still advances.

## Migration Plan

Backend and frontend ship together (contract field swap is breaking for the generated client). No data migration — the registry is static code. `split-boat-categories` must be applied after this change.

## Open Questions

None.
