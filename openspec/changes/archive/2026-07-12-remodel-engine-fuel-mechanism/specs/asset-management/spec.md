## ADDED Requirements

### Requirement: Derived powertrain summary on asset responses
`AssetResponse` (and any list/summary projection used for asset cards or dashboard display) SHALL include a derived, non-persisted `powertrain` field computed from the asset's `Active`-status engines only: `null`/absent when the asset has only `Ice` engines or no engines, `"Electric"` when it has only `Electric` engines, `"Hybrid"` when it has at least one `Active` `Ice` engine and at least one `Active` `Electric` engine with `IsExternallyChargeable = false`, and `"Plug-in Hybrid"` when it has at least one `Active` `Ice` engine and at least one `Active` `Electric` engine with `IsExternallyChargeable = true`. This value SHALL NOT be stored on the `Asset` entity; it is computed at read time from the asset's current engines.

#### Scenario: Ice-only asset has no powertrain badge
- **WHEN** an asset has a single `Active` `Ice` engine and its `AssetResponse` is fetched
- **THEN** `powertrain` is absent or `null`

#### Scenario: Pure EV asset shows Electric
- **WHEN** an asset has a single `Active` `Electric` engine and its `AssetResponse` is fetched
- **THEN** `powertrain` is `"Electric"`

#### Scenario: Conventional hybrid shows Hybrid
- **WHEN** an asset has an `Active` `Ice` engine and an `Active` `Electric` engine with `IsExternallyChargeable = false`, and its `AssetResponse` is fetched
- **THEN** `powertrain` is `"Hybrid"`

#### Scenario: Plug-in hybrid shows Plug-in Hybrid
- **WHEN** an asset has an `Active` `Ice` engine and an `Active` `Electric` engine with `IsExternallyChargeable = true`, and its `AssetResponse` is fetched
- **THEN** `powertrain` is `"Plug-in Hybrid"`

#### Scenario: Retired engine does not count toward the powertrain badge
- **WHEN** an asset has an `Active` `Ice` engine and a `Retired` `Electric` engine (e.g. a discontinued EV conversion), and its `AssetResponse` is fetched
- **THEN** `powertrain` is absent or `null` (the retired electric engine is excluded)
