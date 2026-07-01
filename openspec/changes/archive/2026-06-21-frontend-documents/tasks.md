## 1. Shared Foundations

- [x] 1.1 Add `src/lib/documents.ts`: `ALLOWED_DOCUMENT_TYPES`, `MAX_DOCUMENT_SIZE_BYTES` constants, `validateDocumentFile(file)` helper.
- [x] 1.2 Add `src/lib/expiry.ts`: `getExpiryStatus(expiresOn)` → `"overdue" | "dueSoon" | "ok" | "none"`, with a configurable "coming due" threshold constant.
- [x] 1.3 Add an `ExpiryBadge` component rendering the appropriate visual state.

## 2. Document Attachment Widget

- [x] 2.1 Add `src/api/documents.ts`: generic multipart `upload(url, file)`, `download(url)`, `remove(url)` helpers.
- [x] 2.2 Build `src/components/documents/DocumentAttachment.tsx`: shows current state (none/has-document), upload control (with client-side validation from 1.1), download link, replace, remove — parameterized by the record's document endpoint paths.
- [x] 2.3 Gate upload/replace/remove controls to Contributor/Owner via `useHouseholdRole()`; Viewers see only the download action.

## 3. Registration API Client and UI

- [x] 3.1 Add `src/api/registrations.ts`: typed `list`/`create`/`update`/`delete` using generated `schema.d.ts` types.
- [x] 3.2 Build `RegistrationsSection`: list (expiresOn descending) with `ExpiryBadge`, create/edit dialog, delete confirm, `DocumentAttachment` per row.
- [x] 3.3 Wire to `/households/:householdId/assets/:assetId/registrations` route/tab.
- [x] 3.4 Gate create/edit/delete to Contributor/Owner via `useHouseholdRole()` (`canEdit`, consistent with tracking-record delete semantics).

## 4. Warranty API Client and UI

- [x] 4.1 Add `src/api/warranties.ts`: typed `list`/`create`/`update`/`delete`.
- [x] 4.2 Build `WarrantiesSection`: list with `ExpiryBadge`, create/edit dialog, delete confirm, `DocumentAttachment` per row.
- [x] 4.3 Wire to `/households/:householdId/assets/:assetId/warranties` route/tab.
- [x] 4.4 Gate create/edit/delete to Contributor/Owner via `useHouseholdRole()`.

## 5. Asset Detail Integration

- [x] 5.1 Add "Registrations" and "Warranties" tabs to the asset detail page's tab navigation, alongside the four tracking-log tabs.

## 6. Tests

- [x] 6.1 `validateDocumentFile`/`getExpiryStatus` unit tests: type/size rejection, overdue/due-soon/ok boundaries.
- [x] 6.2 `DocumentAttachment` tests: upload happy path, validation rejection (no API call made), replace, remove, Viewer sees download-only.
- [x] 6.3 `RegistrationsSection` tests: list ordering, create/edit/delete, expiry badge rendering, role-gated controls.
- [x] 6.4 `WarrantiesSection` tests: create/edit/delete, expiry badge rendering, role-gated controls.
- [x] 6.5 Backend-rejection test: upload endpoint returns `400` despite client validation passing → global toast shown, widget stays in pre-upload state.

## 7. Manual Verification

- [x] 7.1 Against the local Docker Compose stack: log a registration renewal, attach a PDF, download it, replace it with a JPEG, remove it.
- [x] 7.2 Attempt to upload an oversized file and an unsupported type; confirm both are rejected client-side with no network request, then bypass the client check (e.g. via devtools) to confirm the backend's own `400` still surfaces via the toast pattern.
- [x] 7.3 Set a registration/warranty `expiresOn` to a past date and to a near-future date; confirm the overdue/due-soon badges render as expected.
