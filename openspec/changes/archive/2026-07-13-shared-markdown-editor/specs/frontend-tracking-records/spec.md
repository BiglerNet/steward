## ADDED Requirements

### Requirement: Notes and description fields use the shared markdown editor
The create/edit forms for service records, mileage logs, engine hours logs, fuel logs, registrations, and warranties SHALL render their free-text field (`description` for service records, `notes` for the others) using the shared `MarkdownEditor` component instead of a plain textarea. List and detail views displaying these fields SHALL render the stored value through the shared read-only markdown renderer instead of as raw text.

#### Scenario: Logging a service record with formatted notes
- **WHEN** a Contributor/Owner creates a service record and enters a description containing a bulleted list of work performed
- **THEN** the create form presents the `MarkdownEditor` for that field, and the saved record's description list appears in the service records list as formatted markdown, not raw markdown syntax

#### Scenario: Editing an existing entry's notes
- **WHEN** a Contributor/Owner opens the edit form for an existing mileage log entry with a `notes` value
- **THEN** the `MarkdownEditor` loads that value already rendered in its WYSIWYG form, ready for further editing
