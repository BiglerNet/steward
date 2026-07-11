## Context

The current domain model uses EF Core TPH with 10 concrete asset classes under three abstract parents. Most leaves add no fields (`Car`, `Truck`, `Utv` are empty; `Snowmobile` adds one column). The class hierarchy is simultaneously the user-facing taxonomy, the field schema, and (implicitly) the behavior model — and the frontend maintains a parallel hardcoded copy (`assetTypeFieldConfig.ts`) of which fields apply to which type, which can drift from the backend.

Three follow-up changes (asset creation wizard + VIN decode, registration rework, asset photos) all need per-type behavior metadata ("does this type typically have an engine?", "is a VIN decodable?", "which permits are typical?"). There is currently no home for that metadata.

The product is not live: EF migrations can be deleted and regenerated freely, and API contract breaks are acceptable.

## Goals / Non-Goals

**Goals:**
- Structural C# classes exist only where the set of columns differs.
- A user-facing `Category` covers today's 10 types plus obvious neighbors, cheap to extend (enum value + registry entry — no class, no mapper case).
- One backend-owned source of truth (the registry) for per-category behavior and field applicability, served to the frontend.
- New-asset `UsageTrackingMode` defaults sensibly per category instead of `None`.

**Non-Goals:**
- The creation wizard, VIN decoding, registration/permit rework, and photo management (separate changes; this change only *stores and serves* the registry fields they will consume).
- Per-household or user-editable type customization — the registry is static application config.
- Removing `PhotoUrl` (happens in the asset-photos change).
- Data migration of existing rows (pre-launch; databases are recreated).

## Decisions

### D1: Four concrete structural classes; behavior lives in data, not inheritance

`Asset` (abstract) → concrete `Vehicle`, `Boat`, `Trailer`, `Equipment`. Leaf and intermediate classes are deleted.

- `Vehicle`: `Vin`, `Make`, `Model`, `Color`, `TrackLengthIn` (hoisted from Snowmobile, nullable)
- `Boat`: `Hin`, `HullMaterial`, `LengthFt`, `BeamFt`, `Make`, `Model`, `Color` — a **sibling** of Vehicle, not a subclass, because VIN semantics don't apply to hulls
- `Trailer`: `BallSizeIn`, `MaxLoadLbs`, `InteriorHeightFt`, `InteriorLengthFt` (merged from both trailer types, all nullable)
- `Equipment`: `CuttingWidthIn`, `MaxPsi`, `MaxGpm`, `EquipmentDescription` (merged, all nullable)

*Alternatives considered:* keeping leaf classes (rejected — empty classes that exist only to name things; every new type costs a class + discriminator mapping + mapper case); intermediate behavior classes like `RoadVehicle`/`PowerSport` (rejected — they would add zero columns; behavior-as-inheritance becomes marker-class soup as golf carts, dirt bikes, etc. arrive). A few nullable columns of "schema slop" on a TPH table (e.g. `TrackLengthIn` on all vehicles) is the accepted cost; applicability is enforced by validation (D4), not by the type system.

`Make`/`Model`/`Color` are duplicated on `Vehicle` and `Boat` rather than pushed to `Asset` — trailers and equipment arguably have makes too, but widening `Asset` is a one-line follow-up if wanted; starting narrow.

### D2: `AssetCategory` enum on `Asset`, registry entry per value

New required `Category` (enum `AssetCategory`) on the `Asset` base. Initial values: `Car`, `Truck`, `Suv`, `Van`, `Motorcycle`, `Utv`, `Atv`, `Snowmobile`, `DirtBike`, `GolfCart`, `Boat`, `Pwc`, `UtilityTrailer`, `EnclosedTrailer`, `SnowmobileTrailer`, `BoatTrailer`, `RidingMower`, `PowerWasher`, `Generator`, `SmallEngine`.

*Enum vs string vs lookup table:* enum wins — it flows through OpenAPI into `schema.d.ts` so the frontend gets the value set for free, and FluentValidation/model binding reject unknown values without custom code. A DB lookup table was rejected: the registry is code-defined config, not user data, and joining for static metadata buys nothing. Category is stored using the same enum-persistence convention the schema already uses for `UsageTrackingMode` etc. Category is immutable after creation (same rule as today's `assetType`).

A unit test asserts a **bijection between `AssetCategory` values and registry entries** so an enum value can never ship without metadata (and vice versa).

### D3: Registry defined in Application, served by a small Api controller

`Steward.Application/AssetTypes/`: `AssetTypeRegistry` (static, pure C# — allowed in Application, no framework deps) defining per category:

| Field | Type | Consumed by |
|---|---|---|
| `Category` | `AssetCategory` | everyone |
| `Group` | enum `AssetGroup`: Road, Powersport, Water, Trailer, Equipment | picker grouping, `?group=` filter |
| `StructuralType` | enum: Vehicle, Boat, Trailer, Equipment | asset factory/mapper, frontend field rendering |
| `DisplayLabel` | string | UI labels (replaces `ASSET_TYPE_LABELS`) |
| `DefaultUsageTrackingMode` | `UsageTrackingMode` | create defaulting (D5), form prefill |
| `TypicallyHasEngine` | bool | wizard change (served now, consumed later) |
| `VinDecodeSupport` | enum: None, BestEffort, Supported | wizard change (served now, consumed later) |
| `TypicalPermitKinds` | list of string (e.g. `Registration`, `TrailPass`) | registration change (served now, consumed later) |
| `ApplicableFields` | list of camelCase field names | backend validation (D4), frontend form |
| `IconColor` | hex string (from `docs/design/tokens.md`) | list/detail icons (replaces `assetTypeIconColors`) |

Served via `GET /api/asset-types` returning all entries; `[AllowAnonymous]` — the payload is static, non-sensitive product metadata, and skipping auth simplifies client caching and future public pages. Response DTOs flow through `generate:api` so frontend types stay in sync.

*Alternative:* keep the registry only in the frontend (status quo) — rejected because the backend needs it too (validation, usage defaults, follow-up features), and dual-maintained copies drift. *Alternative:* build-time codegen into the frontend — rejected as a second pipeline; a once-per-session fetch of ~2 KB JSON via the existing TanStack Query stack is simpler.

### D4: Flat DTO keyed by `category`; registry-driven applicability validation

`CreateAssetRequest` keeps the flat shape (all type-specific fields optional/nullable) but replaces `assetType` with required `category`. The service maps `category` → structural class via the registry and instantiates the right entity (single `switch` on 4 structural types — the only place structure is dispatched).

FluentValidation gains a registry-driven rule: **a non-null value in a field not listed in the category's `ApplicableFields` is a 400**, naming the offending field. Explicit rejection over silent nulling — silent data loss during create/update is worse than an error, and the frontend already clears inapplicable fields.

`AssetResponse` replaces `assetType` with `category` and adds read-only `structuralType` (derivable from the registry, but cheap and saves every consumer a lookup). List endpoint filter `?assetType=` becomes `?category=`, plus `?group=` for coarse filtering.

### D5: `UsageTrackingMode` defaulting

`CreateAssetRequest.usageTrackingMode` becomes nullable; when omitted the service applies the registry's `DefaultUsageTrackingMode`. It remains stored per-asset and freely editable. The frontend form prefills the registry default on category selection (visible, not hidden magic), so the backend default is a safety net for API callers.

### D6: Frontend consumes the served registry

- New `useAssetTypeRegistry()` hook: TanStack Query, `staleTime: Infinity`, `gcTime: Infinity` — fetched once per session, in-memory only. No localStorage persistence (adds invalidation concerns for no present benefit; payload is tiny).
- `lib/assetTypeFieldConfig.ts` is deleted. Labels, grouping, applicable-field lists, icon colors, and usage defaults come from the registry. A thin `lib/assetTypes.ts` may keep pure helpers (e.g. clearing inapplicable fields given a registry entry) plus the static field-label/kind map for rendering inputs (labels/input-kinds are presentation, acceptable to keep client-side; *which fields apply* is not).
- Asset form's type `Select` becomes grouped by `group` (shadcn `SelectGroup`) — the full card-based picker belongs to the wizard change.
- Views depending on the registry gate on the query (single small fetch on first authenticated page).

### D7: Migration reset

Delete `src/Steward.Infrastructure/Migrations/`, regenerate a single `InitialCreate` against the new model. Discriminator values are the four structural class names. Dev databases (including the compose Postgres volume and `steward_test`) are dropped/recreated. The `--setup` hosted service and Helm setup job (db-migrations spec) are unaffected — they just apply whatever migrations exist.

## Risks / Trade-offs

- [Enum/registry drift — a new `AssetCategory` without a registry entry] → bijection unit test fails the build.
- [Schema slop: inapplicable nullable columns (e.g. `TrackLengthIn` on a Car row)] → accepted TPH trade-off; applicability enforced at the validation boundary; test covers rejection.
- [Losing per-leaf C# types removes compile-time exhaustiveness for type-specific logic] → accepted; no current logic dispatches on leaf types except the mapper, which this change deletes. Structural dispatch (4 cases) remains type-safe.
- [Frontend async dependency: forms need the registry before rendering] → single ~2 KB cached query; loading gate on the form; failure surfaces as a retryable error state.
- [Broad test churn: every integration/unit test creating assets references old `assetType` values] → mechanical rename (`assetType: "Car"` → `category: "Car"`); most category names overlap with old type names.
- [`TypicalPermitKinds` as strings could desync with the future Registration `Kind` enum] → acceptable for now (nothing consumes it yet); the registration change will replace it with the real enum type when that enum exists.

## Migration Plan

Pre-launch, no deployed data. Order of work: Domain → Infrastructure (config/mapper/registry + fresh migration) → Application (DTOs/validators/service) → Api (controllers) → regenerate OpenAPI/`schema.d.ts` → frontend. Local/dev databases are dropped. Rollback = revert the branch; no data concerns.

## Open Questions

- Exact `DefaultUsageTrackingMode` per category (proposed: road vehicles `Mileage`; powersports and boats `Both`; PWC `Hours`; trailers `None`; equipment `Hours`) — confirm during implementation review.
- Whether `Generator` and `GolfCart` make the initial list or wait — zero marginal cost either way; defaulting to including them.
