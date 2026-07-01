# frontend-warranty-tracking Specification

## Purpose
TBD - created by archiving change frontend-documents. Update Purpose after archive.
## Requirements
### Requirement: List warranties for an asset
The frontend SHALL list an asset's warranty records via `GET /api/households/{householdId}/assets/{assetId}/warranties`.

#### Scenario: Viewing warranties
- **WHEN** any household member opens `/households/:householdId/assets/:assetId/warranties`
- **THEN** the app lists the asset's warranty records

#### Scenario: No warranties yet
- **WHEN** an asset has no warranty records
- **THEN** the app shows an empty state prompting the first entry

### Requirement: Create and edit warranty record
The frontend SHALL provide create/edit forms for a warranty record (provider, description, starts-on, expires-on, notes), available to Contributors and Owners.

#### Scenario: Adding a warranty
- **WHEN** a Contributor/Owner submits a new warranty record
- **THEN** the app creates it via `POST .../warranties` and it appears in the warranty list

#### Scenario: Editing a warranty
- **WHEN** a Contributor/Owner edits an existing warranty record's fields
- **THEN** the app submits the update via `PUT .../warranties/{id}`

#### Scenario: Viewer cannot create or edit
- **WHEN** a Viewer-role user views the warranty list
- **THEN** the app hides create/edit controls

### Requirement: Delete warranty record
The frontend SHALL allow Contributors and Owners to delete a warranty record via `DELETE .../warranties/{id}`.

#### Scenario: Contributor deletes a warranty
- **WHEN** a Contributor confirms deletion of a warranty record
- **THEN** the app calls the delete endpoint and removes it from the list

### Requirement: Expiry status cue
The frontend SHALL visually flag a warranty record as overdue or due soon based on its `expiresOn` date, without sending any notification.

#### Scenario: Overdue warranty
- **WHEN** a warranty's `expiresOn` is in the past
- **THEN** the app shows an "overdue" badge on that record

#### Scenario: Warranty due soon
- **WHEN** a warranty's `expiresOn` falls within the configured "coming due" window
- **THEN** the app shows a "due soon" badge on that record

### Requirement: Document attachment on a warranty record
The frontend SHALL allow attaching, replacing, downloading, and removing a document on an existing warranty record using the shared document-attachment widget.

#### Scenario: Attaching proof of warranty coverage
- **WHEN** a Contributor/Owner uploads a file on an existing warranty record
- **THEN** the app calls `POST .../warranties/{id}/document` and the record's attachment state updates to show the new document

#### Scenario: Downloading an attached document
- **WHEN** any household member opens the download link on a warranty record with `hasDocument: true`
- **THEN** the app fetches `GET .../warranties/{id}/document`
