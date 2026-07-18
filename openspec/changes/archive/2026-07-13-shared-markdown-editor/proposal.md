## Why

Every long-form text field in Steward (`ServiceRecord.Description`, `MileageLog.Notes`, `EngineHoursLog.Notes`, `FuelLog.Notes`, `Registration.Notes`, `Warranty.Notes`, `Asset.Description`) is currently a plain `<textarea>` with no formatting. The upcoming maintenance-tracking work introduces a `MaintenanceItem.Description` field that needs real structure (steps, emphasis, links) and a WYSIWYG editing experience usable by both technical and non-technical household members. Building that editor once, now, and retrofitting it onto every existing free-text field gives immediate value on its own and avoids building it twice.

## What Changes

- New shared `MarkdownEditor` frontend component: full WYSIWYG editing backed by a markdown-native editor library (not an HTML/JSON-native editor with a markdown export bolted on), so the stored value is always valid markdown text with no lossy round-tripping.
- Retrofit `MarkdownEditor` onto every existing free-text field's create/edit form: `ServiceRecord.Description`, `MileageLog.Notes`, `EngineHoursLog.Notes`, `FuelLog.Notes`, `Registration.Notes`, `Warranty.Notes`, `Asset.Description`.
- Add a read-only markdown renderer for displaying these fields wherever they're shown outside an edit form (list rows, detail views), rendering safely without executing arbitrary HTML.
- Confirm and preserve the existing no-length-cap convention on all of these fields (no `HasMaxLength` in EF config, no length rule in FluentValidation) — markdown content is denser than plain text and must not be constrained by an accidental cap introduced later.
- **BREAKING**: none — storage stays a plain string column; this is a UI-layer change only, no API contract or schema change.

## Capabilities

### New Capabilities
- `markdown-editor`: the shared WYSIWYG editor component and the safe read-only markdown renderer used to display its content.

### Modified Capabilities
- `frontend-tracking-records`: create/edit forms for service records, mileage logs, engine hours logs, fuel logs, registrations, and warranties render their notes/description fields as the WYSIWYG markdown editor instead of a plain textarea, and list/detail views render the stored markdown instead of raw text.
- `frontend-asset-management`: the asset create/edit form's `Description` field uses the WYSIWYG markdown editor instead of a plain textarea, and asset detail views render the stored markdown.

## Impact

- **Frontend**: new dependency on a markdown-native WYSIWYG editor library (e.g. Milkdown or equivalent) — the first editor-framework dependency in this codebase, a real addition to the frontend's dependency footprint. New shared component under `src/components/ui/` or a dedicated `markdown/` folder. Every form/list component touching the seven affected fields is updated to use it.
- **Backend**: no schema or API changes. Existing `Description`/`Notes` columns and DTOs are unchanged.
- **No migration required.**
