# Proposal: asset-photos

## Why

Assets currently carry a single free-text `PhotoUrl` that nothing populates or serves — there is no way to actually upload pictures of a vehicle, see them on the asset, or pick which one represents the asset in lists. Photos are the most requested visual improvement from the asset-UX exploration, and the upcoming creation wizard needs a working photo step to build on. Unbounded uploads also need a storage ceiling per household before any file-heavy feature ships.

## What Changes

- **Asset photo uploads**: new `AssetPhoto` entity (0..N per asset) with upload, list, stream, and delete endpoints. Uploads are processed server-side with SkiaSharp: content sniffed by magic bytes (not the declared content type), ~15 MB upload cap, EXIF orientation applied then metadata stripped, re-encoded into exactly two JPEG variants (thumbnail ~320px, display ~2048px, never upscaled); the original file is discarded.
- **Cover photo pointer**: `Asset.CoverPhotoId` (nullable FK) selects the photo shown on asset cards and the detail header — pointer on the asset, not an `IsCover` flag on photos. The first uploaded photo becomes the cover automatically; deleting the cover reassigns it to the newest remaining photo.
- **Household storage quota**: `Household.StorageUsedBytes` counter maintained transactionally by every stored-file write/delete — asset photos AND existing registration/warranty documents. A configured default quota applies to all households, with a per-household override settable only by PlatformAdmin. Uploads that would exceed the quota are rejected. Usage and quota are visible to household members.
- **BREAKING**: `Asset.PhotoUrl` is removed from the entity and all asset DTOs; migrations reset to a fresh `InitialCreate` (pre-launch, no data migration).
- New backend dependency: **SkiaSharp** (+ Linux native assets for container deploys).

## Capabilities

### New Capabilities

- `asset-photo-management`: upload/list/stream/delete asset photos, the SkiaSharp processing pipeline, and cover-photo selection.
- `household-storage-quota`: per-household storage accounting across photos and document attachments, quota enforcement, member-visible usage on household detail, configured default with a PlatformAdmin per-household override endpoint.
- `frontend-asset-photos`: photo gallery on the asset detail page — upload, delete, set-cover — plus cover thumbnails on asset cards.

### Modified Capabilities

- `domain-model`: `AssetPhoto` entity added; `Asset` loses `PhotoUrl` and gains `CoverPhotoId`; migration reset. (The `photoUrl` removal and `coverPhotoId` addition flow through `asset-management` contracts, whose requirement text describes fields generically — no requirement change there.)
- `household-multitenancy`: the `Household` entity gains `StorageUsedBytes` + `StorageQuotaOverrideBytes`.
- `document-storage`: registration/warranty document upload/replace/delete now update household storage usage and enforce the quota.
- `frontend-asset-management`: asset cards render the cover photo thumbnail (icon fallback stays); asset form drops the `photoUrl` input.
- `frontend-household-management`: household settings shows storage usage against the effective quota.

## Impact

- **Backend**: `Steward.Domain` (AssetPhoto, Asset, Household), new `Steward.Application/Photos` + quota service in `Storage`, `Steward.Infrastructure` (SkiaSharp image processor, photo service, EF configurations, regenerated `InitialCreate`), `Steward.Api` (`AssetPhotosController`, PlatformAdmin endpoint). SkiaSharp NuGet added to Infrastructure; container image needs `SkiaSharp.NativeAssets.Linux.NoDependencies`.
- **Frontend**: new photos section on asset detail, updated asset cards/form, household settings usage display, new hooks (`useAssetPhotos`), regenerated `schema.d.ts`. Authenticated image fetching follows the existing document-download pattern (bearer fetch → object URL).
- **Data**: migrations reset; `Storage:HouseholdQuotaBytes` config key added (existing stored documents predate launch, so the usage counter starts correct at zero).
- **Not in scope**: photo captions/reordering, image CDN/caching layers, the creation wizard (next change), photos on entities other than assets.
