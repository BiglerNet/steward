## Why

The shared `MarkdownEditor` component (shipped in the archived `shared-markdown-editor` change) presents a WYSIWYG surface with no toolbar, menu, or any other visible affordance for formatting. The only way to produce a heading, bold text, or a list is to already know markdown shorthand syntax (`# `, `**bold**`, `- item`) and type it directly. This contradicts the component's own spec requirement — "a full WYSIWYG editing surface (no visible markdown syntax during normal editing)" — and the original design goal that non-technical household members "shouldn't have to understand markdown syntax at all." The feature is effectively undiscoverable for the audience it was built for.

## What Changes

- Add a fixed, always-visible formatting toolbar to `MarkdownEditor`'s WYSIWYG mode (bold, italic, headings, bulleted/numbered lists, link) so formatting is discoverable without knowing any markdown syntax.
- Add a mode toggle, similar to GitHub/GitLab's comment box, so `MarkdownEditor` supports two views over the same underlying markdown string:
  - **WYSIWYG mode** (default on load) — the existing Milkdown-based rich editing surface, now with the toolbar.
  - **Source mode** — a plain-text editor showing literal markdown syntax, for users who already know markdown and want direct text control.
- Switching modes mid-edit preserves content losslessly in both directions (WYSIWYG → source and source → WYSIWYG), since both modes read/write the same markdown string.
- **BREAKING**: none — this is a frontend-only enhancement to an existing shared component. No API, DTO, or database changes. No consuming form's props or usage changes.

## Capabilities

### Modified Capabilities
- `markdown-editor`: `MarkdownEditor` gains a visible formatting toolbar in WYSIWYG mode and a WYSIWYG/source mode toggle (defaulting to WYSIWYG), both operating on the same persisted markdown value. The `MarkdownContent` read-only renderer is unaffected.

## Impact

- **Frontend**: `src/Steward.Web/src/components/markdown/MarkdownEditor.tsx` gains a toolbar UI and mode-toggle state; likely a new Milkdown toolbar/plugin dependency (or hand-rolled toolbar driven by Milkdown commands) plus a plain `<textarea>`-based source-mode view. No changes required in any of the seven consuming forms (`MaintenanceItemEditorPage`, `MileageLogsPage`, `EngineHoursLogsPage`, `FuelLogsPage`, `RegistrationsSection`, `WarrantiesSection`, `AssetFieldsSection`) — they all consume `MarkdownEditor` via its existing `value`/`onChange`/`onBlur` props contract, which is unchanged.
- **Backend**: none.
- **No migration required.**
