## Why

`docker-compose.yml` currently only runs PostgreSQL — the API and frontend still run via `dotnet run`/`npm run dev` directly on the host. With `registration-and-warranty` having just shipped a filesystem-backed document storage layer expecting a mounted volume, and the end deployment target being Kubernetes (Helm), the project needs a containerized local dev stack now so the Docker images, volume mounts, and environment-variable configuration get exercised and debugged locally before they're re-expressed as a Helm chart later. Doing this now (rather than waiting) also means every subsequent change gets tested against the same container topology it will eventually ship with.

## What Changes

- Add a `Dockerfile` for `Steward.Api` (multi-stage: .NET 10 SDK build → ASP.NET runtime image).
- Add a `Dockerfile` for `Steward.Web` (multi-stage: Node build → static file serve, e.g. via `nginx` or a minimal Node static server) — production-style container, not the Vite dev server.
- Extend `docker-compose.yml` to add `api` and `web` services alongside the existing `postgres` service, wired together via a Compose network, with the API depending on Postgres's healthcheck.
- Add a named volume (or local bind mount) for the API's `Storage:RootPath`, mounted into the `api` container — the local-dev equivalent of the NFS-backed PersistentVolumeClaim planned for the Kubernetes deployment, so the same `IFileStorageService`/`LocalFileStorageService` code path from `registration-and-warranty` is exercised end-to-end locally.
- Externalize all container configuration (connection string, JWT key, OAuth client secrets, CORS origins, frontend base URL, storage root path) as Compose environment variables / a `.env.example` file, rather than relying on `appsettings.Development.json` — this is also a forcing function for the eventual Helm `values.yaml`/Secret mapping.
- Document the local container workflow (`docker compose up`, rebuild on change, running EF Core migrations against the containerized Postgres) in the project README.
- Explicitly design the Compose file's service/volume/env-var shape to map cleanly onto a future Helm chart (Deployment + Service per container, PVC for storage, ConfigMap/Secret for env vars) — that Helm chart itself is out of scope for this change.

## Capabilities

### New Capabilities
- `local-containerized-stack`: Defines the Docker images and Compose topology (services, network, volumes, environment configuration) needed to run the full application (API, frontend, database, document storage) locally in containers.

### Modified Capabilities
- (none)

## Impact

- **Api**: New `Dockerfile`; no application code changes — configuration moves from `appsettings.Development.json` defaults to environment-variable overrides supplied by Compose.
- **Web**: New `Dockerfile`; needs a production build step (`npm run build`) and a static file server stage; needs to know the API's base URL at runtime (build-time env var or runtime-injected config, decided in design.md).
- **Infrastructure (deployment)**: `docker-compose.yml` gains `api`, `web` services, a Compose network, and a storage volume; new `.env.example` documenting required variables.
- **Dependencies**: No new .NET/npm packages; adds Docker/Compose as a required local tool (already assumed given the project's containerized deployment target).
