## Context

Greenfield project. The repository contains only a placeholder README and OpenSpec config. This design establishes the monorepo layout, Clean Architecture layer structure, domain entity model, and auth/multi-tenancy infrastructure that all subsequent feature changes will build on. The reference implementation is `my-marina` (same author), which uses the same tech stack and conventions.

## Goals / Non-Goals

**Goals:**
- Establish the .NET 10 monorepo solution structure with correct project references and shared build config
- Define all domain entities in code with EF Core TPH inheritance for the asset hierarchy
- Configure Npgsql/PostgreSQL, ASP.NET Core Identity, and JWT Bearer auth
- Wire OAuth providers (Google, Facebook, Apple)
- Implement household multi-tenancy entities and resource-based authorization infrastructure
- Produce an initial EF Core migration and a `docker-compose.yml` for local dev PostgreSQL

**Non-Goals:**
- Feature CRUD endpoints (assets, service records, fuel logs, etc.) — subsequent changes
- Frontend pages and components — scaffold only (Vite project init, no pages)
- Email delivery, background jobs (Hangfire) — deferred
- File/document storage for registration and warranty attachments — deferred
- Refresh token storage strategy — deferred (access token expiry is decided here; refresh token backend is not)

## Decisions

### 1. Clean Architecture layer separation

**Decision:** Four-project backend: `Domain` → `Application` → `Infrastructure` → `Api`.

- `Domain`: Pure C# entities, enums, and domain interfaces. Zero framework dependencies.
- `Application`: Use cases, FluentValidation validators, service interfaces. References only `Domain`.
- `Infrastructure`: EF Core `DbContext`, Identity, Npgsql, OAuth wiring, service implementations. References `Application` + `Domain`.
- `Api`: ASP.NET Core composition root, controllers/minimal API endpoints, OpenAPI/Scalar. References `Application` + `Infrastructure`.

**Rationale:** Consistent with `my-marina`. Keeps domain logic independently testable. Makes infrastructure swappable (e.g., switching storage providers) without touching business logic.

**Alternative considered:** Single project "modular monolith" — rejected because it conflates persistence concerns with domain logic, making unit testing harder and future extraction of bounded contexts more painful.

---

### 2. EF Core TPH for the asset hierarchy

**Decision:** Single `Assets` table with a `Discriminator` string column. Intermediate abstract classes (`Vehicle`, `Trailer`, `Equipment`) exist in C# for shared properties but do not produce extra tables.

```
Assets table
├── Id, HouseholdId, Name, Description, Year, Photo, UsageTrackingMode, Discriminator
├── Vehicle columns: VIN (HIN for Boat), Color, ...
├── Snowmobile columns: TrackLengthIn, ...
├── Boat columns: HullMaterial, Length, BeamWidth, ...
└── ... (nullable columns for each subtype's specific fields)
```

**Rationale:** Performs best for the expected scale (dozens of assets per household, not millions). "All assets in a household" is a single table scan with no joins. Nullable column sprawl (~30 columns) is manageable and well within PostgreSQL limits. EF Core's default strategy; best tooling support.

**Alternative considered:** TPT (Table Per Type) — avoids nullable columns but requires a JOIN for every subtype read. Given the small per-household data volume, this is unnecessary complexity. TPC was eliminated early because polymorphic FK relationships (ServiceRecord → any Asset) require a shared key space.

---

### 3. JWT Bearer (stateless) over cookies

**Decision:** Issue signed JWT access tokens; clients send `Authorization: Bearer <token>`. No server-side session storage for access tokens.

**Rationale:** The frontend is a separate SPA served independently; future mobile apps will use the same API. Cookie-based auth complicates cross-origin requests and mobile clients. JWT is consistent with `my-marina`.

**Token expiry:** Access tokens expire in 15 minutes. Refresh token strategy is deferred; for now tokens are short-lived to limit exposure window.

---

### 4. Resource-based authorization (not dynamic Identity roles per household)

**Decision:** A single `HouseholdAuthorizationHandler` implements `IAuthorizationHandler<OperationAuthorizationRequirement, IHouseholdResource>`. It queries `HouseholdMembership` at request time to evaluate the caller's role.

```
HouseholdOperations.View       → Viewer | Contributor | Owner
HouseholdOperations.Edit       → Contributor | Owner
HouseholdOperations.Delete     → Owner
HouseholdOperations.Invite     → Owner
```

**Rationale:** Dynamic Identity roles per household (e.g., `BiglersGarage_Owner`) cause role table explosion at SaaS scale and make permission queries unreadable. Embedding household roles in the JWT causes stale permission issues (JWT doesn't invalidate when membership is revoked). Resource-based handlers query live DB state — membership changes take effect on the next request.

**PlatformAdmin bypass:** Controllers decorated with `[Authorize(Roles = "PlatformAdmin")]` skip household checks entirely.

---

### 5. Scrutor for DI registration

**Decision:** Use Scrutor assembly scanning to register application services and infrastructure implementations by convention rather than manual `services.AddScoped<IFoo, Foo>()` calls.

**Rationale:** Consistent with `my-marina`. Eliminates boilerplate as the service count grows.

---

### 6. docker-compose.yml for local development

**Decision:** Provide `docker-compose.yml` at repo root with a PostgreSQL 16 service, a named volume for data persistence, and a `healthcheck`. Connection string in `appsettings.Development.json` points at the compose service.

**Rationale:** Eliminates the requirement for developers to install PostgreSQL locally. Consistent with `my-marina` conventions.

### 7. API versioning — URL segment strategy

**Decision:** Use `Asp.Versioning.Mvc` (URL segment versioning) with the route template `api/v{version:apiVersion}/[controller]`. All controllers are decorated with `[ApiVersion("1.0")]`. Default version is `1.0`; `AssumeDefaultVersionWhenUnspecified = true` so unversioned clients still work. `ReportApiVersions = true` adds `api-supported-versions` headers to responses.

```csharp
// Program.cs
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";   // → "v1"
        options.SubstituteApiVersionInUrl = true;
    });

// OpenAPI — one document per version
builder.Services.AddOpenApi("v1");

// All controllers
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
```

**Rationale:** URL segment versioning is the most explicit and cache-friendly strategy — the version is visible in logs, proxies, and browser history. Header and query-string versioning are harder to test manually and less visible. `Asp.Versioning.Mvc.ApiExplorer` integrates cleanly with `Microsoft.AspNetCore.OpenApi` to produce a separate Scalar document per version, keeping v1 and future v2 docs independent.

**Alternatives considered:**
- Header versioning (`api-version: 1.0`) — less discoverable for API consumers, harder to test in a browser
- Query string (`?api-version=1.0`) — pollutes URLs, conflicts with query parameters on filter endpoints

**Future versions:** Adding v2 requires `builder.Services.AddOpenApi("v2")` and a new `[ApiVersion("2.0")]` on the relevant controllers. No structural changes.

---

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| TPH nullable column sprawl if many new asset subtypes are added | Acceptable at current scale; can migrate specific subtypes to TPT later if needed |
| JWT stateless — revoked memberships not immediately reflected | Short 15-minute access token expiry limits window; refresh token revocation list deferred to auth change |
| Large initial migration touches all tables at once | No risk on greenfield; document for future contributors that all-table migrations are exceptional |
| OAuth provider configuration requires client ID/secret in environment | Use `dotnet user-secrets` locally; document required env vars in README |

## Open Questions

- **Refresh token backend**: Store in PostgreSQL (`RefreshTokens` table) or Redis (as in `my-marina`)? Deferred to a dedicated auth change.
- **File storage provider**: S3-compatible (MinIO for self-hosted) or local filesystem for registration/warranty document attachments? Deferred.
- **PublicSlug uniqueness enforcement**: At DB level (unique index) or application level with retry? Recommend DB-level unique index on `Households.PublicSlug`.
