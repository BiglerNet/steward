# Design: asset-creation-wizard

## Context

Asset create and edit currently share `AssetFormDialog` (a single registry-driven dialog). The asset-type registry already exposes everything the wizard needs to gate steps: `group` for the type cards, `vinDecodeSupport` (None | BestEffort | Supported), `typicallyHasEngine`, and `applicableFields`. The asset-photos change shipped the photo endpoints the final step composes. This change adds one new backend surface (a VIN-decode proxy) and reorganizes the frontend create path; everything else is composition of existing endpoints.

## Goals / Non-Goals

**Goals**
- Guided, full-page create flow that produces an asset, optionally an engine, and optionally photos in one pass.
- VIN decode as an accelerator: prefills fields, never gates progress.
- Keep the registry as the single source of truth for step gating and field applicability.

**Non-Goals**
- No edit wizard — the dialog remains for edit.
- No HIN decode for boats (vPIC is VIN-only; boats stay `vinDecodeSupport: None` unless the registry says otherwise).
- No server-side draft persistence — abandoning the wizard before creation persists nothing.
- No caching or storage of decode results.

## Decisions

### D1: VIN decode is a thin authenticated proxy — `GET /api/vin-decode/{vin}`

- **Application**: `IVinDecodeService` + `VinDecodeResult` DTO (`vin`, `make`, `model`, `modelYear`, `bodyClass`, `vehicleType`, `fuelTypePrimary`, `engineCylinders`, `displacementLiters` — all nullable except `vin`).
- **Infrastructure**: `VpicVinDecodeService` using a typed `HttpClient` (`https://vpic.nhtsa.dot.gov/api/vehicles/DecodeVinValues/{vin}?format=json`, ~8s timeout), registered via a new `AddStewardVinDecode` extension. vPIC returns empty strings for unknown fields — map those to `null`; parse numerics defensively (bad number → null, not an error).
- **Api**: `VinDecodeController`, plain `[Authorize]` — no household scoping, since a VIN decode touches no household resource. VIN validated as exactly 17 alphanumeric characters excluding I/O/Q → 400 otherwise. Upstream failure or timeout → 502 Problem Details; a VIN that decodes to nothing useful → 200 with null fields (the frontend treats both as "continue manually").
- **Why proxy instead of calling vPIC from the browser**: keeps the third-party dependency and its quirks (string-typed payload, availability) server-side, avoids CORS, and gives one place to swap providers later. No API key or rate-limit handling needed — vPIC is a free public API.

### D2: Wizard is a full-page route with client-side step state

`/households/:householdId/assets/new`, guarded like other write surfaces (Viewer never sees the entry points and is redirected if they navigate directly). Step order: **Type → VIN → Details → Engine → Photos**, with VIN and Engine conditionally present:

- VIN step appears only when the selected category's `vinDecodeSupport != None`. `BestEffort` categories show a caveat that decode data may be sparse.
- Engine step appears only when `typicallyHasEngine` is true; it is skippable, and engines remain manageable from the detail page as today.

Step state lives in one client-side object (react-hook-form per step, Zod validation, accumulated into wizard state). No per-step URLs — refresh/back-out before creation simply discards the draft. Changing the category after the Type step resets fields that are no longer applicable (same rule the dialog already implements).

### D3: The asset is created when the user advances past the last data step

"Create" fires when leaving the Engine step (or Details, when the Engine step is hidden or skipped): first `POST .../assets`, then `POST .../assets/{id}/engines` if engine data was entered. If the engine call fails, the asset already exists — the wizard surfaces the error on the Engine step with retry/skip options rather than pretending to roll back. The Photos step therefore always operates on a real `assetId` and reuses the existing `PhotosSection` upload machinery. Finishing — or skipping photos — navigates to the new asset's detail page. Abandoning during the Photos step leaves a valid asset with no photos, which is an acceptable end state, not an error.

### D4: Decode prefill is best-effort field mapping with a soft mismatch hint

- Details prefill: `make`, `model`, `modelYear → year`. Prefilled values are ordinary editable form values — no lock, no visual noise beyond a "from VIN" affordance.
- Engine prefill: `engineCylinders → cylinders`, `displacementLiters → displacement`, `fuelTypePrimary` mapped to the `FuelType` enum by a small lookup ("Gasoline" → Gasoline, "Diesel" → Diesel, "Electric" → Electric; anything unrecognized → left unset). Mapping lives in the frontend wizard code — it is presentation-level prefill, not domain logic.
- If the decoded `bodyClass`/`vehicleType` doesn't plausibly match the chosen category (e.g. decode says "Motorcycle", user picked Car), show an informational hint ("Decoded as Motorcycle — double-check the asset type") with no enforcement. The plausibility check is a coarse frontend keyword map and intentionally forgiving.

### D5: The dialog becomes edit-only; shared field components are extracted

`AssetFormDialog` is already ~325 lines; rather than duplicating the registry-driven type-specific inputs into the wizard, extract them into a shared `AssetFieldsSection` (fields + Zod schema fragments keyed by `applicableFields`) consumed by both the edit dialog and the wizard's Details step. Create-specific branches (category selector, usage-mode prefill-on-select) move out of the dialog into the wizard's Type/Details steps. All create entry points (asset list button, empty state) become links to the wizard route.

### D6: Wizard UI is hand-rolled from existing primitives

Stepper header (step labels + progress), grouped category cards, and hint banners are built from existing Tailwind/shadcn primitives already in the repo — no `npx shadcn add` (components.json pulls incompatible Base UI).

## Risks / Trade-offs

- **vPIC availability/latency** → 8s timeout, 502 mapped to a "couldn't decode, continue manually" notice; the flow never blocks on decode.
- **Engine-create failure after asset-create leaves a partial result** → surfaced honestly with retry/skip; the asset is valid on its own, and engines are addable later from detail.
- **Dialog/wizard field drift** → mitigated by extracting `AssetFieldsSection` as the single registry-driven field renderer; component tests cover both consumers.
- **Fuel-type/body-class string mapping is inherently lossy** → unmapped values are simply left blank/unflagged; prefill is an accelerator, not a source of truth.

## Migration Plan

No database or contract changes to existing endpoints; deploy backend (new endpoint is additive) then frontend. Rollback = revert frontend; the decode endpoint is harmless if unused.

## Open Questions

None — decisions above follow the settled 2026-07-09 exploration.
