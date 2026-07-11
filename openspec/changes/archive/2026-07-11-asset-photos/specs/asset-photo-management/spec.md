## ADDED Requirements

### Requirement: Upload asset photo with server-side processing
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/photos` (Contributor or Owner only) accepting a single multipart image upload. The server SHALL identify the format by magic-byte sniffing (JPEG, PNG, and WebP accepted; the declared `Content-Type` SHALL NOT be trusted), reject uploads exceeding the configured maximum photo size, and reject images exceeding 12,000 pixels on either side using codec header bounds before full decode. Accepted images SHALL be normalized: EXIF orientation applied, metadata stripped, and exactly two JPEG variants stored — a thumbnail (long edge ≤ 320px) and a display variant (long edge ≤ 2048px, never upscaled). The original upload SHALL NOT be persisted. On success it SHALL return HTTP 201 with an `AssetPhotoResponse` (id, assetId, width, height, sizeBytes, createdAt).

#### Scenario: Contributor uploads a sideways phone photo
- **WHEN** a Contributor POSTs a 4000×3000 JPEG with EXIF orientation 6 and GPS metadata
- **THEN** HTTP 201 is returned, the stored display variant is 2048px on its long edge with orientation applied, the thumbnail is ≤ 320px, and neither variant contains EXIF or GPS metadata

#### Scenario: Small image is not upscaled
- **WHEN** a Contributor uploads a 800×600 image
- **THEN** the display variant remains 800×600 and only the thumbnail is downscaled

#### Scenario: Disguised non-image rejected
- **WHEN** a user POSTs a file with `Content-Type: image/jpeg` whose bytes are not a supported image format
- **THEN** HTTP 400 is returned and nothing is stored

#### Scenario: Oversized dimensions rejected before decode
- **WHEN** a user POSTs an image whose header declares 20,000×20,000 pixels
- **THEN** HTTP 400 is returned without a full decode

#### Scenario: Viewer cannot upload a photo
- **WHEN** a user with `Role = Viewer` POSTs to the photos endpoint
- **THEN** HTTP 403 is returned

### Requirement: List and stream asset photos
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/photos` (any Active member or PlatformAdmin) returning the asset's photos newest-first, and `GET .../photos/{photoId}/content?variant=thumb|display` streaming the requested JPEG variant. An unknown `variant` value SHALL return HTTP 400; a missing photo SHALL return HTTP 404.

#### Scenario: Member views a photo variant
- **WHEN** an Active member requests `.../photos/{photoId}/content?variant=display`
- **THEN** HTTP 200 is returned with `Content-Type: image/jpeg` and the display variant body

#### Scenario: Invalid variant rejected
- **WHEN** a member requests `?variant=original`
- **THEN** HTTP 400 is returned

#### Scenario: Non-member cannot view photos
- **WHEN** a user with no membership in the household requests the photo list or content
- **THEN** HTTP 403 is returned

### Requirement: Cover photo selection
Each `Asset` SHALL reference at most one of its photos as cover via `Asset.CoverPhotoId` (pointer on the asset; no flag on photos). The first photo uploaded to a coverless asset SHALL automatically become the cover. The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/cover-photo` (Contributor or Owner only) accepting `{ photoId }`; a `photoId` not belonging to that asset SHALL return HTTP 400. `AssetResponse` SHALL include `coverPhotoId`.

#### Scenario: First upload becomes cover
- **WHEN** a Contributor uploads a photo to an asset with no photos
- **THEN** the asset's `coverPhotoId` equals the new photo's id

#### Scenario: Contributor changes the cover
- **WHEN** a Contributor PUTs `{ photoId }` referencing another photo of the same asset
- **THEN** HTTP 200 is returned and subsequent asset responses carry the new `coverPhotoId`

#### Scenario: Foreign photo rejected as cover
- **WHEN** a Contributor PUTs a `photoId` that belongs to a different asset
- **THEN** HTTP 400 is returned

### Requirement: Delete asset photo
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/photos/{photoId}` (Contributor or Owner only), removing the photo record and both stored variants and returning HTTP 204. If the deleted photo was the cover, the cover SHALL be reassigned to the asset's newest remaining photo, or cleared when none remain.

#### Scenario: Deleting the cover reassigns it
- **WHEN** a Contributor deletes the cover photo of an asset that has two other photos
- **THEN** HTTP 204 is returned and the asset's `coverPhotoId` now references the newest remaining photo

#### Scenario: Deleting the last photo clears the cover
- **WHEN** a Contributor deletes an asset's only photo
- **THEN** HTTP 204 is returned and the asset's `coverPhotoId` is null

#### Scenario: Viewer cannot delete a photo
- **WHEN** a user with `Role = Viewer` calls the delete endpoint
- **THEN** HTTP 403 is returned
