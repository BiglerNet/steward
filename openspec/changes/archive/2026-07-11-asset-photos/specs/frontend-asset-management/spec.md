## MODIFIED Requirements

### Requirement: Asset list
The frontend SHALL list a household's assets via `GET /api/households/{householdId}/assets`, optionally filtered by category, showing name, category display label (from the registry), year, and the cover photo thumbnail when the asset has one (`coverPhotoId` non-null), fetched with the authenticated client and rendered via object URLs. Assets without a cover photo SHALL fall back to the registry icon treatment. Category icons/colors SHALL come from the registry's `iconColor`.

#### Scenario: Viewing assets
- **WHEN** a household member opens `/households/:householdId/assets`
- **THEN** the app lists all assets in that household with registry-provided display labels

#### Scenario: Cover thumbnail on the card
- **WHEN** an asset has a `coverPhotoId`
- **THEN** its card shows that photo's thumbnail variant, while assets without one keep the icon fallback

#### Scenario: Filtering by category
- **WHEN** a user selects a category filter
- **THEN** the list shows only assets of that category

#### Scenario: No assets yet
- **WHEN** a household has zero assets
- **THEN** the app shows an empty state prompting asset creation
