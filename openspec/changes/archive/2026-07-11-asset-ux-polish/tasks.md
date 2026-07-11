# Tasks: asset-ux-polish

## 1. Registry contract

- [x] 1.1 `AssetTypeDefinition`: replace `IconColor` with `Icon` (kebab-case lucide name); assign an icon per category in `AssetTypeRegistry`
- [x] 1.2 Update registry unit tests (every entry has a non-empty icon, no color field remains)
- [x] 1.3 Regenerate `schema.d.ts`; update `api/types.ts` and test fixtures (`test-fixtures/assetTypes.ts`) to mirror the new contract

## 2. Icon chip system

- [x] 2.1 Define per-group chip tint CSS variables (light + dark values) in `index.css`; replace `--light-asset-types` in `docs/design/tokens.md` with the group tokens
- [x] 2.2 `AssetTypeIcon` component: lucide icon map with neutral fallback, group-tinted chip, pinned icon foreground, size variants
- [x] 2.3 Component tests: every fixture icon name resolves to a real lucide icon; unknown name renders the fallback; chip uses group tint classes

## 3. Consume the chip

- [x] 3.1 `AssetListPage` cards: replace letter/inline-hex chip with `AssetTypeIcon`
- [x] 3.2 Sweep any other letter/`iconColor` usages (asset detail header, dialogs) to the shared component

## 4. Wizard type step

- [x] 4.1 Rework `TypeStep` to grouped compact rows (icon chip + label, radio semantics, two columns on desktop)
- [x] 4.2 Component tests: grouping preserved, selection works, all categories rendered as rows

## 5. Wizard VIN step

- [x] 5.1 Add explainer copy; remove Decode button; decode on Continue with pending state; malformed-VIN inline validation; Skip retained
- [x] 5.2 Carry decode outcome into Details: "Found: {year} {make} {model}" confirmation banner on success, couldn't-decode notice on failure/empty
- [x] 5.3 Component tests: decode fires on Continue, success banner content, 502 still advances with notice, malformed VIN blocked, skip path

## 6. Wizard engine step

- [x] 6.1 Add horsepower + torque inputs to `EngineStep` (match `EnginesSection` unit conventions); include in create-engine payload and decode prefill mapping
- [x] 6.2 Component tests: values submitted, decode prefill populates them

## 7. Verification

- [x] 7.1 `dotnet build` + `dotnet test`, `npm test`, `tsc -b`, lint, `vite build` all green
- [x] 7.2 Visual smoke in both themes: type picker fits desktop viewport, chips legible in dark mode, asset cards show icons, VIN flow shows found/failed feedback
