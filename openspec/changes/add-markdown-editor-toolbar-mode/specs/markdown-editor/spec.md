## MODIFIED Requirements

### Requirement: Shared WYSIWYG markdown editor component
The frontend SHALL provide a shared `MarkdownEditor` component that presents a full WYSIWYG editing surface (no visible markdown syntax required during normal editing) while persisting its value as plain markdown text. The component SHALL be backed by a markdown-native editor implementation (the document's source of truth is markdown text itself, not HTML or a proprietary JSON tree translated to markdown on save). The WYSIWYG surface SHALL include a fixed, always-visible formatting toolbar exposing the common formatting operations (heading level, bold, italic, bulleted list, numbered list, link) as clickable controls, so a user can produce formatted output without knowing any markdown syntax.

#### Scenario: Editing produces markdown text
- **WHEN** a user types a heading, a bold phrase, and a bulleted list into the `MarkdownEditor`
- **THEN** the component's value is valid markdown text (e.g. `# Heading`, `**bold**`, `- item`) suitable for storing directly in a string field

#### Scenario: Loading existing markdown renders as WYSIWYG
- **WHEN** the `MarkdownEditor` is initialized with an existing markdown string containing headings, emphasis, and lists
- **THEN** the editor renders those elements in their formatted (WYSIWYG) form rather than showing raw markdown syntax

#### Scenario: No length constraint imposed by the editor
- **WHEN** a user enters a long multi-paragraph markdown document into the `MarkdownEditor`
- **THEN** the component does not truncate or reject the input based on length

#### Scenario: Toolbar produces formatted markdown without typed syntax
- **WHEN** a user selects a word in the WYSIWYG surface and clicks the toolbar's Bold control (without typing `**` anywhere)
- **THEN** the selected word is rendered bold in the WYSIWYG view and the component's underlying value contains that word wrapped in `**`

#### Scenario: Toolbar heading control applies a heading level
- **WHEN** a user places the cursor on a line of plain text and selects "Heading 1" from the toolbar's heading control
- **THEN** that line renders as a top-level heading in the WYSIWYG view and the component's underlying value has that line prefixed with `# `

## ADDED Requirements

### Requirement: WYSIWYG / source mode toggle
`MarkdownEditor` SHALL provide a toggle allowing the user to switch between the WYSIWYG editing surface and a plain-text source view that shows the field's literal markdown syntax. The component SHALL default to WYSIWYG mode whenever it is mounted. Both modes SHALL read from and write to the same underlying markdown string value — switching modes SHALL NOT lose, duplicate, or corrupt any content entered in the other mode.

#### Scenario: Defaults to WYSIWYG mode
- **WHEN** `MarkdownEditor` is mounted, regardless of whether it is given an empty value or an existing markdown string
- **THEN** it initially renders the WYSIWYG editing surface, not the source view

#### Scenario: Switching to source mode shows literal markdown
- **WHEN** a user in WYSIWYG mode has produced formatted content (e.g. a heading and a bold phrase) and then switches to source mode via the toggle
- **THEN** the source view displays the literal markdown text corresponding to that content (e.g. `# Heading` and `**bold**` visible as typed characters)

#### Scenario: Editing in source mode reflects back in WYSIWYG mode
- **WHEN** a user in source mode types markdown syntax directly (e.g. adds a new `- item` line) and then switches back to WYSIWYG mode
- **THEN** the WYSIWYG surface renders that addition in its formatted form (a new bulleted list item)

#### Scenario: Mode switching preserves unsaved content
- **WHEN** a user makes an edit in either mode without triggering a save (no blur/submit yet) and then toggles to the other mode
- **THEN** the edit is present in the newly active mode's view and is included in the value if the field is saved from that mode
