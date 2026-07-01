## Context

`Registration` and `Warranty` were modeled and EF-configured in `core-solution-structure` with `DeleteBehavior.Restrict` FKs to `Asset`, matching the precedent set by the tracking entities. Both currently expose `DocumentUrl` as a plain string, originally intended as a placeholder until file storage was decided.

`Registration` as originally modeled (`RegistrationNumber`, `IssuingAuthority`, `ExpiresOn`, `DocumentUrl`, `Notes`) has no `Cost` and no concept of "when was this renewed" — it reads as a single current-state row. The user wants to track registration *history*: each renewal cycle's cost and dates, not just the latest expiry. This reframes `Registration` the same way `ServiceRecord`/`MileageLog`/`FuelLog` are already framed in the `tracking` change — an append-only log scoped to an asset — rather than a single mutable record. `Warranty` keeps its original single-record-per-coverage shape; a warranty doesn't "renew" the same way a registration does, so no equivalent change is needed there.

The deployment target is self-hosted Kubernetes with an NFS-mounted share, with a possible future move to an S3-compatible provider (Cloudflare R2) if usage grows. The storage layer needs to be abstracted now so that move doesn't require touching `RegistrationsController`/`WarrantiesController` or the application-layer services later.

## Goals / Non-Goals

**Goals:**
- CRUD for Registration and Warranty, following the same controller/service/authorization pattern as the tracking change.
- A single-file upload/download/delete flow per record, decoupled from the CRUD body so updating `registrationNumber` doesn't require re-uploading the document.
- A storage abstraction (`IFileStorageService`) with a local-filesystem implementation today, designed so an S3-compatible implementation is a pure addition later.
- Basic upload safety: content-type allowlist (PDF, JPEG, PNG) and a size cap, enforced at the API boundary.

- Registration history: each renewal is its own row with `cost`, `renewedOn`, and `expiresOn`, so a household can see what they paid and when across multiple renewal cycles, not just the current one.

**Non-Goals:**
- S3/R2 implementation itself — only the abstraction is built now; the concrete `LocalFileStorageService` is the only implementation shipped in this change.
- Virus/malware scanning of uploads — out of scope; revisit if the app is exposed beyond trusted household members.
- Multiple documents per Registration/Warranty (e.g., front+back of a card) — one document per record for now; revisit if a real need for multiple attachments emerges.
- Thumbnail/preview generation — the frontend just offers a download link.
- Renewal/expiry reminder notifications — explicitly deferred to a future change, but `ExpiresOn` is indexed on both entities now so that change can query "what's expiring within N days" cheaply without a schema change later.
- Insurance policy tracking — shares an almost identical shape to the renewal-history model below (provider/cost/renewal-date/document), but is deliberately scoped to its own future change rather than bundled in here.

## Decisions

### 1. `IFileStorageService` abstraction with a `LocalFileStorageService` implementation
`IFileStorageService` exposes `SaveAsync(Stream, string contentType) → storageKey`, `OpenReadAsync(storageKey) → Stream`, `DeleteAsync(storageKey)`. `LocalFileStorageService` resolves `storageKey` to a path under a configured `Storage:RootPath`, generating the key as `{entityType}/{entityId}/{guid}{extension}` to avoid filename collisions and directory traversal.
**Alternative considered**: write directly to `IFormFile`/`Path.Combine` calls inline in the controller — rejected, that's exactly the coupling that would make a future S3 migration touch every controller; an interface in `Application` with the implementation in `Infrastructure` keeps the swap localized to one new class plus a DI registration change.

### 2. Document upload/download are separate endpoints from the CRUD body, not a `multipart/form-data` field on create/update
`POST .../registrations/{id}/document` (multipart upload, replaces any existing document), `GET .../registrations/{id}/document` (streams it back with the stored content-type), `DELETE .../registrations/{id}/document` (removes the attachment, keeps the parent record).
**Alternative considered**: accept the file directly on `POST .../registrations` as multipart — rejected, it would force every create/update request through multipart encoding even when a client just wants to edit `expiresOn`, and conflates two independently-failable operations (saving a record vs. saving a file) into one request.

### 3. `DocumentUrl` becomes a server-generated opaque storage key, not a client-supplied URL
Clients never set `documentUrl` directly; `RegistrationResponse`/`WarrantyResponse` expose a `hasDocument: bool` plus a `documentUrl` that points at the API's own download endpoint (e.g. `/api/households/{householdId}/assets/{assetId}/registrations/{id}/document`), not the underlying storage path.
**Rationale**: keeps the storage key (and therefore the storage backend) entirely an implementation detail; switching to S3/R2 later means the download endpoint starts redirecting/proxying to a signed URL instead of streaming from disk, with no client-visible change.

### 4. Upload validation: content-type allowlist + size cap enforced in the controller before calling `IFileStorageService`
Allowed: `application/pdf`, `image/jpeg`, `image/png`. Max size: 10 MB (configurable via `Storage:MaxUploadSizeBytes`).
**Alternative considered**: validate inside `LocalFileStorageService` — rejected, validation is a business rule independent of storage backend and belongs in the same layer as the rest of FluentValidation request validation, not duplicated into every future storage implementation.

### 5. Registration/Warranty delete is Contributor+Owner, matching the tracking-record precedent
Same rationale as `tracking`'s Decision 3 — correcting or removing a registration/warranty entry is routine maintenance, not a structural household decision like deleting an asset.

### 6. `Registration` gains `Cost` and `RenewedOn`, becoming a renewal-history log rather than a single current-state record
Each `POST .../registrations` call represents one renewal cycle: `registrationNumber`, `issuingAuthority`, `renewedOn` (when this renewal was filed/paid), `cost`, `expiresOn` (when this renewal is due to lapse), `notes`, optional document. The list endpoint orders by `expiresOn` descending, so the first item in the list is always the current/most relevant renewal — no separate "get current registration" endpoint is needed.
**Alternative considered**: keep `Registration` as a single mutable row per asset and just add `Cost`/`LastRenewedOn` fields, overwriting them on each renewal — rejected, this is exactly what the user asked to avoid: it destroys history (you can no longer see what the *previous* renewal cost or when it happened), and the household-multitenancy/tracking precedent already established that historical entries should accumulate as rows, not get overwritten in place.
**Note**: `Warranty` is not changed by this decision — a warranty's start/expiry describe one continuous coverage period, not a sequence of discrete renewal transactions, so its original single-record-per-coverage shape stands.

## Risks / Trade-offs

- **[Risk]** Local filesystem storage ties uploaded files to a single mounted volume; if the NFS mount is misconfigured or unavailable, uploads/downloads fail with no automatic fallback → **Mitigation**: accepted for now per the chosen deployment target; `IFileStorageService`'s abstraction means swapping to R2 later requires no controller/service changes if this becomes a real problem.
- **[Risk]** Orphaned files on disk if a database transaction fails after `SaveAsync` succeeds (or vice versa for delete) → **Mitigation**: save the file first, then commit the DB row referencing its key — a failed DB commit leaves an orphaned file (cheap, cleanable later) rather than a DB row pointing at a missing file (worse, breaks downloads); document this ordering as a service-implementation requirement in tasks.md.
- **[Risk]** No malware scanning means a malicious file could be uploaded and later downloaded by another household member → **Mitigation**: explicitly out of scope per Non-Goals; content-type/size validation narrows but doesn't eliminate this; flagged for revisit if the app is ever exposed to less-trusted users.

## Migration Plan

A new EF Core migration (`AddRegistrationCostAndRenewedOn`) is required to add the `Cost` (`numeric`, nullable) and `RenewedOn` (`date`, nullable) columns to the existing `Registrations` table, plus an index on `ExpiresOn` to support future reminder queries (Non-Goals). `Warranty` needs no migration — it's unchanged. `DocumentUrl` on both already exists as a nullable string column and is reused as the storage key — only its semantic meaning changes (server-generated key vs. free-text URL), which is an application-layer concern, not a further schema change.

Deployment-side: the Kubernetes manifests need a `Storage:RootPath` volume mount (NFS-backed PersistentVolumeClaim) — tracked as a task, not part of this repo's `docker-compose.yml` change set beyond a local bind-mount equivalent for dev.

## Open Questions

None outstanding — file storage strategy is now resolved per the user's direction (local filesystem now, S3-compatible later if needed).
