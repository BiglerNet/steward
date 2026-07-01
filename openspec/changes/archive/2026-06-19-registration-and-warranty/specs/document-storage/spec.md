## ADDED Requirements

### Requirement: Upload a document to a registration or warranty record
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/registrations/{id}/document` and the equivalent `.../warranties/{id}/document` endpoint (Contributor or Owner only) accepting a single multipart file upload. Allowed content types SHALL be `application/pdf`, `image/jpeg`, `image/png`. Files exceeding the configured maximum size SHALL be rejected. Uploading a new document SHALL replace any existing one for that record. On success it SHALL return HTTP 200 with the updated response (`hasDocument: true`).

#### Scenario: Contributor uploads a registration card photo
- **WHEN** a Contributor POSTs a JPEG file to the registration document endpoint
- **THEN** HTTP 200 is returned with `hasDocument: true`

#### Scenario: Unsupported content type rejected
- **WHEN** a user POSTs a `.zip` file to the document endpoint
- **THEN** HTTP 400 is returned

#### Scenario: File exceeds size limit
- **WHEN** a user POSTs a file larger than the configured maximum
- **THEN** HTTP 400 is returned

#### Scenario: Viewer cannot upload a document
- **WHEN** a user with `Role = Viewer` POSTs to the document endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Uploading a new document replaces the old one
- **WHEN** a Contributor uploads a second document to a record that already has one
- **THEN** HTTP 200 is returned and downloading the document returns only the newly uploaded file

---

### Requirement: Download a registration or warranty document
The system SHALL provide `GET /api/households/{householdId}/assets/{assetId}/registrations/{id}/document` and the `.../warranties/{id}/document` equivalent (any Active member or PlatformAdmin) streaming the stored file with its original content type. If no document is attached, HTTP 404 SHALL be returned.

#### Scenario: Member downloads an attached document
- **WHEN** a user with any Active role calls the document download endpoint for a record with an attached PDF
- **THEN** HTTP 200 is returned with `Content-Type: application/pdf` and the file body

#### Scenario: No document attached
- **WHEN** a user calls the document download endpoint for a record with no attached document
- **THEN** HTTP 404 is returned

---

### Requirement: Delete a registration or warranty document
The system SHALL provide `DELETE /api/households/{householdId}/assets/{assetId}/registrations/{id}/document` and the `.../warranties/{id}/document` equivalent (Contributor or Owner only), removing the attachment without deleting the parent record. On success it SHALL return HTTP 204.

#### Scenario: Contributor removes an attachment
- **WHEN** a Contributor calls `DELETE` on the document endpoint for a record with an attached document
- **THEN** HTTP 204 is returned, the parent record still exists, and `hasDocument` becomes `false`

#### Scenario: Viewer cannot delete a document
- **WHEN** a user with `Role = Viewer` calls the delete document endpoint
- **THEN** HTTP 403 is returned
