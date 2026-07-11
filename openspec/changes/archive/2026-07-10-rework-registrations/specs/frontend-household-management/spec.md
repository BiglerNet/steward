## ADDED Requirements

### Requirement: Household location settings
The frontend SHALL let Owners set or clear the household's `country` and `region` on the household settings page via `PUT /api/households/{id}`. The selectors SHALL be populated from `GET /api/regions` through a `useRegionRegistry()` TanStack Query hook fetched once per session (`staleTime: Infinity`, mirroring the asset-type registry hook); the region selector SHALL list only regions of the selected country and SHALL clear when the country changes. Non-Owner roles SHALL see the location read-only or hidden, consistent with the API's authorization.

#### Scenario: Owner sets the household location
- **WHEN** an Owner selects "United States" and "Wisconsin" on the settings page and saves
- **THEN** the app submits `country: "US"`, `region: "US-WI"` via `PUT /api/households/{id}` and reflects the saved location

#### Scenario: Changing country resets region
- **WHEN** an Owner switches the country from "United States" to "Canada"
- **THEN** the region selector clears and offers only Canadian provinces and territories

#### Scenario: Region registry fetched once per session
- **WHEN** a user visits household settings and registration forms repeatedly in one session
- **THEN** `GET /api/regions` is called at most once
