## ADDED Requirements

### Requirement: Reusable document attachment widget
The frontend SHALL provide a single `DocumentAttachment` component, parameterized by upload/download/delete endpoints, reused by Registration and Warranty records.

#### Scenario: Widget reused across record types
- **WHEN** the document attachment widget is rendered on a registration record and on a warranty record
- **THEN** both use the same component, configured only with that record type's endpoint paths

### Requirement: Client-side file validation before upload
The frontend SHALL reject a file before calling the upload endpoint if its content type isn't in the allowed set (`application/pdf`, `image/jpeg`, `image/png`) or its size exceeds the configured maximum, showing an inline error.

#### Scenario: Unsupported file type selected
- **WHEN** a user selects a file whose content type isn't allowed
- **THEN** the app shows an inline error and does not call the upload endpoint

#### Scenario: File too large
- **WHEN** a user selects a file exceeding the maximum allowed size
- **THEN** the app shows an inline error and does not call the upload endpoint

#### Scenario: Backend rejects a file the client-side check missed
- **WHEN** the upload endpoint returns `400 BadRequest` despite passing client-side checks (e.g. stale client constants)
- **THEN** the app surfaces the server's error message via the global toast pattern

### Requirement: Document preview and download
The frontend SHALL show whether a record has an attached document and provide a download action when one exists.

#### Scenario: Record has a document
- **WHEN** a record's `hasDocument` is `true`
- **THEN** the widget shows the attachment is present with a download action pointing at `documentUrl`

#### Scenario: Record has no document
- **WHEN** a record's `hasDocument` is `false`
- **THEN** the widget shows an "attach document" upload action and no download action

### Requirement: Replace and remove document
The frontend SHALL allow replacing an existing document (re-upload, overwriting the prior one) or removing it entirely via the delete-document endpoint.

#### Scenario: Replacing an attached document
- **WHEN** a Contributor/Owner uploads a new file on a record that already has a document
- **THEN** the app calls the upload endpoint again and the widget reflects the new document, replacing the old reference

#### Scenario: Removing an attached document
- **WHEN** a Contributor/Owner removes a record's document
- **THEN** the app calls the delete-document endpoint and the widget reverts to the "attach document" state

#### Scenario: Viewer cannot modify documents
- **WHEN** a Viewer-role user views a record with an attached document
- **THEN** the widget shows the download action but hides upload/replace/remove controls
