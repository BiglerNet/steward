# db-migrations Specification

## Purpose
Defines how EF Core migrations are applied via the SetupHostedService at startup/deploy time.

## Requirements
### Requirement: SetupHostedService applies EF Core migrations when invoked with --setup
The system SHALL include a `SetupHostedService : IHostedService` in `Steward.Infrastructure/Setup/`. When the application is started with `--setup` as a command-line argument, this service SHALL call `database.Database.MigrateAsync()` to apply all pending EF Core migrations, then stop the application via `IHostApplicationLifetime.StopApplication()`. The normal application startup (without `--setup`) SHALL be unaffected.

#### Scenario: Pending migrations are applied on --setup run
- **WHEN** the application is started with `--setup` and the database has pending migrations
- **THEN** all pending migrations are applied and the process exits with code 0

#### Scenario: No-op when no pending migrations
- **WHEN** the application is started with `--setup` and the database is already up-to-date
- **THEN** the service logs that no migrations are pending and the process exits with code 0

#### Scenario: Process exits with code 1 on migration failure
- **WHEN** the application is started with `--setup` and migration fails (e.g., DB unreachable)
- **THEN** the error is logged and the process exits with code 1

#### Scenario: Normal startup is unaffected
- **WHEN** the application is started without `--setup`
- **THEN** `SetupHostedService` does not register and the application starts normally without running migrations

---

### Requirement: Setup runs as a Helm hook Job on install and before upgrade
The Helm chart SHALL include a `setup-job.yaml` template that creates a Kubernetes `Job` with hook annotations `post-install,pre-upgrade`. The Job SHALL run the API container image with `args: ["--setup"]`. It SHALL have `backoffLimit: 6` and `restartPolicy: Never`. It SHALL include an initContainer that polls `pg_isready` until the database accepts connections before the setup container starts. The hook-delete policy SHALL be `hook-succeeded,before-hook-creation`.

#### Scenario: Setup job runs migrations after fresh install
- **WHEN** `helm install` completes and PGO has provisioned the database
- **THEN** the setup Job runs, applies migrations, and completes successfully; the API Deployment starts with a migrated schema

#### Scenario: Setup job runs migrations before new API pods on upgrade
- **WHEN** `helm upgrade` is executed with a new image tag containing a new migration
- **THEN** the setup Job runs and applies the migration BEFORE the new API Deployment rolls out

#### Scenario: Setup job survives PGO provisioning delay on first install
- **WHEN** the setup Job pod starts before PGO has created the database credential secret
- **THEN** the pod enters a retry cycle (up to backoffLimit: 6) and eventually succeeds once the secret is available and the database is ready

#### Scenario: Failed job persists for debugging
- **WHEN** the setup Job fails all retries
- **THEN** the failed Job and its pods remain in the namespace (not auto-deleted) so logs can be inspected; the Helm install/upgrade is marked as failed

#### Scenario: Setup job can be disabled
- **WHEN** the chart is installed with `setupJob.enabled=false`
- **THEN** no setup Job is created and migrations must be applied manually

---

### Requirement: Database connection config is sourced from PGO credential secret in the setup job
The setup Job SHALL read database connection details from the PGO-created secret (`<cluster>-pguser-<db-username>`) using `secretKeyRef` for `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, and `DB_PASSWORD`. The `ConnectionStrings__DefaultConnection` environment variable SHALL be assembled from these values inline, matching the format the Npgsql provider accepts.

#### Scenario: Connection string is assembled from PGO secret fields
- **WHEN** the setup Job pod starts with PGO secret present
- **THEN** the container's `ConnectionStrings__DefaultConnection` environment variable is set to a valid Npgsql connection string using the credentials from the secret, without any chart-managed plaintext passwords
