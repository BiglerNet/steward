## ADDED Requirements

### Requirement: Photo gallery on asset detail
The frontend SHALL show an asset's photos on its detail page as a thumbnail grid fetched via `GET .../photos`, marking the current cover. Photos SHALL be fetched with the authenticated client and rendered via object URLs (same pattern as document downloads); no unauthenticated image URLs. Clicking a thumbnail SHALL show the display-size variant.

#### Scenario: Member browses photos
- **WHEN** any household member opens an asset detail page for an asset with photos
- **THEN** a thumbnail grid renders with the cover photo indicated, and clicking a thumbnail shows the larger display variant

#### Scenario: No photos yet
- **WHEN** an asset has no photos
- **THEN** the gallery area shows an empty state, with an upload prompt for Contributors/Owners only

### Requirement: Upload, delete, and set cover from the gallery
The frontend SHALL let Contributors and Owners upload photos (client-side pre-checks for file type and the configured size cap before POSTing), delete photos with confirmation, and set any photo as cover via `PUT .../cover-photo`. Viewers SHALL see none of these controls. A quota-exceeded rejection from the API SHALL surface the server's message to the user.

#### Scenario: Contributor uploads a photo
- **WHEN** a Contributor selects a JPEG within the size cap
- **THEN** the app POSTs it to the photos endpoint and the new photo appears in the grid (as cover if it is the asset's first)

#### Scenario: Contributor sets a different cover
- **WHEN** a Contributor uses the set-cover control on a non-cover photo
- **THEN** the app calls `PUT .../cover-photo` and the cover marker moves to that photo

#### Scenario: Quota-exceeded upload surfaces the reason
- **WHEN** an upload is rejected because the household storage quota would be exceeded
- **THEN** the app shows the quota-exceeded message rather than a generic failure

#### Scenario: Viewer sees a read-only gallery
- **WHEN** a Viewer-role user opens the detail page of an asset with photos
- **THEN** the grid renders without upload, delete, or set-cover controls
