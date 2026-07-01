# frontend-registration-tracking Specification

## Purpose
TBD - created by archiving change frontend-documents. Update Purpose after archive.
## Requirements
### Requirement: List registration history for an asset
The frontend SHALL list an asset's registration records via `GET /api/households/{householdId}/assets/{assetId}/registrations`, ordered by `expiresOn` descending.

#### Scenario: Viewing registration history
- **WHEN** any household member opens `/households/:householdId/assets/:assetId/registrations`
- **THEN** the app lists the asset's registration records with the most recent renewal first

#### Scenario: No registrations yet
- **WHEN** an asset has no registration records
- **THEN** the app shows an empty state prompting the first entry

### Requirement: Create and edit registration record
The frontend SHALL provide create/edit forms for a registration record (registration number, issuing authority, renewed-on, cost, expires-on, notes), available to Contributors and Owners.

#### Scenario: Logging a renewal
- **WHEN** a Contributor/Owner submits a new registration renewal
- **THEN** the app creates it via `POST .../registrations` and it appears at the top of the history list

#### Scenario: Correcting a past renewal
- **WHEN** a Contributor/Owner edits an existing registration record's fields
- **THEN** the app submits the update via `PUT .../registrations/{id}` without affecting other history entries

#### Scenario: Viewer cannot create or edit
- **WHEN** a Viewer-role user views the registration history
- **THEN** the app hides create/edit controls

### Requirement: Delete registration record
The frontend SHALL allow Contributors and Owners to delete a registration record via `DELETE .../registrations/{id}`.

#### Scenario: Contributor deletes a registration entry
- **WHEN** a Contributor confirms deletion of a registration record
- **THEN** the app calls the delete endpoint and removes it from the history list without affecting other entries

### Requirement: Expiry status cue
The frontend SHALL visually flag a registration record as overdue or due soon based on its `expiresOn` date, without sending any notification.

#### Scenario: Overdue registration
- **WHEN** a registration's `expiresOn` is in the past
- **THEN** the app shows an "overdue" badge on that record

#### Scenario: Registration due soon
- **WHEN** a registration's `expiresOn` falls within the configured "coming due" window
- **THEN** the app shows a "due soon" badge on that record

### Requirement: Document attachment on a registration record
The frontend SHALL allow attaching, replacing, downloading, and removing a document on an existing registration record using the shared document-attachment widget.

#### Scenario: Attaching proof of registration
- **WHEN** a Contributor/Owner uploads a file on an existing registration record
- **THEN** the app calls `POST .../registrations/{id}/document` and the record's attachment state updates to show the new document

#### Scenario: Downloading an attached document
- **WHEN** any household member opens the download link on a registration record with `hasDocument: true`
- **THEN** the app fetches `GET .../registrations/{id}/document`
