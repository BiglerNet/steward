# Maintenance Tracker

Self-hosted, multi-tenant garage/vehicle maintenance tracker. Backend: ASP.NET Core (.NET 10) Clean Architecture (`Domain` → `Application` → `Infrastructure` → `Api`) with EF Core/PostgreSQL. Frontend: React 19 + TypeScript + Vite.

## Local development

### Option A: native (no containers for API/frontend)

1. Start PostgreSQL: `docker compose up -d postgres`
2. Apply EF Core migrations: `dotnet ef database update --project src/Steward.Infrastructure --startup-project src/Steward.Api`
3. Run the API: `dotnet run --project src/Steward.Api`
4. Run the frontend: `cd src/Steward.Web && npm install && npm run dev`

The API exposes a Scalar API explorer at `/scalar/v1` in the Development environment.

### Option B: fully containerized stack

Runs Postgres, the API, and the frontend as production-style containers — the same topology this project will eventually run as a Helm-deployed Kubernetes stack.

1. Copy the example environment file: `cp .env.example .env` (adjust values if needed; defaults work out of the box).
2. Build and start everything: `docker compose up --build`
   - `postgres` starts first; `api` waits for its healthcheck before starting; `web` serves the built frontend via Nginx on `http://localhost:8081`, and `api` is reachable at `http://localhost:5000`.
3. Apply EF Core migrations against the containerized Postgres, using the host .NET SDK against Postgres's host-published port (the shipped `api` image is runtime-only and doesn't include the `dotnet ef` tooling):
   ```
   dotnet ef database update --project src/Steward.Infrastructure --startup-project src/Steward.Api --connection "Host=localhost;Port=5432;Database=steward;Username=steward;Password=steward"
   ```
4. After running migrations, restart the `api` service so it starts cleanly against the now-migrated schema: `docker compose up -d api`.

Notes:
- Uploaded documents (`document-storage` capability) are persisted in the named volume `storage_data`, mounted at `/app/storage` in the `api` container. Inspect it with `docker compose exec api ls /app/storage`, or `docker volume inspect steward_storage_data` from the host. To use a host-visible folder instead, see the commented-out bind-mount alternative in `docker-compose.yml`.
- The frontend's API base URL is injected at container *startup* (not baked into the image) via the `API_BASE_URL` environment variable — changing it and running `docker compose up -d web` picks up the new value without a rebuild.
- Changes to `api`/`web` source require a rebuild: `docker compose up --build api web`.

## Required configuration

Set the following via `dotnet user-secrets`, environment variables, or `appsettings.Development.json` (already configured for local docker-compose use):

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Jwt:Key` | HS256 signing key for JWT access tokens (min 32 chars) |
| `Jwt:Issuer` / `Jwt:Audience` | JWT issuer/audience values |
| `Jwt:ExpiryMinutes` | Access token lifetime (default 15) |
| `Auth:Google:ClientId` / `ClientSecret` | Google OAuth credentials (optional — provider skipped if unset) |
| `Auth:Facebook:ClientId` / `ClientSecret` | Facebook OAuth credentials (optional — provider skipped if unset) |
| `Auth:Apple:ClientId` / `KeyId` / `TeamId` | Apple OAuth credentials (optional — provider skipped if unset) |
| `Cors:AllowedOrigins` | Array of allowed frontend origins |
