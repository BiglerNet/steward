## MODIFIED Requirements

### Requirement: Create engine
The system SHALL provide `POST /api/households/{householdId}/assets/{assetId}/engines` (Contributor or Owner only) accepting `{ label, make, model, serialNumber, year, engineType, mechanism, fuelType, isExternallyChargeable, twoStrokeOilDelivery, twoStrokeMixRatio, cylinders, displacementCc, installedDate, installedAtAssetMiles, installedAtAssetHours, horsepowerHp, torqueNm, oilCapacityL, recommendedOilType, coolantCapacityL, recommendedOctane }` with `label` required. `engineType` SHALL be one of `Ice` | `Electric`. `mechanism` and `fuelType` are optional and SHALL be rejected (HTTP 400) unless `engineType = Ice`. `isExternallyChargeable` is optional and SHALL be rejected (HTTP 400) unless `engineType = Electric`. `twoStrokeOilDelivery` and `twoStrokeMixRatio` are optional and SHALL be rejected (HTTP 400) unless `mechanism = TwoStroke`. New engines SHALL default to `Status = Active`. On success it SHALL return HTTP 201 with the created `EngineResponse` including all spec fields.

#### Scenario: Contributor adds an engine to a boat
- **WHEN** a Contributor POSTs to `/api/households/{householdId}/assets/{assetId}/engines` with `label: "Port"` for a twin-engine boat
- **THEN** HTTP 201 is returned with an `EngineResponse` having `status: "Active"` and null spec fields

#### Scenario: Contributor adds a two-stroke gasoline snowmobile engine with oil-injection details
- **WHEN** a Contributor POSTs with `label: "Engine"`, `engineType: "Ice"`, `mechanism: "TwoStroke"`, `fuelType: "Gasoline"`, `twoStrokeOilDelivery: "OilInjected"`, `twoStrokeMixRatio: "50:1"`
- **THEN** HTTP 201 is returned with an `EngineResponse` reflecting all provided values

#### Scenario: Contributor adds an externally-chargeable electric motor
- **WHEN** a Contributor POSTs with `label: "Electric motor"`, `engineType: "Electric"`, `isExternallyChargeable: true`
- **THEN** HTTP 201 is returned with an `EngineResponse` reflecting `isExternallyChargeable: true`

#### Scenario: Mechanism rejected on an Electric engine
- **WHEN** a Contributor POSTs with `engineType: "Electric"` and `mechanism: "FourStroke"`
- **THEN** HTTP 400 is returned

#### Scenario: FuelType rejected on an Electric engine
- **WHEN** a Contributor POSTs with `engineType: "Electric"` and `fuelType: "Gasoline"`
- **THEN** HTTP 400 is returned

#### Scenario: IsExternallyChargeable rejected on an Ice engine
- **WHEN** a Contributor POSTs with `engineType: "Ice"` and `isExternallyChargeable: true`
- **THEN** HTTP 400 is returned

#### Scenario: Two-stroke oil fields rejected without TwoStroke mechanism
- **WHEN** a Contributor POSTs with `mechanism: "FourStroke"` and `twoStrokeMixRatio: "50:1"`
- **THEN** HTTP 400 is returned

#### Scenario: Unknown engineType value rejected
- **WHEN** a Contributor POSTs with `engineType: "Hybrid"`
- **THEN** HTTP 400 is returned

#### Scenario: Viewer cannot add an engine
- **WHEN** a user with `Role = Viewer` POSTs to the create engine endpoint
- **THEN** HTTP 403 is returned

#### Scenario: Missing label rejected
- **WHEN** a create engine request omits `label`
- **THEN** HTTP 400 is returned

#### Scenario: Asset belongs to a different household
- **WHEN** `assetId` does not belong to `householdId` in the route
- **THEN** HTTP 404 is returned

### Requirement: Update engine
The system SHALL provide `PUT /api/households/{householdId}/assets/{assetId}/engines/{engineId}` (Contributor or Owner only) accepting the same fields as create, excluding `status` (changed only via the dedicated status-transition endpoints). The same field-applicability validation as create (`mechanism`/`fuelType` require `engineType = Ice`; `isExternallyChargeable` requires `engineType = Electric`; `twoStrokeOilDelivery`/`twoStrokeMixRatio` require `mechanism = TwoStroke`) SHALL apply. On success it SHALL return HTTP 200 with the updated `EngineResponse`.

#### Scenario: Contributor updates engine serial number
- **WHEN** a Contributor PUTs the engine endpoint with a corrected `serialNumber`
- **THEN** HTTP 200 is returned with the updated `EngineResponse`

#### Scenario: Contributor adds spec data to an existing engine
- **WHEN** a Contributor PUTs with `horsepowerHp: 200` and `torqueNm: 320` on an engine that previously had null values for those fields
- **THEN** HTTP 200 is returned with the spec values updated in the `EngineResponse`

#### Scenario: Changing mechanism away from TwoStroke clears incompatible fields
- **WHEN** a Contributor PUTs an engine that currently has `mechanism: "TwoStroke"`, `twoStrokeMixRatio: "50:1"` with a new `mechanism: "FourStroke"` and no `twoStrokeMixRatio`
- **THEN** HTTP 200 is returned and the updated `EngineResponse` has `twoStrokeMixRatio: null`

#### Scenario: Viewer cannot update an engine
- **WHEN** a user with `Role = Viewer` PUTs the update engine endpoint
- **THEN** HTTP 403 is returned
