# frontend-asset-management Specification

## Purpose
TBD - created by archiving change frontend-assets-and-tracking. Update Purpose after archive.
## Requirements
### Requirement: Asset type registry consumption
The frontend SHALL fetch the asset type registry from `GET /api/asset-types` via a `useAssetTypeRegistry()` TanStack Query hook configured to fetch once per session (`staleTime: Infinity`), and SHALL use it as the single source of truth for category display labels, grouping, applicable type-specific fields, icon identity, and default usage tracking modes. Category icons SHALL be rendered through a shared `AssetTypeIcon` component that maps the registry's `icon` name to a lucide icon (with a neutral fallback for unknown names) on a theme-aware tinted chip whose colors are frontend-owned CSS variables per registry group — the registry SHALL NOT be a source of colors. Type-specific fields backed by a domain enum (`hullType`, `driveType`) SHALL render as selects with the enum's members as options; other fields keep their text/number inputs. Views that depend on the registry SHALL show a loading state until it resolves and a retryable error state if the fetch fails.

#### Scenario: Registry fetched once per session
- **WHEN** a user navigates between the asset list, detail, and form views repeatedly in one session
- **THEN** the asset-types endpoint is called at most once

#### Scenario: Registry drives field applicability
- **WHEN** the registry entry for `Sailboat` lists `hin`, `hullMaterial`, `hullType`, `keelType`, `mastHeightFt`, `mastCount`, `lengthFt`, `beamFt`, `make`, `model`, `color` as applicable fields
- **THEN** the asset form for a Sailboat renders exactly those type-specific inputs, without any frontend-hardcoded per-category field list

#### Scenario: Enum-backed fields render as selects
- **WHEN** the asset form renders `driveType` for a PowerBoat
- **THEN** it renders a select whose options are the `DriveType` enum members with readable labels (e.g. "Stern drive (I/O)"), not a free-text input

#### Scenario: Unknown icon name degrades gracefully
- **WHEN** the registry serves an `icon` name with no entry in the frontend's lucide icon map
- **THEN** the chip renders the neutral fallback icon rather than a letter, a blank, or an error

#### Scenario: Chip is readable in dark mode
- **WHEN** the dark theme is active and an asset-type chip renders (type picker, asset card)
- **THEN** the chip uses the dark values of the group tint variables and the icon remains legible against it

### Requirement: Asset list
The frontend SHALL list a household's assets via `GET /api/households/{householdId}/assets`, optionally filtered by category, showing name, category display label (from the registry), year, and the cover photo thumbnail when the asset has one (`coverPhotoId` non-null), fetched with the authenticated client and rendered via object URLs. Assets without a cover photo SHALL fall back to the registry icon treatment. Each card SHALL show the category's icon via the shared `AssetTypeIcon` chip (lucide icon on the group-tinted, theme-aware chip) rather than a letter or hardcoded color.

#### Scenario: Viewing assets
- **WHEN** a household member opens `/households/:householdId/assets`
- **THEN** the app lists all assets in that household with registry-provided display labels

#### Scenario: Cover thumbnail on the card
- **WHEN** an asset has a `coverPhotoId`
- **THEN** its card shows that photo's thumbnail variant, while assets without one keep the icon fallback

#### Scenario: Category icon on the card
- **WHEN** an asset card renders in either theme
- **THEN** it shows the category's lucide icon on its group-tinted chip, legible in both light and dark modes

#### Scenario: Filtering by category
- **WHEN** a user selects a category filter
- **THEN** the list shows only assets of that category

#### Scenario: No assets yet
- **WHEN** a household has zero assets
- **THEN** the app shows an empty state prompting asset creation

### Requirement: Type-adaptive asset create/edit form
The frontend SHALL provide an edit form (dialog) for assets whose visible type-specific fields adapt to the asset's `AssetCategory` as defined by the registry's `applicableFields`, submitting to `PUT /api/households/{householdId}/assets/{assetId}`. The category SHALL not be changeable on edit. Asset creation SHALL NOT use this dialog: create controls (the asset list's new-asset action and the empty state's prompt) SHALL navigate to the creation wizard at `/households/:householdId/assets/new`. The registry-driven type-specific field inputs SHALL be shared between the edit dialog and the wizard's Details step rather than duplicated.

#### Scenario: Editing a Car
- **WHEN** a Contributor/Owner edits a Car
- **THEN** the dialog renders exactly the type-specific inputs the registry lists for Car and submits an update request, with inapplicable fields omitted or null

#### Scenario: Create actions open the wizard
- **WHEN** a Contributor clicks the new-asset action on the asset list (or the empty state's prompt)
- **THEN** the app navigates to `/households/:householdId/assets/new` instead of opening a dialog

#### Scenario: Category not changeable on edit
- **WHEN** a user edits an existing asset
- **THEN** the category is displayed but cannot be changed

#### Scenario: Viewer cannot create or edit
- **WHEN** a Viewer-role user opens the asset list
- **THEN** the app hides create/edit controls

### Requirement: Asset detail view
The frontend SHALL show an asset's full details, including its registry display label and the type-specific fields applicable to its category, via `GET /api/households/{householdId}/assets/{assetId}`.

#### Scenario: Viewing an asset
- **WHEN** any household member opens an asset's detail page
- **THEN** the app shows its name, category display label, and all populated type-specific fields

### Requirement: Asset deletion restricted to Owners
The frontend SHALL only show the delete-asset control to Owner-role users, calling `DELETE /api/households/{householdId}/assets/{assetId}`.

#### Scenario: Owner deletes an asset
- **WHEN** an Owner confirms deletion of an asset
- **THEN** the app calls the delete endpoint and navigates back to the asset list

#### Scenario: Contributor cannot see delete control
- **WHEN** a Contributor (non-Owner) views an asset's detail page
- **THEN** the app does not show a delete control

### Requirement: Engine management nested under an asset
The frontend SHALL provide list/create/edit UI for an asset's engines via `GET/POST/PUT /api/households/{householdId}/assets/{assetId}/engines`, with delete restricted to Owners.

#### Scenario: Listing engines
- **WHEN** any household member views an asset's detail page
- **THEN** the app lists its engines with label, type, fuel type, and status

#### Scenario: Adding an engine
- **WHEN** a Contributor/Owner submits a new engine's details
- **THEN** the app creates it and it appears in the asset's engine list

#### Scenario: Deleting an engine restricted to Owners
- **WHEN** a Contributor (non-Owner) views the engine list
- **THEN** the app does not show a delete control for any engine

### Requirement: License plate surfaced on asset detail
The frontend SHALL display an asset's `licensePlate`, when populated, prominently in the asset detail header area (not only within the type-specific field list), so the plate is readable without scanning detail fields. The plate SHALL be edited through the existing type-adaptive asset form, where it appears automatically for categories whose registry `applicableFields` include `licensePlate`.

#### Scenario: Plate visible at the top of asset detail
- **WHEN** a member opens the detail page of a Car with `licensePlate: "ABC-1234"`
- **THEN** the plate is shown in the header area of the page

#### Scenario: No plate, no placeholder
- **WHEN** a member opens the detail page of an asset with no `licensePlate` (unset, or a category where it does not apply)
- **THEN** the header shows no plate element

#### Scenario: Plate editable via the asset form
- **WHEN** a Contributor edits a UtilityTrailer and the registry lists `licensePlate` as applicable
- **THEN** the asset form renders a "License plate" input and submits its value like any other type-specific field

### Requirement: Asset description uses the shared markdown editor
The asset create/edit form's `description` field SHALL render using the shared `MarkdownEditor` component instead of a plain textarea. Asset detail views displaying the description SHALL render it through the shared read-only markdown renderer instead of as raw text.

#### Scenario: Editing an asset's description
- **WHEN** a Contributor/Owner opens the asset edit form for an asset with an existing markdown-formatted `description`
- **THEN** the `MarkdownEditor` loads that value in its WYSIWYG form for further editing

#### Scenario: Viewing a formatted description
- **WHEN** an asset's `description` contains markdown formatting (e.g. a heading and a list) and its detail page is viewed
- **THEN** the description renders as formatted markdown, not raw markdown syntax

