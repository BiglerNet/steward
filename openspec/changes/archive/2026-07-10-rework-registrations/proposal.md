# Proposal: rework-registrations

## Why

Registration tracking today conflates three distinct real-world documents (vehicle registrations, trail passes, and other permits) into one undifferentiated record, forces re-entry of the plate/registration number on every renewal, and buries the license plate — the single most-looked-up identifier for a road vehicle — inside the registration history instead of showing it on the asset itself. Users also get no help entering issuing authorities (typically a US state or Canadian province) and no nudge when an asset category that typically needs a trail pass has none on file.

## What Changes

- **License plate hoisted onto the asset**: `Vehicle` and `Trailer` gain a nullable `LicensePlate` field (registry-driven applicability, like other type-specific fields), shown top-level on the asset detail page. Registration records stop being the home of the plate number.
- **Registration record kinds**: `Registration` gains `Kind` (Registration | TrailPass | Permit) and `ValidFrom` (DateOnly, nullable), so trail passes with varied validity windows (day/week/month/year/multi-year) can be tracked as first-class entries alongside plate registrations. **BREAKING** (API contract): `registrationNumber` becomes optional (short-lived passes may not have a meaningful number); `kind` becomes a required field on create/update; responses gain `kind` and `validFrom`. DB migrations are reset freely (product is pre-launch).
- **Renew without re-entry**: a "Renew" action on a registration record opens the create form prefilled from that record with dates and cost cleared (frontend behavior; no new endpoint).
- **Household location**: `Household` gains nullable `Country` + `Region` (ISO 3166, US + Canada only for now), editable by Owners in household settings.
- **Issuing-authority assistance**: a backend-owned region registry (US states + Canadian provinces/territories) served via an anonymous config endpoint powers an issuing-authority combobox — household region floated to top, always free-text (`IssuingAuthority` stays a plain string, no FK).
- **Permit nudges**: on an asset's registrations tab, if the asset type registry lists a `typicalPermitKinds` entry with no current record of that kind, the frontend shows a non-blocking hint (e.g. "UTVs usually need a trail pass — none on file").

## Capabilities

### New Capabilities

- `region-registry`: backend-owned static reference data for supported countries (US, CA) and their ISO 3166-2 regions, served via an anonymous `GET /api/regions` endpoint mirroring the asset-type-registry pattern.

### Modified Capabilities

- `domain-model`: `Registration` gains `Kind` + `ValidFrom` and `RegistrationNumber` becomes nullable; `Vehicle` and `Trailer` gain `LicensePlate`; migrations reset to a fresh `InitialCreate`.
- `household-multitenancy`: the `Household` entity gains nullable `Country` + `Region`.
- `registration-tracking`: create/update/list contracts gain `kind` (required) and `validFrom`; `registrationNumber` becomes optional; list ordering accounts for records without `expiresOn`.
- `frontend-registration-tracking`: kind selection and badges, Renew-prefill action, permit-kind nudges, issuing-authority combobox seeded from the region registry.
- `frontend-asset-management`: license plate shown prominently on asset detail (the plate reaches asset DTOs and the type-adaptive form through the existing registry `applicableFields` mechanism — no `asset-management` requirement change).
- `household-management`: household create/update/get contracts gain optional `country` + `region`, validated against the region registry.
- `frontend-household-management`: household settings gains country/region selectors (Owner-editable).

## Impact

- **Backend**: `Steward.Domain` (Registration, Household, Vehicle, Trailer entities + new `RegistrationKind` enum), `Steward.Application` (Registrations + Households DTOs/validators, new `Regions` registry area), `Steward.Infrastructure` (services, EF configurations, regenerated `InitialCreate` migration), `Steward.Api` (new `RegionsController`, updated request contracts).
- **Frontend**: registration list/form components, asset detail/form, household settings, new region-registry hook (session-cached like the asset-type registry), regenerated `schema.d.ts`.
- **Data**: existing migrations nuked and regenerated — no data migration (pre-launch).
- **Not in scope**: notifications/reminders for expiring records; issuing-authority normalization (stays free text); the asset-creation wizard and photos (later changes).
