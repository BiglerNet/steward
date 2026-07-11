# frontend-registration-tracking Specification

## Purpose
TBD - created by archiving change frontend-documents. Update Purpose after archive.
## Requirements
### Requirement: List registration history for an asset
The frontend SHALL list an asset's registration records via `GET /api/households/{householdId}/assets/{assetId}/registrations` in the order returned by the API (current-first), showing each record's kind as a badge (Registration / Trail pass / Permit) alongside its number, validity dates, and cost.

#### Scenario: Viewing registration history
- **WHEN** any household member opens `/households/:householdId/assets/:assetId/registrations`
- **THEN** the app lists the asset's records with the most recent first, each labeled with its kind badge

#### Scenario: No registrations yet
- **WHEN** an asset has no registration records
- **THEN** the app shows an empty state prompting the first entry

### Requirement: Create and edit registration record
The frontend SHALL provide create/edit forms for a registration record (kind, number, issuing authority, renewed-on, valid-from, cost, expires-on, notes), available to Contributors and Owners. `kind` SHALL be a required selection; the number field's label SHALL follow the kind ("Registration #", "Pass #", "Permit #") and SHALL be optional. The issuing-authority input SHALL be a free-text combobox seeded from the region registry: regions of the household's country listed first with the household's own region at the top, other supported countries' regions after, and any typed value accepted as-is.

#### Scenario: Logging a renewal
- **WHEN** a Contributor/Owner selects a kind and submits a new record
- **THEN** the app creates it via `POST .../registrations` and it appears in the history list with its kind badge

#### Scenario: Issuing authority suggested from household location
- **WHEN** a Contributor whose household has `region: "US-WI"` opens the issuing-authority combobox
- **THEN** "Wisconsin" appears as the top suggestion, other US states follow, Canadian provinces appear after, and typing an arbitrary value like "Bayfield County" is accepted

#### Scenario: Correcting a past renewal
- **WHEN** a Contributor/Owner edits an existing registration record's fields
- **THEN** the app submits the update via `PUT .../registrations/{id}` without affecting other history entries

#### Scenario: Viewer cannot create or edit
- **WHEN** a Viewer-role user views the registration history
- **THEN** the app hides create/edit controls

### Requirement: Renew action prefills a new record
The frontend SHALL show a "Renew" action on each registration record (Contributors and Owners) that opens the create form prefilled from that record with `renewedOn`, `validFrom`, `expiresOn`, and `cost` cleared. Submitting SHALL create a new record via the normal create endpoint; the source record SHALL be unchanged.

#### Scenario: Renewing a registration without re-entry
- **WHEN** a Contributor clicks "Renew" on a record with `kind: "Registration"`, `registrationNumber: "ABC-1234"`, `issuingAuthority: "Wisconsin"`
- **THEN** the create form opens with kind, number, issuing authority, and notes prefilled and dates/cost empty, and submitting adds a new record while the original remains

### Requirement: Typical permit-kind nudges
The frontend SHALL, on an asset's registrations tab, compare the asset's registry `typicalPermitKinds` against its registration records and show a non-blocking inline hint for each typical kind with no current record (a record is current when `expiresOn` is today or later, or has no `expiresOn`). The hint SHALL not block any action and SHALL disappear once a current record of that kind exists.

#### Scenario: Missing trail pass nudge
- **WHEN** a member views the registrations tab of a Snowmobile whose registry entry lists `TrailPass` in `typicalPermitKinds` and whose only trail pass expired last season
- **THEN** the app shows a hint that snowmobiles usually need a trail pass and none is current

#### Scenario: Nudge clears when a current record exists
- **WHEN** a Contributor adds a trail pass whose `expiresOn` is in the future
- **THEN** the trail-pass hint is no longer shown

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
