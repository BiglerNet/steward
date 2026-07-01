## 1. Api — Dockerfile

- [x] 1.1 [Api] Create `src/Steward.Api/Dockerfile` — multi-stage build: `mcr.microsoft.com/dotnet/sdk:10.0` build stage running `dotnet restore`/`dotnet publish`, `mcr.microsoft.com/dotnet/aspnet:10.0` runtime final stage.
- [x] 1.2 [Api] Add a `.dockerignore` excluding `bin/`, `obj/`, `node_modules/`, `.storage/` from the build context.
- [x] 1.3 [Api] Verify the image starts standalone (`docker build` + `docker run` with env vars set manually) before wiring into Compose.

## 2. Web — Dockerfile

- [x] 2.1 [Web] Create `src/Steward.Web/Dockerfile` — multi-stage build: Node build stage (`npm ci && npm run build`), `nginx:alpine` final stage copying `dist/`.
- [x] 2.2 [Web] Add `nginx.conf` with SPA fallback (`try_files $uri /index.html`) and gzip enabled.
- [x] 2.3 [Web] Add a startup entrypoint script that generates a small `config.js` (exposing `window.__APP_CONFIG__.apiBaseUrl`) from the `API_BASE_URL` environment variable before Nginx starts; reference `config.js` from `index.html`.
- [x] 2.4 [Web] Update the frontend's API client setup to read `window.__APP_CONFIG__.apiBaseUrl` (falling back to a Vite dev-time env var when running outside the container) instead of a hardcoded base URL.
- [x] 2.5 [Web] Add a `.dockerignore` excluding `node_modules/`, `dist/` from the build context.

## 3. Compose — Services and Networking

- [x] 3.1 [Infrastructure] Add an `api` service to `docker-compose.yml` (build context `src/Steward.Api`), with `depends_on: postgres: condition: service_healthy`.
- [x] 3.2 [Infrastructure] Add a `web` service to `docker-compose.yml` (build context `src/Steward.Web`).
- [x] 3.3 [Infrastructure] Confirm all three services share the default Compose network and can reach each other by service name (`api` resolves `postgres`, `web`'s runtime config points at `api`'s exposed port).

## 4. Compose — Storage Volume

- [x] 4.1 [Infrastructure] Add a `storage_data` named volume to `docker-compose.yml`, mounted into the `api` service at the container path matching `Storage__RootPath`.
- [x] 4.2 [Infrastructure] Document the host-bind-mount override (commented-out alternative in `docker-compose.yml` or a `docker-compose.override.yml.example`) for developers who want host-visible uploaded files.

## 5. Configuration Externalization

- [x] 5.1 [Infrastructure] Add `.env.example` documenting every required variable: `POSTGRES_*`, `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Cors__AllowedOrigins__0`, `Frontend__BaseUrl`, `Storage__RootPath`, `Storage__MaxUploadSizeBytes`, `API_BASE_URL`, OAuth client id/secret placeholders.
- [x] 5.2 [Infrastructure] Add `.env` to `.gitignore` if not already covered.
- [x] 5.3 [Infrastructure] Wire `env_file: .env` (or explicit `environment:` entries referencing `${VAR}`) into the `api` and `web` services in `docker-compose.yml`.

## 6. Documentation

- [x] 6.1 [Docs] Update `README.md` with the containerized local dev workflow: copying `.env.example` to `.env`, `docker compose up [--build]`, running EF Core migrations against the containerized Postgres (`dotnet ef database update` with the Compose connection string, or a one-off `docker compose run api dotnet ef database update`), and inspecting the storage volume.

## 7. Verification

- [x] 7.1 [Manual] Run `docker compose up --build` from a clean state and confirm `postgres` → `api` → `web` all reach a healthy/running state.
- [x] 7.2 [Manual] Upload a document via the running API, restart the `api` container, and confirm the document is still downloadable (validates the storage volume persists).
- [x] 7.3 [Manual] Change `API_BASE_URL` and restart only the `web` container (no rebuild) and confirm the frontend picks up the new value.
