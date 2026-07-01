## Context

Registration and Warranty are structurally similar to the four tracking-record logs from `frontend-assets-and-tracking` (asset-scoped, list/create/edit/delete, Contributor-can-delete), but each row also optionally carries a document: `HasDocument`/`DocumentUrl` on the response, and separate `POST/GET/DELETE {recordId}/document` endpoints rather than multipart fields on the record's own create/update body. The backend enforces allowed content types (`application/pdf`, `image/jpeg`, `image/png`) and a max upload size (10 MB default, via `Storage:MaxUploadSizeBytes`) server-side; there's no endpoint exposing those limits to the client, so the frontend must mirror them as constants.

## Goals / Non-Goals

**Goals:**
- Registration and Warranty list/create/edit/delete UI, consistent with the established tracking-record patterns (Contributor-can-delete, asset-scoped, routed tabs).
- One reusable document-attachment widget (upload/preview/download/replace/remove) shared by both record types.
- Client-side mirroring of the backend's content-type/size constraints, to fail fast with a clear message instead of round-tripping a doomed upload.
- A purely visual "coming due / overdue" cue on `expiresOn`, since both record types already carry that field and it costs nothing extra here.

**Non-Goals:**
- Actually sending reminders/notifications — still deferred (per `registration-and-warranty`'s design.md); this change only adds a visual cue computed client-side from already-fetched data.
- Insurance policy tracking — separate future change, not started.
- Changing the backend's upload constraints or exposing them via a new config endpoint — the frontend mirrors the known current values as constants; if they drift, the backend's own validation remains authoritative and surfaces via the existing global error toast.

## Decisions

### 1. Document attachment is its own widget, only available once the record exists
Because `POST .../document` requires an existing `registrationId`/`warrantyId`, the document attachment control only appears on a saved record (in the list row's expanded state or a record detail panel) — never inside the create dialog. Creating a registration/warranty is a two-step flow by necessity: save the record, then (optionally) attach its document.
**Alternative considered**: a single combined create-with-file form that creates the record then immediately uploads — rejected as unnecessary complexity for marginal UX gain; given users may not have the document on hand at creation time anyway (e.g. logging a renewal before the paperwork arrives), making attachment a distinct, repeatable action (upload, replace, remove) is simpler and matches the backend's own endpoint separation.

### 2. Registration and Warranty get dedicated section components, not squeezed into the generic `TrackingLogSection`
`TrackingLogSection` (from `frontend-assets-and-tracking`) assumes a record has no attached sub-resource. Registration/Warranty rows need an extra document column/action that doesn't fit that config shape cleanly. Two new components (`RegistrationsSection`, `WarrantiesSection`) reuse the same list/create/edit/delete *patterns* and the shared `DocumentAttachment` widget, but aren't forced through the generic component.
**Alternative considered**: extend `TrackingLogSection`'s config to optionally support a document column — rejected, it would add a fifth conditional branch to a component already flagged in that change's design as a risk if forced to grow; two records needing it isn't enough to justify it, consistent with that design's stated mitigation ("if a future log type doesn't fit, build it standalone").

### 3. Client-side upload constraints are hardcoded constants mirroring `FileUploadOptions` defaults
`ALLOWED_DOCUMENT_TYPES = ["application/pdf", "image/jpeg", "image/png"]` and `MAX_DOCUMENT_SIZE_BYTES = 10 * 1024 * 1024` live in `src/lib/documents.ts`, used to reject an obviously-invalid file (wrong type, too large) before calling the upload endpoint.
**Alternative considered**: skip client-side checks entirely and rely on the backend's `400 BadRequest` — rejected, multipart uploads of a too-large file waste bandwidth and time for an outcome that's knowable instantly client-side; the backend check remains authoritative regardless (handles drift if the constants go stale).

### 4. Expiry cue is a pure function over `expiresOn`, computed at render time, no caching/polling
A small `getExpiryStatus(expiresOn: string | null): "overdue" | "dueSoon" | "ok" | "none"` helper (overdue = past; dueSoon = within 30 days; configurable threshold as a constant) drives a colored badge on each Registration/Warranty row. No background job, no notification — purely a render-time computation over data already in the TanStack Query cache.
**Alternative considered**: a dashboard/summary view aggregating all upcoming expirations across assets — rejected as out of scope; that's effectively the reminder feature's UI half and was explicitly deferred as a unit in `registration-and-warranty`'s design.

## Risks / Trade-offs

- **[Risk]** Hardcoded upload constants can drift from the backend's actual `Storage:MaxUploadSizeBytes`/`AllowedContentTypes` if an operator changes them in deployment config — **Mitigation**: accepted; the backend's own validation is authoritative and any mismatch just means an occasional avoidable round-trip, not a correctness bug.
- **[Risk]** Two near-identical section components (`RegistrationsSection`, `WarrantiesSection`) duplicate some list/dialog scaffolding that `TrackingLogSection` already solved — **Mitigation**: factor only the genuinely shared piece (`DocumentAttachment`) out; if a third document-bearing record type appears later, revisit whether a shared base component is warranted then, rather than guessing now.

## Migration Plan

No backend/database changes. Purely additive frontend work.

## Open Questions

None.
