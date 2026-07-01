## Why

`frontend-assets-and-tracking` built asset/engine CRUD and the four tracking-record logs, but deliberately deferred Registration and Warranty — both of which are renewal/coverage records with an attached proof document (`HasDocument`/`DocumentUrl`, upload/download/delete as separate endpoints from the record's own CRUD). This change closes that gap: UI for both record types plus a reusable document upload/preview/download pattern.

## What Changes

- Add a `frontend-tracking-records`-style list/create/edit/delete UI for **Registration** (renewal history: registration number, issuing authority, renewed-on, cost, expires-on, notes) scoped to an asset, ordered by `expiresOn` descending.
- Add the equivalent UI for **Warranty** (provider, description, starts-on, expires-on, notes) scoped to an asset.
- Add a reusable document attachment widget (upload button + file picker, current-document preview/filename, download link, replace, remove) used by both Registration and Warranty rows/detail views, calling each type's `POST/GET/DELETE .../document` endpoints.
- Enforce the backend's upload constraints client-side before submitting: allowed content types (`application/pdf`, `image/jpeg`, `image/png`) and max upload size (10 MB, read from config rather than hard-coded), with a clear inline error when a file is rejected.
- Apply the same Contributor/Owner-can-edit-and-delete role gating already established for tracking records (registration/warranty delete uses `HouseholdOperations.Edit`, not Owner-only).
- Add expiry-awareness in the UI: visually flag registrations/warranties whose `expiresOn` is in the past or within a configurable "coming due" window, as a precursor to the deferred reminder/notification feature (no notifications sent — purely a visual list/detail cue).

## Capabilities

### New Capabilities
- `frontend-registration-tracking`: List/create/edit/delete UI for registration renewal history, scoped to an asset, with document attachment.
- `frontend-warranty-tracking`: List/create/edit/delete UI for warranty coverage records, scoped to an asset, with document attachment.
- `frontend-document-attachments`: Reusable upload/preview/download/replace/remove widget shared by both record types.

### Modified Capabilities
- (none)

## Impact

- **Web**: New `src/api/registrations.ts`, `src/api/warranties.ts`, `src/api/documents.ts` typed clients (multipart upload helper); new `src/components/documents/DocumentAttachment.tsx` shared widget; two new tabs on the asset detail page (`/assets/:assetId/registrations`, `/assets/:assetId/warranties`) alongside the four tracking-log tabs from the previous change.
- **No backend changes** — consumes the already-built `registration-tracking`, `warranty-tracking`, and `document-storage` capabilities.
- **Out of scope for this change**: the deferred reminder/notification capability itself (only a visual "coming due" cue, no sends); the platform-admin UI; the public garage view.
