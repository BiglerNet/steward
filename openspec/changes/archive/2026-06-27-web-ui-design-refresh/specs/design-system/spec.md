## ADDED Requirements

### Requirement: Centralized design tokens
The frontend SHALL define its color palette, corner radii, and type scale as CSS variables in `index.css`, matching `docs/design/tokens.md`, and components SHALL consume them through Tailwind's theme mapping rather than hardcoded color or radius values.

#### Scenario: Component uses a themed color
- **WHEN** a component needs a surface, border, accent, or status color
- **THEN** it references the corresponding semantic Tailwind class (e.g. `bg-card`, `border-border`, `text-primary`) backed by a CSS variable, not a literal hex value

#### Scenario: Updating the palette
- **WHEN** a token value in `index.css` changes (e.g. the accent color)
- **THEN** every component using the corresponding semantic class reflects the new value without per-component edits

### Requirement: Type scale applied to headings
The frontend SHALL apply the documented type scale (H1 32px/700, H2 20px/700, H3 16px/600, body 14px/500) to page headings and section titles via shared utility classes, rather than ad-hoc `text-*`/`font-*` combinations per page.

#### Scenario: Page heading rendering
- **WHEN** a page renders its top-level `<h1>`
- **THEN** it visually matches the H1 scale (32px, 700 weight, -0.02em tracking) defined in the tokens
