# Tasks: asset-creation-wizard

## 1. VIN decode backend

- [x] 1.1 Application: `VinDecode/IVinDecodeService.cs` + `VinDecodeResult` DTO (all fields nullable except `vin`)
- [x] 1.2 Infrastructure: `VpicVinDecodeService` with typed `HttpClient` (vPIC `DecodeVinValues`, ~8s timeout, empty strings → null, defensive numeric parsing); `AddStewardVinDecode` extension wired into `Program.cs`
- [x] 1.3 Api: `VinDecodeController` — `GET /api/vin-decode/{vin}`, `[Authorize]`, 17-char VIN validation (alphanumeric, no I/O/Q) → 400; upstream failure/timeout → 502 Problem Details
- [x] 1.4 Unit tests: vPIC payload mapping (populated, empty-string, malformed numerics), VIN format validation
- [x] 1.5 Integration tests: 401 anonymous, 400 malformed VIN, 200-with-nulls and 502 via a fake/stubbed upstream handler

## 2. Frontend API layer

- [x] 2.1 Regenerate `schema.d.ts`; add `api/vinDecode.ts` + `hooks/useVinDecode.ts` (mutation-style, no caching)

## 3. Shared field extraction

- [x] 3.1 Extract the registry-driven type-specific inputs + Zod schema fragments from `AssetFormDialog` into a shared `AssetFieldsSection`
- [x] 3.2 Trim `AssetFormDialog` to edit-only (drop category selector and create branches); update its tests

## 4. Wizard UI

- [x] 4.1 Route `/households/:householdId/assets/new` + `AssetCreateWizardPage` shell: hand-rolled stepper header, client-side step state, Viewer redirect, per-step react-hook-form + Zod
- [x] 4.2 Type step: grouped category cards (registry groups/labels/iconColor), usage-mode prefill on select, inapplicable-field reset on category change
- [x] 4.3 VIN step: gated by `vinDecodeSupport`, decode button enabled on valid 17-char VIN, prefill mapping into details/engine state, failure notice, BestEffort caveat, body-class mismatch soft hint (coarse keyword map)
- [x] 4.4 Details step: `AssetFieldsSection` prefilled from decode, "from VIN" affordance
- [x] 4.5 Engine step: gated by `typicallyHasEngine`, skippable, prefilled from decode (`fuelTypePrimary` → `FuelType` lookup)
- [x] 4.6 Creation flow: POST asset on advancing past Engine/Details, then POST engine if entered; engine failure → error with retry/skip, asset kept
- [x] 4.7 Photos step: reuse asset-photos upload machinery against the created asset id; skippable; finish → navigate to asset detail
- [x] 4.8 Point create entry points (asset list button, empty state) at the wizard route

## 5. Frontend tests

- [x] 5.1 Component tests: step gating per category (Car full flow vs trailer short flow), Viewer redirect, type-change field reset
- [x] 5.2 Component tests: decode prefill, decode failure continues manually, mismatch hint, engine-failure retry/skip keeps asset, finish navigation
- [x] 5.3 Edit dialog still green with shared `AssetFieldsSection`; create-entry-point link tests

## 6. Verification

- [x] 6.1 `dotnet build` + `dotnet test`, `npm test`, `tsc -b`, lint, `vite build` all green
- [x] 6.2 Smoke: real VIN decode through the running API, full wizard pass (Car with VIN + engine + photo), trailer short pass, abandon-before-create persists nothing
