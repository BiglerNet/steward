# Tasks: asset-photos

## 1. Domain

- [x] 1.1 Add `AssetPhoto` entity (Id, AssetId, ThumbStorageKey, DisplayStorageKey, Width, Height, SizeBytes, CreatedAt)
- [x] 1.2 `Asset`: remove `PhotoUrl`, add `CoverPhotoId` (Guid?)
- [x] 1.3 `Household`: add `StorageUsedBytes` (long) + `StorageQuotaOverrideBytes` (long?)

## 2. Image processing (backend)

- [x] 2.1 Add SkiaSharp + `SkiaSharp.NativeAssets.Linux.NoDependencies` to Infrastructure; verify container build
- [x] 2.2 Define `IImageProcessor` in Application (input stream → validated, oriented, two JPEG variants with dimensions/sizes); implement with SkiaSharp in Infrastructure: magic-byte sniff (JPEG/PNG/WebP), `SKCodec` bounds check (≤12,000px/side) before decode, EXIF orient, resize ≤2048px / ≤320px without upscaling, JPEG q≈80
- [x] 2.3 Add `Storage:MaxPhotoUploadSizeBytes` (15 MB) and `Storage:HouseholdQuotaBytes` (1 GB) to options + appsettings + compose/k8s env
- [x] 2.4 Unit tests with real image fixtures: orientation applied, metadata stripped, no upscale, disguised non-image rejected, oversized dimensions rejected pre-decode

## 3. Storage quota service

- [x] 3.1 Add `IStorageQuotaService` (`EnsureCapacityAsync`, `AdjustUsageAsync`) in Application; EF implementation adjusting `Household.StorageUsedBytes` in the caller's transaction
- [x] 3.2 Route registration/warranty document upload/replace/delete through the quota service (replace adjusts by difference)
- [x] 3.3 Integration tests: usage counter moves on document upload/replace/delete; over-quota document upload → 400, nothing stored

## 4. Photo service + endpoints

- [x] 4.1 Application: `Photos/Dtos.cs` (`AssetPhotoResponse`, cover request), `IAssetPhotoService`, validators
- [x] 4.2 Infrastructure: `AssetPhotoService` — upload (process → quota check on stored bytes → save both variants → rollback files on failure → auto-cover on first photo), list newest-first, stream variant, delete (remove files, decrement usage, reassign cover to newest remaining or null), set-cover (reject foreign photo); EF `AssetPhotoConfiguration` (cascade from Asset); register in `AddStewardAssets`/new extension
- [x] 4.3 Api: `AssetPhotosController` (`POST`/`GET` photos, `GET .../{photoId}/content?variant=`, `DELETE`), cover endpoint `PUT .../assets/{assetId}/cover-photo`; household authorization via existing handler
- [x] 4.4 `AssetResponse`: drop `photoUrl`, add `coverPhotoId`; update `AssetMapper` and asset DTO interfaces
- [x] 4.5 Integration tests: upload→201 + auto-cover, variant streaming + invalid variant 400, delete reassigns/clears cover, foreign cover 400, viewer 403s, over-quota photo 400, usage counter matches stored variant bytes

## 5. PlatformAdmin + household exposure

- [x] 5.1 `PUT /api/admin/households/{householdId}/storage-quota` (`{ quotaBytes: long? }`, PlatformAdmin only, non-positive → 400; routed under `/api/admin/...` alongside the existing `PlatformAdminController`)
- [x] 5.2 `HouseholdResponse`: add `storageUsedBytes` + effective `storageQuotaBytes`
- [x] 5.3 Integration tests: override set/clear changes effective quota, Owner-without-admin 403, member sees usage/quota in household detail

## 6. Migration reset

- [x] 6.1 Delete `Migrations/`, regenerate `InitialCreate` (AssetPhotos table, `Assets.CoverPhotoId`, no `Assets.PhotoUrl`, household storage columns), apply to clean DB
- [x] 6.2 Full backend suite green

## 7. Frontend API layer

- [x] 7.1 Regenerate `schema.d.ts`; update `api/types.ts` (drop `photoUrl`, add `coverPhotoId`, `AssetPhoto`, household storage fields)
- [x] 7.2 Add `api/assetPhotos.ts` (list/upload/delete/setCover/content-blob) + `hooks/useAssetPhotos.ts`; shared authenticated-image → object-URL helper (reuse document-download pattern, revoke on unmount)

## 8. Frontend photos UI

- [x] 8.1 `PhotosSection` on asset detail: thumbnail grid with cover marker, click-to-view display variant, empty state; upload (client-side type/size pre-check), delete with confirm, set-cover — Contributor/Owner only; surface quota-exceeded message
- [x] 8.2 Asset cards: cover thumbnail when `coverPhotoId` set, icon fallback otherwise; remove `photoUrl` from form, fixtures, and tests
- [x] 8.3 Component tests: gallery render + cover marker, viewer read-only, upload calls API and refreshes, quota error surfaced, card thumbnail fallback

## 9. Frontend household settings

- [x] 9.1 Storage usage summary + progress indicator (warning ≥90%) on settings page, visible to all members
- [x] 9.2 Component tests: usage rendering and warning threshold

## 10. Verification

- [x] 10.1 `dotnet build` + `dotnet test`, `npm test`, `tsc -b`, lint, `vite build` all green
- [x] 10.2 API-level smoke with a real image: upload (sideways EXIF JPEG) → variants correct, cover auto-set, set-cover, delete-reassign, quota exceeded path, document upload counted, household usage visible
