## ADDED Requirements

### Requirement: Asset description uses the shared markdown editor
The asset create/edit form's `description` field SHALL render using the shared `MarkdownEditor` component instead of a plain textarea. Asset detail views displaying the description SHALL render it through the shared read-only markdown renderer instead of as raw text.

#### Scenario: Editing an asset's description
- **WHEN** a Contributor/Owner opens the asset edit form for an asset with an existing markdown-formatted `description`
- **THEN** the `MarkdownEditor` loads that value in its WYSIWYG form for further editing

#### Scenario: Viewing a formatted description
- **WHEN** an asset's `description` contains markdown formatting (e.g. a heading and a list) and its detail page is viewed
- **THEN** the description renders as formatted markdown, not raw markdown syntax
