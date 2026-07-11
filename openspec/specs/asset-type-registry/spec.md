# asset-type-registry Specification

## Purpose
TBD - created by syncing change consolidate-asset-types. Update Purpose after archive.

## Requirements
### Requirement: Backend-owned asset type registry
The system SHALL define a static asset type registry in the Application layer containing exactly one entry per `AssetCategory` enum value. Each entry SHALL define: `category`, `group` (Road | Powersport | Water | Trailer | Equipment), `structuralType` (Vehicle | Boat | Trailer | Equipment), `displayLabel`, `defaultUsageTrackingMode`, `typicallyHasEngine` (bool), `vinDecodeSupport` (None | BestEffort | Supported), `typicalPermitKinds` (list of strings), `applicableFields` (list of camelCase type-specific field names valid for the category), and `icon` (kebab-case lucide icon name, e.g. `"car"`, `"sailboat"`). The registry SHALL NOT serve colors or other theme-dependent presentation values.

#### Scenario: Registry covers every category
- **WHEN** the registry completeness unit test enumerates all `AssetCategory` enum values
- **THEN** each value has exactly one registry entry, and the registry contains no entries for values not in the enum

#### Scenario: Applicable fields match the structural type
- **WHEN** a registry entry declares `structuralType: Vehicle`
- **THEN** its `applicableFields` contain only fields that exist on the `Vehicle` structural class (e.g. `vin`, `make`, `model`, `color`, `trackLengthIn`)

#### Scenario: Every entry names an icon
- **WHEN** the registry completeness unit test inspects entries
- **THEN** every entry has a non-empty `icon` value and no entry carries a color

---

### Requirement: Asset types endpoint
The system SHALL provide `GET /api/asset-types` returning all registry entries. The endpoint SHALL allow anonymous access (the payload is static, non-sensitive product metadata) and its response DTOs SHALL appear in the OpenAPI document so the generated frontend types include them.

#### Scenario: All entries returned
- **WHEN** any caller (authenticated or not) sends `GET /api/asset-types`
- **THEN** HTTP 200 is returned with one entry per `AssetCategory` value, each including category, group, structural type, display label, default usage tracking mode, typically-has-engine, VIN decode support, typical permit kinds, applicable fields, and icon name

#### Scenario: Registry types available to the frontend
- **WHEN** `npm run generate:api` is executed against the running API
- **THEN** `schema.d.ts` contains the asset-type registry response types
