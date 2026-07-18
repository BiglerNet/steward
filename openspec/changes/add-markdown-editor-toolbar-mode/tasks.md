## 1. Dependency swap (Web)

- [x] 1.1 Add `@milkdown/crepe` to `src/Steward.Web/package.json` alongside the existing `@milkdown/kit`/`@milkdown/react`.
- [x] 1.2 Spike: confirm which Crepe theme/CSS entry point is closest to headless (or produces the smallest override diff) to match the existing Radix/Tailwind design system, per the open risk in design.md. Record the chosen theme import in `MarkdownEditor.tsx`.

## 2. Rebuild WYSIWYG mode on Crepe with a fixed toolbar (Web)

- [x] 2.1 Replace the hand-wired `Editor.make()` + `commonmark`/`history`/`listener` setup in `MarkdownEditor.tsx` with a `Crepe` instance (`@milkdown/crepe`), configured with `defaultValue` seeded from the `value` prop.
- [x] 2.2 Enable `Crepe.Feature.TopBar` (not the floating `Toolbar` feature) so the toolbar is fixed and always visible; configure heading options and icon set to match the existing design system's iconography (`lucide-react`) as closely as Crepe's override hooks allow.
- [x] 2.3 Wire `crepe.on((listener) => listener.markdownUpdated(...))` to call the component's `onChange`, preserving the existing `lastEmitted` ref pattern that avoids feedback loops when the `value` prop is updated externally.
- [x] 2.4 Re-implement the existing `useEffect` that calls `replaceAll`-equivalent behavior when an external `value` change doesn't match the last emitted value (e.g. form reset), using Crepe's public API for setting content.
- [x] 2.5 Re-implement `handleBlur`'s synchronous flush (calling `crepe.getMarkdown()` directly instead of relying on the debounced listener) so a blur immediately followed by form submit cannot submit stale content — keep this behavior identical to today's.
- [x] 2.6 Forward `id`/`aria-labelledby`/`aria-describedby`/`aria-invalid`/`role="textbox"`/`aria-multiline` onto Crepe's root element the same way the current `editorViewOptionsCtx` config does.

## 3. Source mode and mode toggle (Web)

- [x] 3.1 Add local `mode` state (`"wysiwyg" | "source"`) to `MarkdownEditor`, defaulting to `"wysiwyg"` on every mount.
- [x] 3.2 Add a small toggle control (e.g. two tab buttons) rendered above the editing surface, switching `mode` and mounting/unmounting the Crepe instance vs. a plain `<textarea>` accordingly (only one is ever mounted at a time, per design.md's decision to avoid a hidden-but-live second instance).
- [x] 3.3 Bind the `<textarea>` source view to the same `value`/`onChange` props (raw markdown text, no transformation), including the same `onBlur` flush contract.
- [x] 3.4 Ensure toggling modes mid-edit does not drop unsaved keystrokes: the mode being left flushes its pending value into `value`/`onChange` before or as the other mode mounts.
- [x] 3.5 Style the toggle and the `<textarea>` source view to match the existing form input styling (border, focus ring, spacing) used elsewhere in the design system.

## 4. Tests (Web)

- [x] 4.1 Update `MarkdownEditor.test.tsx`'s existing three scenarios (load renders WYSIWYG, typing emits markdown, blur flushes synchronously) to pass against the Crepe-backed implementation without changing their assertions' intent.
- [x] 4.2 Add a test: clicking the toolbar's Bold control on a selection produces `**...**` in the emitted markdown (per the new "Toolbar produces formatted markdown without typed syntax" spec scenario).
- [x] 4.3 Add a test: selecting a heading level from the toolbar's heading control prefixes the current line with the correct `#` markdown.
- [x] 4.4 Add a test: `MarkdownEditor` renders in WYSIWYG mode by default on mount, both with an empty value and with an existing markdown string.
- [x] 4.5 Add a test: switching to source mode after producing formatted WYSIWYG content shows the literal markdown text in the `<textarea>`.
- [x] 4.6 Add a test: typing markdown syntax in source mode and switching back to WYSIWYG mode renders the formatted result.
- [x] 4.7 Add a test: an unsaved edit made in one mode is present in the other mode's view immediately after toggling (no blur/save in between).

## 5. Verification

- [ ] 5.1 Manually exercise `MarkdownEditor` in the running app across at least two of its seven consuming forms (e.g. maintenance item description, warranty description): use the toolbar to produce a heading/bold/list, switch to source mode and confirm the literal markdown matches, switch back, save, reload, and confirm the WYSIWYG view and the read-only `MarkdownContent` view both render the formatted result.
- [ ] 5.2 Confirm no visual regressions in light and dark theme for both the toolbar and the source-mode `<textarea>`.
- [ ] 5.3 Run `npm run lint` and `npm test` in `src/Steward.Web`.
