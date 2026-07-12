## MODIFIED Requirements

### Requirement: Creation on advancing past the last data step
The wizard SHALL create the asset via the existing `POST /api/households/{householdId}/assets` when the user advances past the Engine step (or the Details step when the Engine step is not shown or is skipped), followed by one `POST .../assets/{assetId}/engines` call when a single engine's data was entered, or two sequential `POST .../assets/{assetId}/engines` calls when the Engine step's engine-type choice was `Hybrid` or `Plug-in Hybrid`. If any engine call fails after the asset is created, the wizard SHALL surface the error on the Engine step with retry and skip options while keeping the created asset and any engine(s) already successfully created.

#### Scenario: Asset and engine created together
- **WHEN** a user completes Details and Engine and advances with engine type `Ice`
- **THEN** the asset is created first and one engine is created against the new asset id

#### Scenario: Hybrid selection creates two engines
- **WHEN** a user completes Details and Engine with engine type `Hybrid` and advances
- **THEN** the asset is created first, followed by two engine records against the new asset id — one `EngineType: "Ice"`, one `EngineType: "Electric"` with `isExternallyChargeable: false`

#### Scenario: Engine failure keeps the asset
- **WHEN** the asset creation succeeds but an engine creation fails
- **THEN** the wizard shows the engine error with retry/skip options and the asset (and any already-created engine) remains

## ADDED Requirements

### Requirement: Engine step engine-type selection
The Engine step SHALL offer four engine-type choices — `Ice`, `Electric`, `Hybrid`, `Plug-in Hybrid` — even though only `Ice` and `Electric` are ever persisted as an `Engine.EngineType` value. Selecting `Ice` or `Electric` SHALL show a single set of engine fields (label, make, model, mechanism/fuel type fields when `Ice`, etc.) resulting in one engine created. Selecting `Hybrid` or `Plug-in Hybrid` SHALL show two labeled field groups — one for the gas engine, one for the electric motor — resulting in two engines created together: an `Ice` engine from the first group, and an `Electric` engine from the second group with `isExternallyChargeable` set to `false` for `Hybrid` or `true` for `Plug-in Hybrid`.

#### Scenario: Ice selection shows one field group
- **WHEN** a user selects engine type `Ice` in the Engine step
- **THEN** a single group of engine fields is shown, including `mechanism` and `fuelType` options

#### Scenario: Hybrid selection shows two field groups
- **WHEN** a user selects engine type `Hybrid` in the Engine step
- **THEN** two labeled field groups appear — a gas engine group and an electric motor group — and no `isExternallyChargeable` control is shown to the user (it is implied `false` by the `Hybrid` choice)

#### Scenario: Plug-in Hybrid selection implies external chargeability
- **WHEN** a user selects engine type `Plug-in Hybrid` in the Engine step and completes the wizard
- **THEN** the created electric engine has `isExternallyChargeable: true`

#### Scenario: Switching away from Hybrid clears the second field group
- **WHEN** a user who selected `Hybrid` and filled both field groups switches the engine-type choice to `Ice`
- **THEN** the electric motor field group is hidden and its entered values are discarded
