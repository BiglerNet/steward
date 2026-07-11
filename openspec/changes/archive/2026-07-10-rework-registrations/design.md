# Design: rework-registrations

## Context

Registration records are flat per-renewal rows (`Registration`: number, issuing authority, renewed-on, cost, expires-on, document, notes) attached to an asset. Pain points settled during exploration: the plate number lives only inside registration history (re-entered every renewal, not visible on the asset), trail passes and permits have no representation despite very different validity windows, issuing authority is unassisted free text, and users get no hint that e.g. a UTV typically needs a trail pass.

The prior change (`consolidate-asset-types`) established the patterns this change reuses: structural fields on the four TPH classes gated by registry `applicableFields`, backend-owned static registries served via anonymous endpoints, and session-cached registry fetches on the frontend. The asset type registry already carries `typicalPermitKinds` per category. Migrations can be reset freely (pre-launch).

## Goals / Non-Goals

**Goals**
- Track registrations, trail passes, and permits as distinct kinds of the same flat record (Option A from exploration — no credential/renewal aggregate).
- Surface the license plate on the asset itself; stop re-entering identifiers per renewal.
- Household country/region (US + CA) driving issuing-authority suggestions.
- Registry-driven "typically needs a trail pass" nudges.

**Non-Goals**
- No notifications/reminders (existing due-soon badges stay as-is).
- No issuing-authority normalization or FK — plain string forever until proven otherwise.
- No credential/renewal parent-child modeling; renewals stay independent rows.
- No wizard or photo work (later changes).

## Decisions

### D1: `LicensePlate` on `Vehicle` and `Trailer`, gated by registry `applicableFields`
Add nullable `LicensePlate` to the `Vehicle` and `Trailer` structural classes (not `Asset`, not `Boat`). Applicability per category comes from the registry, exactly like `vin` or `ballSizeIn`: initially the Road group (Car, Truck, Suv, Van, Motorcycle) plus all four trailer categories. Boats keep their hull/bow numbers in registration records (`registrationNumber`) — a plate label would be wrong there. Powersport categories (Utv, Atv, Snowmobile, …) are omitted initially; adding one later is a one-line registry edit, no schema change.

*Alternative considered*: `LicensePlate` on `Asset` with applicability rules — rejected because Equipment can never have one and the structural-class convention ("classes exist only where columns differ") already handles this.

### D2: `RegistrationKind` enum, required on create/update, editable
New Domain enum `RegistrationKind { Registration, TrailPass, Permit }`, stored as int (repo convention), serialized as string. `kind` is required on create and update (no default — the user must say what the record is) and editable like any other field, since a wrong kind is just a data-entry mistake. The existing asset-type-registry constants `PermitKindRegistration`/`PermitKindTrailPass` must match enum member names; a unit test asserts every `typicalPermitKinds` string in the registry parses to a `RegistrationKind`.

### D3: `registrationNumber` becomes optional
A day trail pass may have no meaningful number, and the plate now lives on the asset. No per-kind requiredness rules — keep validation flat (max length only). The frontend labels the field per kind ("Registration #", "Pass #", "Permit #").

### D4: `ValidFrom` added; list ordering tolerates missing `expiresOn`
`ValidFrom` (DateOnly, nullable) records when a pass/registration takes effect — needed for day/week passes where `renewedOn` (purchase date) and validity start differ. List ordering becomes `expiresOn` DESC with NULLs last, then `validFrom` DESC NULLs last — current records first, undated records at the bottom.

### D5: "Renew" is a frontend prefill, not an endpoint
The Renew button on a record opens the existing create form prefilled from that record with `renewedOn`, `validFrom`, `expiresOn`, and `cost` cleared. No `POST .../renew` clone endpoint: the server would add API surface to do what the client can do with data it already has, and the user usually edits dates before saving anyway.

### D6: Backend-owned region registry at `GET /api/regions`
Static `RegionRegistry` in `Steward.Application/Regions` (mirroring `AssetTypeRegistry`): two countries (`US`, `CA`) each with ISO 3166-2 regions (50 states + DC for US; 13 provinces/territories for CA), each entry `{ code, name }` (e.g. `{ "US-WI", "Wisconsin" }`). Served by an anonymous `RegionsController` (`api/regions`, no version URL segment — repo convention), DTOs in the OpenAPI doc, frontend hook `useRegionRegistry` with `staleTime: Infinity` like `useAssetTypeRegistry`.

*Alternative considered*: hardcode the list in the frontend — rejected because the backend needs the same list to validate `Household.Country/Region`, and two copies drift.

### D7: `Household.Country` + `Region` as nullable ISO code strings
`Country` holds ISO 3166-1 alpha-2 (`"US"`, `"CA"`), `Region` holds full ISO 3166-2 (`"US-WI"`). Both optional; validation (FluentValidation against `RegionRegistry`): `region` requires `country` and must belong to it; unknown codes → 400. Included in household create/update/get contracts; Owner-only to change (same rule as other household fields). No backfill concerns (pre-launch).

### D8: Issuing authority stays free text with region-seeded suggestions
The registration form's issuing-authority input becomes a combobox (shadcn Command-in-Popover pattern): suggestions are the region *names* from the registry — household's country's regions first with the household's own region floated to the top, then the other country's. Free typing always allowed; the stored value is whatever string the user submits. No schema change to `IssuingAuthority`.

### D9: Permit nudges are computed client-side
On the registrations tab: for each kind named in the asset's registry `typicalPermitKinds`, if the asset has no record of that kind whose `expiresOn` is today-or-later (records with no `expiresOn` count as current), render a dismissable-by-ignoring inline hint ("Snowmobiles usually need a trail pass — none current."). Pure derivation from data already fetched (registry + registration list); no backend involvement, no persistence of dismissals.

## Risks / Trade-offs

- [Registry permit-kind strings and `RegistrationKind` enum drift] → unit test asserting every `typicalPermitKinds` value parses to a `RegistrationKind` member.
- [Plate on asset + number on registration records can disagree] → accepted; registration rows are historical snapshots, the asset field is current state. UI labels distinguish them ("License plate" vs. per-kind number).
- [Optional `registrationNumber` allows fully-empty-looking rows] → kind + dates still identify the row; validators still enforce max lengths and sane dates; not worth per-kind requiredness complexity.
- [US/CA-only regions bake an assumption into household validation] → registry is additive; new countries are data, not schema.

## Migration Plan

Pre-launch: delete `src/Steward.Infrastructure/Migrations/`, regenerate a single `InitialCreate` including the new columns (`Assets.LicensePlate`, `Registrations.Kind`/`ValidFrom`, nullable `RegistrationNumber`, `Households.Country`/`Region`), apply to a clean database. Regenerate `schema.d.ts` after backend contracts change.

## Open Questions

None — decisions above were settled in the 2026-07-09 exploration session.
