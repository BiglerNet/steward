## MODIFIED Requirements

### Requirement: Centralized design tokens
The frontend SHALL define its color palette, corner radii, and type scale as CSS variables in `index.css`, sourced from the `biglernet-design-system` package's OKLCH tokens (`tokens/light.css` / `tokens/dark.css`) rather than hand-authored hex values, and components SHALL consume them through Tailwind's theme mapping rather than hardcoded color or radius values.

#### Scenario: Component uses a themed color
- **WHEN** a component needs a surface, border, accent, or status color
- **THEN** it references the corresponding semantic Tailwind class (e.g. `bg-card`, `border-border`, `text-primary`) backed by a CSS variable resolving to a `biglernet-design-system` token, not a literal hex value

#### Scenario: Updating the palette
- **WHEN** a token value changes upstream in `biglernet-design-system` (e.g. the accent color) and the app updates its dependency
- **THEN** every component using the corresponding semantic class reflects the new value without per-component edits

#### Scenario: Neutral hover surfaces stay neutral
- **WHEN** a component uses shadcn's `accent`/`muted` background roles (e.g. a dropdown menu item's hover state, an outline or ghost button's hover state)
- **THEN** it resolves to the design system's `--surface-hover` token, not `--accent-primary`, so brand-red does not appear on routine hover interactions

## ADDED Requirements

### Requirement: Dark theme support
The frontend SHALL support a dark theme by applying a `.dark` class to an ancestor element, which overrides the light-theme CSS variables with the `biglernet-design-system` package's dark tokens (`tokens/dark.css`).

#### Scenario: Dark class applied
- **WHEN** the `.dark` class is present on the `<html>` element
- **THEN** all components consuming the semantic Tailwind classes render with the dark-theme token values instead of light-theme values

#### Scenario: Focus ring uses the shadow-based focus treatment
- **WHEN** a focusable element (input, button) receives keyboard focus
- **THEN** it shows the design system's `--shadow-focus` box-shadow treatment rather than a flat `--ring` outline color, consistent in both light and dark themes

### Requirement: Webfonts loaded by the consumer
The frontend SHALL load the `Inter` and `IBM Plex Mono` webfonts referenced by the design system's `--font-display` and `--font-mono` tokens, since the design system package does not bundle or load them itself.

#### Scenario: Display and heading text renders in Inter
- **WHEN** a page renders a heading or other element using `--font-display`
- **THEN** the text renders in the `Inter` typeface rather than falling back to the system font stack

#### Scenario: Monospace data renders in IBM Plex Mono
- **WHEN** a component renders monospaced data (e.g. a table cell using `--font-mono`)
- **THEN** the text renders in `IBM Plex Mono` rather than falling back to the system monospace stack
