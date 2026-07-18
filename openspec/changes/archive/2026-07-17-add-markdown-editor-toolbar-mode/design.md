## Context

`MarkdownEditor` (`src/Steward.Web/src/components/markdown/MarkdownEditor.tsx`) currently wraps raw `@milkdown/kit` primitives directly: `Editor.make()` configured with the `commonmark` preset, `history`, and `listener` plugins, rendered via `@milkdown/react`'s `<Milkdown />`. There is no toolbar, menu, or any other visible affordance — formatting only happens if the user types markdown shorthand (`# `, `**bold**`, `- item`) and the `commonmark` preset's input rules convert it live. That satisfies the letter of the existing `markdown-editor` spec's scenarios (typed shorthand → correct markdown value) but not its stated intent, since there is nothing to click and no non-technical user would discover it.

The component's public contract (`value: string`, `onChange: (value: string) => void`, `onBlur?: () => void`, plus a few ARIA props) is consumed identically by all seven forms across the app (maintenance items, mileage/engine-hours/fuel logs, registrations, warranties, asset description) via `react-hook-form`. This design must not change that contract — every consumer keeps working unmodified.

## Goals / Non-Goals

**Goals:**
- WYSIWYG mode has a fixed, always-visible toolbar (heading selector, bold, italic, lists, link) so formatting is discoverable with zero prior markdown knowledge — no hover-to-reveal, no slash command.
- A mode toggle lets the user switch to a plain-text "source" view showing literal markdown syntax, and back, without leaving the field.
- WYSIWYG is the default on every load; source mode is opt-in per editing session (mode choice does not need to persist across page loads/forms).
- Switching modes never loses or corrupts content — both modes are views over the same markdown string, not independent buffers.
- `MarkdownEditor`'s external props contract (`value`/`onChange`/`onBlur`) is unchanged.

**Non-Goals:**
- No per-user or per-household persisted preference for default mode — always starts in WYSIWYG. (Can be a later enhancement if requested.)
- No live side-by-side preview pane (source text on one side, rendered preview on the other) — that's a third mode this proposal doesn't introduce; source mode is edit-only raw text, matching GitHub/GitLab's toggle (not split) behavior.
- No new markdown features (tables, images, math) — same content scope as today, just a second way to edit it.

## Decisions

### Decision: Replace hand-wired `Editor.make()` with `@milkdown/crepe`'s `Crepe` class, using its built-in `TopBar` feature
Milkdown ships a higher-level batteries-included editor, `@milkdown/crepe`, built on the same `@milkdown/kit` core already in use. It exposes a `TopBar` feature (`Crepe.Feature.TopBar`) — a fixed, always-visible toolbar with heading selector, bold/italic/strikethrough/code, bulleted/ordered/task lists, link/image/table insert, and block commands — as a supported, documented, maintained feature, distinct from its `Toolbar` feature (a floating/bubble toolbar, which is explicitly not what we want here per the earlier bubble-menu rejection).

Alternative considered: hand-roll toolbar buttons that dispatch raw ProseMirror commands (`toggleMark`, `wrapInList`, etc.) against the existing bare `Editor.make()` setup. Rejected — this means re-implementing and maintaining editor-command wiring, active-state detection (e.g. "is the cursor inside a bold span" to highlight the Bold button), and icon/keyboard-shortcut handling ourselves, all of which `Crepe`'s `TopBar` already provides out of the box. Given the codebase's stated preference for a narrow, swappable dependency surface (one file owns the editor library), taking the library's own supported "batteries included" mode is a better fit than reinventing part of it by hand.

`Crepe` still reads/writes plain markdown as its source of truth (`defaultValue`, `getMarkdown()`, `markdownUpdated` listener — the same shape `MarkdownEditor` already depends on), so this is a swap of *which* Milkdown entry point is used, not a change to the markdown-native storage decision made in the original `shared-markdown-editor` design.

### Decision: Mode toggle lives inside `MarkdownEditor`, implemented as WYSIWYG (Crepe) vs. a plain `<textarea>`
Source mode does not need its own rich editor — it's literal text. A plain `<textarea>` bound to the same `value`/`onChange` is sufficient and keeps the added surface area small. `MarkdownEditor` renders a small mode toggle (e.g. two tab-like buttons "Write" / "Preview"-style, matching GitHub's wording, or "Rich text" / "Markdown" — copy TBD at implementation time) above the editing surface; only one of {Crepe instance, textarea} is mounted at a time, both driven by the same `value` prop and both calling the same `onChange`.

Alternative considered: keep both mounted simultaneously (one hidden via CSS) to avoid remount cost when toggling. Rejected — a mounted-but-hidden Crepe instance still holds an editor view and listeners; unmounting the inactive mode is simpler and the remount cost (re-parsing a markdown string into a ProseMirror doc) is not perceptible at the size of content these fields hold (no length cap, but these are household maintenance notes, not documents).

### Decision: Mode state is local `useState` inside `MarkdownEditor`, not lifted to consumers
Every consumer already only passes `value`/`onChange`/`onBlur`. Keeping "which mode is active" as internal UI state (defaulting to WYSIWYG on mount) means zero changes to any of the seven forms — the proposal's "frontend-only, single-file blast radius" property from the original design is preserved.

## Risks / Trade-offs

- **[Risk] `@milkdown/crepe` is a new dependency (in addition to the already-installed `@milkdown/kit` and `@milkdown/react`) and its default styling is opinionated (Crepe ships its own CSS theme).** → Mitigation: Crepe supports a "frame" theme / headless-style CSS override (or omit its default theme CSS and style the `TopBar` feature's DOM with Tailwind to match the existing Radix-based design system, same approach already used to style the current bare editor). Verify during implementation which of Crepe's theme entry points is closest to headless before committing to override-heavy CSS.
- **[Risk] Switching from `Editor.make()` to `Crepe` changes the low-level plugin composition (no more manually listing `commonmark`/`history`/`listener`), which could subtly change existing behavior (e.g. keyboard shortcuts, paste handling).** → Mitigation: the existing `MarkdownEditor.test.tsx` scenarios (load-renders-WYSIWYG, typing-emits-markdown, blur-flushes-synchronously) must all continue to pass unmodified — they assert on the public contract, not the internal plugin list — plus new tests for toolbar interaction and mode switching.
- **[Risk] Blur-flush behavior (`MarkdownEditor.tsx`'s `handleBlur`, added to avoid losing content to the listener plugin's debounce on rapid blur+submit) must be re-verified against Crepe's `on()`/`markdownUpdated` API**, which may debounce differently than the raw `listener` plugin did. → Mitigation: keep the existing synchronous-flush-on-blur test as a regression guard; if Crepe's API doesn't expose an equivalent synchronous `getMarkdown()` escape hatch, this is a blocking implementation detail to resolve before considering the change done (it is exposed — `crepe.getMarkdown()` is synchronous per Crepe's public API — but must be re-verified in code, not just in docs).
- **[Risk] Source-mode `<textarea>` has none of `MarkdownEditor`'s existing ARIA wiring (`role="textbox"`, `aria-multiline`, etc.) built in the same way.** → Mitigation: a plain `<textarea>` is natively a textbox with multiline support, so less custom ARIA plumbing is needed there than in the current div-based Crepe root, but the `id`/`aria-labelledby`/`aria-describedby`/`aria-invalid` props must be forwarded to whichever surface (Crepe root or textarea) is currently mounted.

## Migration Plan

No data migration — same markdown-string storage as today. Rollout is a frontend-only deploy: swap `MarkdownEditor`'s internals, ship the toolbar and mode toggle, keep the same props contract. Rollback is a plain revert of the frontend deploy. No consuming form needs a code change, so this can ship and be reverted independently of any of the seven forms' own release cadence.
