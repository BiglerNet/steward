## ADDED Requirements

### Requirement: Asset type registry consumption
The frontend SHALL fetch the asset type registry from `GET /api/asset-types` via a `useAssetTypeRegistry()` TanStack Query hook configured to fetch once per session (`staleTime: Infinity`), and SHALL use it as the single source of truth for category display labels, grouping, applicable type-specific fields, icon colors, and default usage tracking modes. The hardcoded `assetTypeFieldConfig.ts` module SHALL be removed. Views that depend on the registry SHALL show a loading state until it resolves and a retryable error state if the fetch fails.

#### Scenario: Registry fetched once per session
- **WHEN** a user navigates between the asset list, detail, and form views repeatedly in one session
- **THEN** the asset-types endpoint is called at most once

#### Scenario: Registry drives field applicability
- **WHEN** the registry entry for `Boat` lists `hin`, `hullMaterial`, `lengthFt`, `beamFt`, `make`, `model`, `color` as applicable fields
- **THEN** the asset form for a Boat renders exactly those type-specific inputs, without any frontend-hardcoded per-category field list

## MODIFIED Requirements

### Requirement: Asset list
The frontend SHALL list a household's assets via `GET /api/households/{householdId}/assets`, optionally filtered by category, showing name, category display label (from the registry), year, and photo when present. Category icons/colors SHALL come from the registry's `iconColor`.

#### Scenario: Viewing assets
- **WHEN** a household member opens `/households/:householdId/assets`
- **THEN** the app lists all assets in that household with registry-provided display labels

#### Scenario: Filtering by category
- **WHEN** a user selects a category filter
- **THEN** the list shows only assets of that category

#### Scenario: No assets yet
- **WHEN** a household has zero assets
- **THEN** the app shows an empty state prompting asset creation

---

### Requirement: Type-adaptive asset create/edit form
The frontend SHALL provide a create/edit form for assets whose visible type-specific fields adapt to the selected `AssetCategory` as defined by the registry's `applicableFields`, submitting to `POST /api/households/{householdId}/assets` or `PUT /api/households/{householdId}/assets/{assetId}`. The category selector SHALL present categories grouped by their registry `group` (Road, Powersport, Water, Trailer, Equipment). On create, selecting a category SHALL prefill `usageTrackingMode` with the registry's default while leaving it editable. On edit, the category SHALL not be changeable.

#### Scenario: Creating a Car
- **WHEN** a Contributor/Owner selects `Car` as the category and fills in the vehicle-specific fields (VIN, make, model, color)
- **THEN** the app submits a `CreateAssetRequest` with `category: "Car"` and those fields populated, with inapplicable type-specific fields omitted or null

#### Scenario: Category selection prefills usage tracking
- **WHEN** a user selects `Car` in the create form
- **THEN** the usage tracking field changes to the registry default for Car (e.g. `Mileage`) and remains editable

#### Scenario: Categories presented in groups
- **WHEN** a user opens the category selector
- **THEN** categories appear grouped under their registry group headings (e.g. Utv and Snowmobile under Powersport)

#### Scenario: Changing category mid-create
- **WHEN** a user changes the selected category while creating an asset
- **THEN** the form clears field values that are not applicable to the newly selected category before submission

#### Scenario: Viewer cannot create or edit
- **WHEN** a Viewer-role user opens the asset list
- **THEN** the app hides create/edit controls

---

### Requirement: Asset detail view
The frontend SHALL show an asset's full details, including its registry display label and the type-specific fields applicable to its category, via `GET /api/households/{householdId}/assets/{assetId}`.

#### Scenario: Viewing an asset
- **WHEN** any household member opens an asset's detail page
- **THEN** the app shows its name, category display label, and all populated type-specific fields
