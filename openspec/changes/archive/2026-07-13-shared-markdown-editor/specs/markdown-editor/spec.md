## ADDED Requirements

### Requirement: Shared WYSIWYG markdown editor component
The frontend SHALL provide a shared `MarkdownEditor` component that presents a full WYSIWYG editing surface (no visible markdown syntax during normal editing) while persisting its value as plain markdown text. The component SHALL be backed by a markdown-native editor implementation (the document's source of truth is markdown text itself, not HTML or a proprietary JSON tree translated to markdown on save).

#### Scenario: Editing produces markdown text
- **WHEN** a user types a heading, a bold phrase, and a bulleted list into the `MarkdownEditor`
- **THEN** the component's value is valid markdown text (e.g. `# Heading`, `**bold**`, `- item`) suitable for storing directly in a string field

#### Scenario: Loading existing markdown renders as WYSIWYG
- **WHEN** the `MarkdownEditor` is initialized with an existing markdown string containing headings, emphasis, and lists
- **THEN** the editor renders those elements in their formatted (WYSIWYG) form rather than showing raw markdown syntax

#### Scenario: No length constraint imposed by the editor
- **WHEN** a user enters a long multi-paragraph markdown document into the `MarkdownEditor`
- **THEN** the component does not truncate or reject the input based on length

---

### Requirement: Safe read-only markdown rendering
The frontend SHALL provide a read-only markdown renderer used to display stored markdown content (in list rows and detail views) that renders standard markdown formatting (headings, emphasis, lists, links) without executing or rendering arbitrary embedded HTML from the source text.

#### Scenario: Formatted display of stored markdown
- **WHEN** a record with a markdown-formatted description (containing a heading and a bulleted list) is displayed in a detail view
- **THEN** the heading and list render as formatted HTML elements, not as raw markdown syntax

#### Scenario: Embedded HTML in stored content is not executed
- **WHEN** a stored markdown value contains a raw `<script>` tag or other embedded HTML
- **THEN** the renderer does not execute or render that HTML as live markup
