# Design: asset-photos

## Context

Assets have a vestigial `PhotoUrl` string that no flow populates. File handling already exists for registration/warranty documents: `IFileStorageService` (`SaveAsync`/`OpenReadAsync`/`DeleteAsync` over a local root path) with `FileUploadOptions` (allowed content types, 10 MB cap), consumed by `RegistrationService`/`WarrantyService` and streamed through their controllers. There is no image processing and no storage accounting anywhere.

Settled in the 2026-07-09 exploration: cover-pointer method, SkiaSharp processing, thumbnail + larger variant, size/dimension limits, household-wide storage quotas. Migrations reset freely (pre-launch). This change must land before the creation wizard so the wizard's photo step has something to call.

## Goals / Non-Goals

**Goals**
- Real photo uploads with server-side normalization (orientation, metadata stripping, bounded dimensions) and exactly two stored variants per photo.
- Cover selection driving asset cards and detail header.
- A storage ceiling per household covering all stored files (photos + documents), enforced at upload time.

**Non-Goals**
- No captions, ordering, or albums; no photos on non-asset entities.
- No CDN, no public/unauthenticated image URLs, no background processing queue (processing is synchronous in the request).
- No retroactive usage recalculation job (counter starts at zero pre-launch and stays transactionally consistent).

## Decisions

### D1: `AssetPhoto` entity with two storage keys; originals discarded
`AssetPhoto`: `Id`, `AssetId` (FK), `ThumbStorageKey`, `DisplayStorageKey`, `Width`/`Height` (display variant, post-orientation), `SizeBytes` (sum of both stored variants — the number the quota uses), `CreatedAt`. Both variants are JPEG (quality ~80); the uploaded original is never persisted. Re-encoding to JPEG loses PNG transparency, which is irrelevant for photographs of vehicles and guarantees uniform output, predictable sizes, and metadata stripping for free.

*Alternative considered*: keep the original alongside variants — rejected; triples storage for no user-visible feature, and "discard original" was the settled exploration decision.

### D2: Processing pipeline (SkiaSharp, synchronous)
1. Enforce the upload cap (~15 MB, config `Storage:MaxPhotoUploadSizeBytes`) before reading the body into memory.
2. Sniff magic bytes (JPEG/PNG/WebP accepted) — the client's `Content-Type` header is ignored for trust purposes.
3. Bounds-check via `SKCodec` **before** full decode; reject images over 12,000px on either side (decompression-bomb guard).
4. Decode, apply EXIF orientation, then resize: display variant capped at 2048px on the long edge (no upscaling), thumbnail at 320px.
5. Encode both as JPEG; metadata (EXIF/GPS) is dropped by re-encoding.
6. Save both via `IFileStorageService` under `asset-photos/{assetId}/…`; on any failure after the first save, delete what was written (no orphaned files).

Synchronous in-request processing is fine at this scale (self-hosted, ≤15 MB inputs); a queue would be premature. Image processing lives behind an Application-layer interface (`IImageProcessor` in `Steward.Application`, SkiaSharp implementation in Infrastructure) so Domain/Application stay framework-free. Container images need `SkiaSharp.NativeAssets.Linux.NoDependencies` for the Alpine/Debian runtime.

### D3: Cover is a pointer on `Asset`, auto-managed at the edges
`Asset.CoverPhotoId` (nullable FK → `AssetPhoto`, `ON DELETE SET NULL` semantics handled in the service so reassignment can happen in the same transaction). Rules: first upload to a coverless asset sets the cover; `PUT /api/households/{hid}/assets/{aid}/cover-photo` `{ photoId }` (Contributor/Owner) changes it; deleting the cover photo reassigns to the newest remaining photo, or null when none remain. A pointer beats an `IsCover` flag because uniqueness is structural (no "two covers" state to defend against).

### D4: Endpoints mirror the document pattern
- `POST /api/households/{hid}/assets/{aid}/photos` — multipart, Contributor/Owner, 201 with `AssetPhotoResponse`.
- `GET .../photos` — list, any Active member.
- `GET .../photos/{photoId}/content?variant=thumb|display` — streams the variant with `image/jpeg`, any Active member; 404 unknown variant value → 400.
- `PUT .../cover-photo` — body `{ photoId }`, Contributor/Owner; 400 if the photo belongs to another asset.
- `DELETE .../photos/{photoId}` — Contributor/Owner, 204; deletes both variants, decrements usage, reassigns cover per D3.

Authorization reuses `IHouseholdResource` + `HouseholdAuthorizationHandler` exactly like registrations/warranties. `AssetResponse` gains `coverPhotoId`; the frontend composes the content URL from ids and fetches with the bearer token (existing document-download pattern → object URLs), so no signed-URL machinery is needed.

### D5: Quota as a counter on `Household`, maintained by a shared accounting service
`Household.StorageUsedBytes` (long, default 0) and `StorageQuotaOverrideBytes` (long?, null = use config default `Storage:HouseholdQuotaBytes`, default 1 GB). A small `IStorageQuotaService` in Application wraps the two operations every file-writing service must use: `EnsureCapacityAsync(householdId, incomingBytes)` (throws quota-exceeded → 400 with a clear message) and `AdjustUsageAsync(householdId, deltaBytes)` called inside the same `SaveChangesAsync` transaction as the entity write. Photo service counts stored-variant bytes (post-processing, not the upload size); registration/warranty document upload/replace/delete are retrofitted onto the same service — replace adjusts by the difference, delete decrements.

*Alternative considered*: computing usage on demand by summing file sizes — rejected; requires filesystem scans or per-file DB rows for documents (which live as bare storage keys on their records), and a transactional counter is exact given every write path goes through the service.

Concurrency note: `EnsureCapacityAsync` check + adjust is check-then-act; two simultaneous uploads could slightly overshoot the quota. Accepted — the quota is a ceiling against runaway usage, not a billing boundary.

### D6: PlatformAdmin quota override endpoint
`PUT /api/admin/households/{householdId}/storage-quota` body `{ quotaBytes: long | null }` (null clears the override), PlatformAdmin role only. Routed under `/api/admin/...` to match the existing `PlatformAdminController` prefix (`/api/admin/users`) rather than introducing a second admin URL convention. Household detail responses (`GET /api/households/{id}`) expose `storageUsedBytes` and `storageQuotaBytes` (effective value) to members so the settings page can show a usage bar; the override itself is not distinguished in the member-facing payload.

### D7: `PhotoUrl` removed everywhere in this change
Entity property, `IAssetTypeFields`-adjacent DTO fields, mapper lines, frontend types/forms/fixtures — all dropped now rather than deprecating, since nothing ever wrote it. Asset cards and detail header switch to `coverPhotoId`-driven thumbnails with the existing icon fallback. Frontend photo UI is hand-rolled from existing primitives (file input + grid + existing Button/Dialog) per the project's shadcn/Base-UI constraint — no new generated components.

## Risks / Trade-offs

- [SkiaSharp native binaries missing in container] → add `SkiaSharp.NativeAssets.Linux.NoDependencies` and cover with an integration test that actually processes a JPEG in CI.
- [Decompression bombs / malformed images] → magic-byte sniff + `SKCodec` bounds check before decode + dimension cap; decode failures return 400, never 500.
- [Quota counter drift if a write path bypasses the service] → all file writes flow through `IStorageQuotaService`; integration test asserts documents and photos both move the counter, including replace and delete.
- [Large uploads held in memory during processing] → 15 MB cap bounds it; acceptable for self-hosted scale.
- [JPEG-only output loses transparency] → accepted; inputs are photographs.

## Migration Plan

Pre-launch reset: delete `src/Steward.Infrastructure/Migrations/`, regenerate `InitialCreate` (adds `AssetPhotos` table, `Assets.CoverPhotoId`, `Households.StorageUsedBytes`/`StorageQuotaOverrideBytes`; drops `Assets.PhotoUrl`), apply to a clean DB. Add `Storage:HouseholdQuotaBytes` and `Storage:MaxPhotoUploadSizeBytes` to appsettings + container env. Regenerate `schema.d.ts`.

## Open Questions

None — decisions settled in exploration; details above fill the gaps consistently with existing patterns.
