## 1. Whole-card drag (Web)

- [x] 1.1 In `KanbanCard.tsx`, move `useDraggable`'s `setNodeRef`/`listeners`/`attributes` from the grip icon `<button>` to the card's root container; keep the `GripVertical` icon rendered as a non-interactive visual indicator.
- [x] 1.2 In `MaintenanceKanbanPage.tsx`, add `activationConstraint: { distance: 8 }` to the existing `PointerSensor`.
- [x] 1.3 Add a `TouchSensor` with `activationConstraint: { delay: 250, tolerance: 5 }` to the `useSensors(...)` call.
- [x] 1.4 Verify the title `<Link>` and the per-card dropdown menu still fire on a plain click/tap and do not start a drag, on both a mouse-driven and a touch-emulated browser session.
- [x] 1.5 Verify a touch press-and-hold past the delay, then move, starts a drag and drops correctly onto a column/the Done zone.

## 2. Kanban asset filter in the URL (Web)

- [x] 2.1 In `MaintenanceKanbanPage.tsx`, replace the `assetFilter` `useState` with `useSearchParams`, reading/writing an `asset` query param (absent/`"all"` meaning all assets).
- [x] 2.2 Confirm a reload with `?asset=<id>` in the URL restores the filtered view.

## 3. Navigation-origin state on links into the item editor (Web)

- [x] 3.1 In `KanbanCard.tsx`, pass `state: { from: <current board path + search>, fromLabel: "Maintenance" }` on the title `<Link>` to the item editor.
- [x] 3.2 In `MaintenanceItemsPage.tsx`, pass the same `state` shape (`from`: this asset's Maintenance tab path, `fromLabel`: the asset's name) on the row's `navigate(...)` call.

## 4. Breadcrumb and back navigation on the item editor (Web)

- [x] 4.1 Add a small breadcrumb component (or inline JSX) to `MaintenanceItemEditorPage.tsx` that reads `useLocation().state` for `{ from, fromLabel }`.
- [x] 4.2 When state is present, render `{fromLabel} › {item.title}` with the leading segment linking to `from`.
- [x] 4.3 When state is absent, render `{asset.name} › Maintenance › {item.title}` using the already-fetched asset data, with the relevant segments linking to `/households/:householdId/assets/:assetId/maintenance`.
- [x] 4.4 Place the breadcrumb above the existing title/delete header row; confirm it renders correctly for a Viewer (read-only session) as well as Contributor/Owner.

## 5. Surface `completedAt` in the UI (Web)

- [x] 5.1 Add a small relative-time helper (e.g. `src/Steward.Web/src/lib/relativeTime.ts`) using `Intl.RelativeTimeFormat("en", { numeric: "auto" })`, bucketing to minutes/hours/days as appropriate — no new dependency.
- [x] 5.2 In `KanbanDoneDropZone.tsx` (and/or `KanbanCard.tsx` when rendered in the Done zone), show the relative label (e.g. "Completed 2 days ago") using `item.completedAt`.
- [x] 5.3 In `MaintenanceItemEditorPage.tsx`, render a read-only "Completed" field with the absolute date when `item.completedAt` is set; omit it entirely when unset.
- [x] 5.4 In `MaintenanceItemsPage.tsx`, add a "Completed" column to the table, showing the absolute date or "—" when unset.

## 6. Tests (Web)

- [x] 6.1 Update/extend `KanbanCard`/`MaintenanceKanbanPage` tests to cover: whole-card drag start, click-through on title and menu, touch press-and-hold activation, URL-persisted asset filter.
- [x] 6.2 Add/extend `MaintenanceItemEditorPage` tests covering the three breadcrumb scenarios (from kanban, from asset tab, direct/no state) and the completed-date field's presence/absence.
- [x] 6.3 Add/extend `MaintenanceItemsPage` tests covering the new Completed column, including the "—" placeholder case.
- [x] 6.4 Run `npm run lint` and `npm test` in `src/Steward.Web`.
