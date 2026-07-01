### Requirement: Containerized API service
The system SHALL provide a `Dockerfile` for `Steward.Api` that produces a runnable container image via a multi-stage build (.NET SDK build stage, ASP.NET runtime final stage). The image SHALL read all configuration (connection string, JWT key, OAuth secrets, CORS origins, frontend base URL, storage root path) from environment variables using ASP.NET Core's double-underscore naming convention, requiring no `appsettings.Development.json` changes to run.

#### Scenario: API container starts against a containerized Postgres
- **WHEN** `docker compose up api` is run with `postgres` healthy
- **THEN** the API container starts successfully and responds to its health endpoint

#### Scenario: API configuration comes entirely from environment variables
- **WHEN** the `api` service is started with `ConnectionStrings__DefaultConnection`, `Jwt__Key`, and `Storage__RootPath` set via Compose environment variables
- **THEN** the API uses those values with no source-controlled secrets baked into the image

---

### Requirement: Containerized frontend service
The system SHALL provide a `Dockerfile` for `Steward.Web` that builds the production Vite bundle and serves it via a lightweight static file server (e.g. `nginx:alpine`), with single-page-application fallback routing to `index.html`. The API base URL the frontend calls SHALL be configurable at container startup without rebuilding the image.

#### Scenario: Frontend container serves the built SPA
- **WHEN** `docker compose up web` is run
- **THEN** requesting `/` returns the built `index.html` and client-side routes (e.g. `/households/123`) also resolve to `index.html` rather than a 404

#### Scenario: API base URL is configurable without a rebuild
- **WHEN** the `web` container is started with a different `API_BASE_URL` environment variable than a previous run, using the same image
- **THEN** the frontend's runtime configuration reflects the new value without requiring `npm run build` to run again

---

### Requirement: Compose stack networks API, frontend, and database together
`docker-compose.yml` SHALL define `api`, `web`, and `postgres` services on a shared network, with `api` depending on `postgres`'s existing healthcheck (`service_healthy`) before starting, and `web` reachable independently of `api`'s readiness.

#### Scenario: Full stack starts in dependency order
- **WHEN** `docker compose up` is run from a clean state
- **THEN** `postgres` becomes healthy before `api` starts, and all three services reach a running state

---

### Requirement: Persistent storage volume for document attachments
`docker-compose.yml` SHALL define a named volume mounted into the `api` service at the path configured by `Storage__RootPath`, so files uploaded via the `document-storage` capability persist across container restarts.

#### Scenario: Uploaded document survives a container restart
- **WHEN** a document is uploaded via the API while the stack is running, and the `api` container is then restarted
- **THEN** the previously uploaded document is still retrievable after restart

---

### Requirement: Externalized configuration via `.env` file
The repository SHALL include a checked-in `.env.example` documenting every environment variable required by the `api` and `web` services, with safe non-secret local-dev defaults. Actual secret values SHALL be supplied via a gitignored `.env` file, never committed.

#### Scenario: A new developer can start the stack from the example file
- **WHEN** a developer copies `.env.example` to `.env` without further edits
- **THEN** `docker compose up` succeeds and the stack is usable for local development
