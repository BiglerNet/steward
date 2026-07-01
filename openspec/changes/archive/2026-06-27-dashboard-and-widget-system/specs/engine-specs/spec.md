## ADDED Requirements

### Requirement: Engine carries extended specification fields
The `Engine` entity SHALL support six additional optional specification fields: `HorsepowerHp` (decimal?, SAE horsepower), `TorqueNm` (decimal?, Newton-metres), `OilCapacityL` (decimal?, litres), `RecommendedOilType` (string?), `CoolantCapacityL` (decimal?, litres), and `RecommendedOctane` (int?). All fields are optional and nullable. These fields SHALL be included in `EngineResponse`, `CreateEngineRequest`, and `UpdateEngineRequest`. The create and update endpoints SHALL accept and persist these values when provided.

#### Scenario: Contributor records oil capacity and type when creating an engine
- **WHEN** a Contributor POSTs to the create engine endpoint with `oilCapacityL: 4.7` and `recommendedOilType: "5W-30 Full Synthetic"`
- **THEN** HTTP 201 is returned with an `EngineResponse` reflecting those values

#### Scenario: Fields are optional — omitting them is valid
- **WHEN** a Contributor POSTs to the create engine endpoint without any of the six new fields
- **THEN** HTTP 201 is returned with all six new fields as `null` in the `EngineResponse`

#### Scenario: Contributor updates an existing engine to add horsepower and torque
- **WHEN** a Contributor PUTs the update engine endpoint with `horsepowerHp: 355` and `torqueNm: 475`
- **THEN** HTTP 200 is returned and the `EngineResponse` reflects the new values

#### Scenario: Recommended octane accepts standard values only
- **WHEN** a Contributor submits `recommendedOctane: 94` (not a standard value)
- **THEN** HTTP 400 is returned

---

### Requirement: Engine status includes a Broken state
The `EngineStatus` enum SHALL have three values: `Active`, `Retired`, and `Broken`. `Active` means the engine is installed and operational. `Broken` means the engine is installed but non-functional. `Retired` means the engine has been removed from service. The `EngineResponse` SHALL expose the `status` field reflecting the current state.

#### Scenario: A newly created engine defaults to Active
- **WHEN** a Contributor creates a new engine
- **THEN** the returned `EngineResponse` has `status: "Active"`

#### Scenario: An engine can be marked broken
- **WHEN** a Contributor POSTs to `POST .../engines/{engineId}/mark-broken` for an `Active` engine
- **THEN** HTTP 200 is returned and `status` becomes `Broken`

#### Scenario: A broken engine can be reactivated
- **WHEN** a Contributor POSTs to `.../engines/{engineId}/reactivate` for a `Broken` engine
- **THEN** HTTP 200 is returned and `status` becomes `Active`

#### Scenario: A broken engine can be retired
- **WHEN** a Contributor POSTs to `.../engines/{engineId}/retire` for a `Broken` engine
- **THEN** HTTP 200 is returned and `status` becomes `Retired`

#### Scenario: Marking an already-broken engine broken is rejected
- **WHEN** a Contributor POSTs to `.../engines/{engineId}/mark-broken` for a `Broken` engine
- **THEN** HTTP 400 is returned

#### Scenario: Marking a retired engine broken is rejected
- **WHEN** a Contributor POSTs to `.../engines/{engineId}/mark-broken` for a `Retired` engine
- **THEN** HTTP 400 is returned

---

### Requirement: Garage Logic aggregate metrics computed from active engine specs
The system SHALL expose four household-level aggregate metrics as part of the dashboard snapshot: Cylinder Index, Total Displacement, Total Horsepower, and Total Torque. All four metrics are computed only from engines with `status = Active`. The Cylinder Index additionally filters to `engineType = Ice` (internal combustion only).

- **Cylinder Index**: `SUM(cylinders)` across Active ICE engines where `cylinders` is not null
- **Total Displacement**: `SUM(displacementCc)` across Active engines where `displacementCc` is not null
- **Total Horsepower**: `SUM(horsepowerHp)` across Active engines where `horsepowerHp` is not null
- **Total Torque**: `SUM(torqueNm)` across Active engines where `torqueNm` is not null

`Broken` and `Retired` engines do NOT contribute to any metric, regardless of their spec values.

#### Scenario: Cylinder Index counts only active ICE engines
- **WHEN** a household has a 4-cyl Active ICE engine, a 4-cyl Broken ICE engine, and a 2-cyl Active Electric engine, and the CylinderIndex widget is in the dashboard
- **THEN** the snapshot returns `totalCylinders: 4` (broken engine excluded; electric engine excluded)

#### Scenario: Total Horsepower includes active electric engines
- **WHEN** a household has an Active ICE engine with `horsepowerHp: 355` and an Active Electric engine with `horsepowerHp: 200`, and the TotalHorsepower widget is in the dashboard
- **THEN** the snapshot returns `totalHp: 555`

#### Scenario: A retired engine with specs does not contribute
- **WHEN** a household has one Active engine (`cylinders: 4`) and one Retired engine (`cylinders: 8`), and the CylinderIndex widget is in the dashboard
- **THEN** the snapshot returns `totalCylinders: 4`

#### Scenario: No active engines yields zero for all metrics
- **WHEN** a household has no Active engines
- **THEN** the snapshot returns `0` (or `null`) for all four Garage Logic metrics
