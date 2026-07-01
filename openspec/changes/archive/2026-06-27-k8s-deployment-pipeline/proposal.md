## Why

The application has a working local stack (docker-compose) but no path to production. This change delivers the full deployment pipeline: a Helm chart for on-premise Kubernetes, a CI/CD pipeline via GitHub Actions, and automated database migrations on every deploy — so the project can ship to a real environment.

## What Changes

- **Helm chart** (`charts/steward/`) — new chart deploying the API, Web UI, and PostgresCluster (Crunchy PGO) to an on-prem k3s cluster, with separate values overrides for test (`steward-test` namespace) and prod (`steward-prod` namespace)
- **SetupHostedService** — new `IHostedService` in `Steward.Infrastructure` triggered by `--setup` CLI arg; applies EF Core migrations idempotently; runs as a Helm `post-install,pre-upgrade` hook Job
- **GitHub Actions workflows** — CI (build + test on every push), image build/push to GHCR org (`ghcr.io/biglernet/`) on merge to main, deploy-to-test on merge to main, deploy-to-prod on GitHub release published
- **release-please config** — conventional-commit-driven semver, automated changelog, and GitHub release creation
- No changes to app behavior, domain model, or API contracts

## Capabilities

### New Capabilities

- `k8s-deployment`: Helm chart that provisions the PostgresCluster, API deployment, Web UI deployment, services, and ingress for on-prem Kubernetes — with per-environment values files for test and prod namespaces
- `db-migrations`: Automated schema migration triggered on every Helm install/upgrade via a setup Job running `--setup`; implemented as a `SetupHostedService` in the Infrastructure layer that calls `database.MigrateAsync()` idempotently
- `ci-cd-pipeline`: GitHub Actions pipeline covering CI (build + test), Docker image build/push to GHCR, Helm-based deploy to test on merge, and Helm-based deploy to prod on release publish; semver and changelog managed by release-please

### Modified Capabilities

(none — no existing spec-level requirements are changing)

## Impact

- **New files**: `charts/steward/**`, `.github/workflows/ci.yml`, `.github/workflows/build-push.yml`, `.github/workflows/deploy-test.yml`, `.github/workflows/deploy-prod.yml`, `.github/release-please-config.json`, `.github/release-please-manifest.json`, `src/Steward.Infrastructure/Setup/SetupHostedService.cs`
- **Modified files**: `src/Steward.Api/Program.cs` (conditional `--setup` host registration), `src/Steward.Infrastructure/InfrastructureServiceExtensions.cs` (register setup services)
- **Dependencies added**: none (EF Core migration APIs already present)
- **External dependencies**: Crunchy PGO operator (already installed cluster-wide), Traefik ingress controller (already installed), ARC self-hosted runners (already installed, org-scoped to `biglernet`), GHCR package registry under `ghcr.io/biglernet/`
