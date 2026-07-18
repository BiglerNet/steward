## Context

Every free-text field in the app today (`ServiceRecord.Description`, `MileageLog.Notes`, `EngineHoursLog.Notes`, `FuelLog.Notes`, `Registration.Notes`, `Warranty.Notes`, `Asset.Description`) is a plain string edited via a bare `<textarea>` inside the existing `TrackingLogSection` dialog pattern (or `AssetFormDialog` for `Asset.Description`). None of these columns have a `HasMaxLength` in their EF Core configuration or a length rule in FluentValidation, so they already map to unbounded Postgres `text` columns — that constraint-free storage must be preserved, not accidentally tightened while touching these forms.

The frontend has no rich-text or markdown dependency today. Its component layer is Radix UI primitives (`@radix-ui/react-*`) hand-wired under `src/components/ui/`, plus `lucide-react` for icons — a deliberately minimal footprint (see the project's own note-to-self about `npx shadcn add` pulling an incompatible `@base-ui/react` variant that isn't in `package.json`). Introducing a WYSIWYG editor is the first time this frontend takes on an editor-framework-sized dependency, so the choice of library matters more than it would in a project already carrying one.

This work is a prerequisite for the upcoming `maintenance-items-and-templates` change, which needs `MaintenanceItem.Description` to be markdown-editable from day one — but it's scoped here to only the fields that exist today, so it ships and proves itself independently.

## Goals / Non-Goals

**Goals:**
- One shared `MarkdownEditor` component, used by every long-form field in the app, not built per-field.
- Full WYSIWYG editing (not a plain textarea, not a split-pane textarea+preview) — the household includes non-technical members who won't hand-write markdown syntax.
- The persisted value is always plain markdown text — no HTML, no proprietary JSON blob — so it round-trips losslessly and stays readable/portable outside the app.
- No new length constraints on any retrofitted field.

**Non-Goals:**
- No change to any API contract, DTO shape, or database column — this is a frontend editing/rendering change only.
- No image embedding, file attachment, or table support inside the editor — out of scope until a concrete need for it shows up.
- No collaborative/multi-cursor editing.

## Decisions

### Decision: Markdown-native editor library, not an HTML/JSON-native editor with a markdown adapter
Two families of WYSIWYG editor exist:
1. **HTML/JSON-native** (TipTap/ProseMirror, Lexical) — the editor's internal document model is HTML or a proprietary JSON tree; a plugin (`tiptap-markdown`, Lexical's markdown transformers) serializes to/from markdown on save/load.
2. **Markdown-native** (e.g. Milkdown, built on `remark` + ProseMirror) — the editor's actual source of truth *is* markdown text; what's rendered is a WYSIWYG view of that markdown, not a translation of some other format.

Because the storage format is a hard requirement (plain markdown), family 2 is the better fit: there's no serialization layer to drift out of sync on edge cases (nested lists inside blockquotes, tables, etc.). Family 1 is far more popular and has a bigger ecosystem, but that popularity doesn't offset the round-trip fidelity risk here. **Decision: use a markdown-native editor (Milkdown, or an equivalent library meeting the same criterion) for `MarkdownEditor`.**

Alternative considered: plain `<textarea>` + a small formatting toolbar (insert `**bold**` at cursor) + a "Preview" tab. Rejected per explicit product direction — the target user includes non-technical household members who shouldn't have to understand markdown syntax at all.

### Decision: Safe rendering for read-only display uses `react-markdown`, not a manually sanitized HTML pipeline
Wherever a field's value is displayed outside the editor (list rows, detail views), it needs to render as formatted markdown, not raw text. `react-markdown` parses markdown to React elements directly and does not render arbitrary embedded HTML unless explicitly configured to (via `rehype-raw`), which will not be enabled here — this makes it safe by default against injected markup without needing a separate sanitizer dependency (e.g. DOMPurify) bolted on.

### Decision: One component, applied everywhere in this change, not incrementally
Given the explicit direction that "anything that looks like a long-form description/note field" should use this editor, all seven existing fields are migrated in this change rather than introducing the component for one field and leaving the rest on plain textareas. This avoids two coexisting editing experiences for the same conceptual kind of field.

## Risks / Trade-offs

- **[Risk] New, heavier frontend dependency in a codebase that has deliberately stayed minimal.** → Mitigation: confine it to a single `MarkdownEditor` component with a narrow props surface (`value: string`, `onChange: (value: string) => void`, roughly) so the rest of the app depends on that component's interface, not on the editor library directly — if the library ever needs to be swapped, the blast radius is one file.
- **[Risk] Markdown-native editors are a smaller ecosystem than TipTap/Lexical — less community support, fewer plugins if requirements grow later (tables, images).** → Mitigation: accepted given the Non-Goals above explicitly exclude those features for now; revisit the library choice only if a real need for them emerges.
- **[Risk] Retrofitting seven fields at once touches every tracking-record form and the asset form in one change.** → Mitigation: the change is mechanical and low-risk per field (swap one input component for another, same `value`/`onChange` contract via `react-hook-form`), and existing component tests for each form catch regressions.

## Migration Plan

No data migration — columns and API contracts are unchanged. Rollout is purely a frontend deploy: ship `MarkdownEditor`, swap it into each of the seven forms, ship the read-only renderer into each of the corresponding list/detail views. Rollback is a plain revert of the frontend deploy; no database state to unwind.
