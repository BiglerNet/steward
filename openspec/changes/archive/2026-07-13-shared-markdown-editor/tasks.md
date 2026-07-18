## 1. Editor component (Web)

- [x] 1.1 Add the markdown-native WYSIWYG editor dependency (Milkdown or equivalent) plus `react-markdown` for read-only rendering to `src/Steward.Web/package.json`.
- [x] 1.2 Build `MarkdownEditor` component (`src/Steward.Web/src/components/markdown/MarkdownEditor.tsx`) with a `value: string` / `onChange: (value: string) => void` props contract, wrapping the chosen editor library so no other component imports the library directly.
- [x] 1.3 Style `MarkdownEditor` to match the existing Radix-based design system (borders, focus ring, spacing consistent with other form inputs) in both light and dark theme.
- [x] 1.4 Build `MarkdownContent` read-only renderer component (`src/Steward.Web/src/components/markdown/MarkdownContent.tsx`) using `react-markdown` with no `rehype-raw`/raw-HTML passthrough.
- [x] 1.5 Unit test `MarkdownEditor`: typing produces expected markdown output; loading an existing markdown string renders its WYSIWYG form.
- [x] 1.6 Unit test `MarkdownContent`: renders headings/emphasis/lists as formatted elements; embedded `<script>`/HTML in the source string is not executed or rendered as live markup.

## 2. Retrofit tracking-record forms (Web)

- [x] 2.1 Swap the `description` textarea for `MarkdownEditor` in the service record create/edit form (via `TrackingLogSection`'s `renderFields`).
- [x] 2.2 Swap the `notes` textarea for `MarkdownEditor` in the mileage log create/edit form.
- [x] 2.3 Swap the `notes` textarea for `MarkdownEditor` in the engine hours log create/edit form.
- [x] 2.4 Swap the `notes` textarea for `MarkdownEditor` in the fuel log create/edit form.
- [x] 2.5 Swap the `notes` textarea for `MarkdownEditor` in the registration create/edit form.
- [x] 2.6 Swap the `notes` textarea for `MarkdownEditor` in the warranty create/edit form.
- [x] 2.7 Render each of the above fields via `MarkdownContent` wherever it's displayed read-only (list rows, detail views) instead of raw text interpolation. (Only `ServiceRecord.Description` and `Warranty.Description` were displayed read-only before this change; the other four `notes` fields remain edit-only, per explicit product direction during implementation.)
- [x] 2.8 Update existing component tests for each of the six forms/lists touched above to assert the new editor/renderer is used, fixing any snapshot/text-query breakage.

## 3. Retrofit asset description (Web)

- [x] 3.1 Swap the `description` textarea for `MarkdownEditor` in `AssetFieldsSection`/`AssetFormDialog`.
- [x] 3.2 Render the asset detail page's description via `MarkdownContent` instead of raw text.
- [x] 3.3 Update `AssetFormDialog` component tests for the new editor.

## 4. Verification

- [x] 4.1 Confirm no `HasMaxLength` exists on any of the seven backing EF Core properties (`ServiceRecord.Description`, `MileageLog.Notes`, `EngineHoursLog.Notes`, `FuelLog.Notes`, `Registration.Notes`, `Warranty.Notes`, `Asset.Description`) and none is added — verify against `src/Steward.Infrastructure/Persistence/Configurations/*Configuration.cs`.
- [x] 4.2 Manually exercise each of the seven forms in the running app: enter formatted markdown (heading, bold, list), save, reload, and confirm the WYSIWYG editor re-renders it correctly and the corresponding list/detail view displays formatted output.
- [x] 4.3 Run `npm run lint` and `npm test` in `src/Steward.Web`.
