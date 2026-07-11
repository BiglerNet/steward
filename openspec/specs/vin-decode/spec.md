# vin-decode Specification

## Purpose
TBD - created by archiving change asset-creation-wizard. Update Purpose after archive.

## Requirements
### Requirement: VIN decode proxy endpoint
The system SHALL provide `GET /api/vin-decode/{vin}` (any authenticated user, no household scoping) that decodes the VIN via the NHTSA vPIC `DecodeVinValues` API and returns a normalized result: `vin`, `make`, `model`, `modelYear`, `bodyClass`, `vehicleType`, `fuelTypePrimary`, `engineCylinders`, `displacementLiters` — all fields nullable except `vin`. The VIN SHALL be validated as exactly 17 alphanumeric characters excluding I, O, and Q; anything else SHALL return HTTP 400. Empty-string or unparseable values from vPIC SHALL map to `null` rather than errors, and a VIN vPIC cannot decode SHALL return HTTP 200 with null fields.

#### Scenario: Successful decode
- **WHEN** an authenticated user requests decode for a valid VIN known to vPIC
- **THEN** HTTP 200 is returned with the decoded fields populated and unknown fields null

#### Scenario: Malformed VIN rejected
- **WHEN** a user requests decode for a 12-character string or one containing the letter O
- **THEN** HTTP 400 is returned without calling the upstream API

#### Scenario: Undecodable VIN is not an error
- **WHEN** vPIC returns no meaningful data for a well-formed VIN
- **THEN** HTTP 200 is returned with null fields

#### Scenario: Anonymous request rejected
- **WHEN** an unauthenticated caller requests a decode
- **THEN** HTTP 401 is returned

### Requirement: Upstream failure isolation
The system SHALL call vPIC with a bounded timeout (approximately 8 seconds) and SHALL return HTTP 502 with Problem Details when the upstream call fails or times out. Upstream failures SHALL NOT be cached or retried server-side.

#### Scenario: vPIC unavailable
- **WHEN** the vPIC API is unreachable or exceeds the timeout
- **THEN** HTTP 502 is returned promptly and no partial result is fabricated
