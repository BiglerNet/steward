## Context

The kanban board and full-page maintenance item editor shipped together in `maintenance-recurrence-and-kanban` (archived 2026-07-17). Both are React 19 + TanStack Query + React Router pages using `@dnd-kit/core` for drag interactions. The item editor is a top-level route (`/households/:householdId/assets/:assetId/maintenance/:itemId`), reachable from two places: the household-wide kanban board (`/households/:householdId/maintenance`) and the asset-scoped Maintenance tab (`/households/:householdId/assets/:assetId/maintenance`, nested under `AssetDetailLayout`). It is not nested under either, so it has no inherited breadcrumb/tab chrome from its parent.

`MaintenanceItem.CompletedAt` already exists end-to-end (domain entity, set/cleared in `MaintenanceItemService.PatchAsync` on transition into/out of `Done`, exposed in `MaintenanceItemResponse`) but no frontend surface reads it except the kanban board's internal 7-day "recently completed" filter (`src/Steward.Web/src/lib/kanban.ts`).

## Goals / Non-Goals

**Goals:**
- Make the whole kanban card a drag surface on both desktop (pointer) and touch, without breaking the title link or per-card menu.
- Give the item editor page reliable orientation and a working way back, for both of its entry points and for direct/refreshed loads.
- Surface `completedAt` in the three places a user would look for it, using only what's already returned by the API.

**Non-Goals:**
- No backend, DTO, or migration changes — `completedAt` is already in the response shape.
- No change to the drag-and-drop status-change logic itself (`planDrop`, the Done-transition confirmation) — only which surface starts the drag and how fine-grained the movement threshold is.
- No generic/reusable "back button" framework for the whole app — scoped to the maintenance item editor, though the pattern (nav-origin `state` + fallback) is one other detail pages could adopt later if useful.

## Decisions

### Decision: distance/delay activation constraints, not separate "drag mode"
Rather than a mode toggle or a "hold to enable dragging" affordance, use dnd-kit's built-in activation constraints so the same pointer-down starts as an ambiguous gesture and resolves to either a click or a drag:
- `PointerSensor` with `activationConstraint: { distance: 8 }` for mouse/pointer — movement past 8px before release commits to a drag; anything less is treated as a click, so the title `<Link>` and the card's dropdown menu keep working unmodified.
- `TouchSensor` with `activationConstraint: { delay: 250, tolerance: 5 }` for touch — a 250ms hold with less than 5px movement arms the drag; a quick tap still taps, and a swipe/scroll gesture is not hijacked.

Alternative considered: keep the dedicated grip icon as the only surface but enlarge it. Rejected because it doesn't address the reported problem (small target) as directly, and every mainstream kanban tool (Trello, Linear, GitHub Projects) uses whole-card drag with a movement threshold, so this matches user expectation rather than inventing a new pattern.

The grip icon stays in the card as a visual "draggable" indicator, but `useDraggable`'s `listeners`/`attributes` move from the icon `<button>` to the card's root container.

### Decision: card title is a `<button>` + `navigate()`, not a `<Link>`
dnd-kit already guards against a click firing on a card's children right after a drag activates: once the activation constraint is met, it registers a capture-phase `click` listener on `document` that calls `stopPropagation()`, so the event never reaches the card's children. That's sufficient to stop a JS-driven click handler (e.g. a `<button onClick>`, or the existing dropdown menu) from firing — but it is *not* sufficient for a real `<a>` element: the browser's native "navigate to `href`" activation behavior only checks the event's canceled flag (set by `preventDefault()`), not whether propagation was stopped. Since the anchor's own click handler (React Router's `Link`, which calls `preventDefault()` before doing its SPA navigation) never runs either — its invocation is skipped for the same propagation-stopped reason — nothing ever calls `preventDefault()`, and the browser falls through to a full native navigation to the `href`. This reproduced exactly as reported: drag a card by its title to another column, and after the status-changing drop, the app also navigates to that item's editor.

Fix: render the title as a `<button type="button" onClick={() => navigate(...)}>` instead of a `<Link>`. A button has no href-driven default action to fall through to, so dnd-kit's existing click-guard fully covers it — matching the card's dropdown-menu button, which was never affected by this bug for the same reason. No new drag-tracking state was needed; this reuses dnd-kit's built-in protection rather than adding a parallel one.

Trade-off accepted: the title is no longer a real anchor, so browser-native "open in new tab" (ctrl/cmd-click, middle-click) on a card title no longer works. Given the card's dropdown/grip elements were already plain buttons with the same limitation, this is consistent with the rest of the card rather than a new regression.

### Decision: breadcrumb shape follows navigation origin, with a data-derived fallback
The item editor reads `useLocation().state` for an origin the caller attached when navigating in:
- Kanban card link/`MaintenanceKanbanPage` → passes `state: { from: "/households/:id/maintenance?...", fromLabel: "Maintenance" }`
- Asset Maintenance tab row (`MaintenanceItemsPage`) → passes `state: { from: "/households/:id/assets/:assetId/maintenance", fromLabel: assetName }` (asset name already loaded on that page)

Breadcrumb rendering:
- `state` present → `{fromLabel} › {item.title}`
- `state` absent (direct URL, refresh, opened in new tab) → `{asset.name} › Maintenance › {item.title}`, built from data already fetched for the page (asset name via the existing asset query)

The back affordance (a leading chevron/segment) navigates to `state.from` when present, else to the canonical asset Maintenance tab path (`/households/:householdId/assets/:assetId/maintenance`). This means "back" is always correct-in-spirit even without history, and precisely returns the caller's view (including its query params, see below) when the origin is known.

Alternative considered: `navigate(-1)` (browser history back). Rejected — breaks on direct link/refresh (no entry to go back to) and is invisible as UI (nothing on the page hints it exists), which is the exact complaint driving this change.

### Decision: kanban asset filter moves into a URL search param
`MaintenanceKanbanPage`'s `assetFilter` becomes a `useSearchParams`-backed value (e.g. `?asset=<id>`) instead of local `useState`. This is what makes the "back to where I came from" promise actually hold: the `state.from` path captured when linking to an item includes the current search string, so returning to it restores the filter instead of resetting to "All assets." No other kanban behavior changes.

### Decision: relative time via native `Intl.RelativeTimeFormat`, no new dependency
No date library exists in `package.json` today. A small helper (e.g. `formatRelativeToNow(date: Date): string`) buckets the delta into the coarsest sensible unit (minutes/hours/days) and calls `Intl.RelativeTimeFormat("en", { numeric: "auto" })`, matching the project's existing preference for native platform APIs over adding dependencies (mirrors `Intl`-based formatting patterns already used for dates elsewhere in the app). Used only on kanban cards in the Done zone; the editor page and the asset table show the absolute date (existing `date` rendering conventions in those files — plain locale date string) since those are places a user wants precision, not a skim-friendly relative label.

## Risks / Trade-offs

- **[Risk]** Attaching drag listeners to the whole card could make the title `<Link>` or dropdown menu harder to hit precisely, or cause a click that lands exactly on the boundary to be swallowed by the 8px-distance drag check → **Mitigation**: this is dnd-kit's documented, widely-used pattern for exactly this problem; the existing keyboard sensor and disabled-when-`!canEdit` behavior are unaffected since the constraint only changes when a drag is armed, not whether one is possible.
- **[Risk]** Passing `state` on navigation is invisible in the URL, so a bookmarked or copy-pasted link to a maintenance item will always fall back to the data-derived breadcrumb, even if the user habitually arrives via the kanban board → **Mitigation**: acceptable — the fallback is still correct and oriented, just less precise about origin; this only affects the *label*, not correctness of the back target.
- **[Risk]** Moving the asset filter into the URL is a small scope creep beyond "fix back navigation" → **Mitigation**: it's the only way the "return to where you were" requirement is actually true rather than superficially true (same route, different visible state); flagged explicitly in the proposal rather than snuck in.

## Migration Plan

Frontend-only, no data migration. Ship as one PR; no feature flag needed since none of these changes are behavioral regressions (whole-card drag is strictly more permissive than icon-only; breadcrumb/back is new UI with no prior equivalent; completed-at display is additive; URL-based filter defaults to "All assets" same as before when the param is absent).

## Open Questions

None outstanding — behavior for all four items was confirmed directly with the user in the preceding exploration.
