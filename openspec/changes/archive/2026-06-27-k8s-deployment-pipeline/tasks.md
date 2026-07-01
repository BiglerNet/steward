## 1. SetupHostedService (Infrastructure + Api)

- [x] 1.1 Create `src/Steward.Infrastructure/Setup/SetupHostedService.cs` implementing `IHostedService`; `StartAsync` calls `db.Database.MigrateAsync()`, logs result, then calls `IHostApplicationLifetime.StopApplication()`; exits 0 on success, 1 on exception
- [x] 1.2 Create `src/Steward.Infrastructure/Setup/SetupOptions.cs` (empty for now — placeholder for future seed config)
- [x] 1.3 Register `SetupHostedService` conditionally in `Program.cs` (Infrastructure layer extension or directly in Api): only when `args.Contains("--setup")`; normal startup path is unchanged
- [x] 1.4 Verify locally: `dotnet run --project src/Steward.Api -- --setup` applies pending migrations and exits 0; subsequent run exits 0 with "no pending migrations" log

## 2. Helm Chart Scaffold

- [x] 2.1 Run `helm create charts/steward` from repo root; delete the generated `charts/steward/templates/` contents and `charts/steward/values.yaml` (keep `Chart.yaml`, `.helmignore`)
- [x] 2.2 Write `charts/steward/templates/_helpers.tpl` with helpers: `steward.fullname`, `steward.name`, `steward.chart`, `steward.labels`, `steward.selectorLabels`, component name helpers (`api`, `web`, `database`), PGO credential secret name helper (`<cluster>-pguser-<username>`), and `initContainers.waitForDatabase` (pg_isready loop, mirrors my-marina pattern)
- [x] 2.3 Write base `charts/steward/values.yaml` with all configurable values: api image/port/resources/nodeSelector, web image/port/resources, database (enabled, postgresVersion=17, username, storage, backups.enabled=false, backupStorage, storageClassName=nfs-csi-slow), ingress (className=traefik, host, tlsSecretName=""), setupJob (enabled=true, backoffLimit=6)

## 3. Helm Chart — Database Template

- [x] 3.1 Write `charts/steward/templates/database.yaml`: `PostgresCluster` CRD gated by `database.enabled`; single instance replica=1; data volume uses cluster-default storage class (no explicit `storageClassName`); pgbackrest block gated by `database.backups.enabled` (when false, emit no repos/schedules)
- [x] 3.2 Verify `helm template` renders the `PostgresCluster` correctly with `database.enabled=true` and that the backup block is absent when `database.backups.enabled=false`

## 4. Helm Chart — API and Web Deployments

- [x] 4.1 Write `charts/steward/templates/api-deployment.yaml`: Deployment + container pulling `api.image.repository:api.image.tag`; env vars `DB_HOST/PORT/NAME/USER/PASSWORD` from PGO secret via `secretKeyRef`; `ConnectionStrings__DefaultConnection` assembled inline; all other config (JWT, CORS, storage) from a ConfigMap or additional env values
- [x] 4.2 Write `charts/steward/templates/web-deployment.yaml`: Deployment pulling `web.image.repository:web.image.tag`; minimal env (API base URL)
- [x] 4.3 Write `charts/steward/templates/api-service.yaml` and `charts/steward/templates/web-service.yaml`: both ClusterIP
- [x] 4.4 Verify `helm template` renders both Deployments and Services with correct image references and secret wiring

## 5. Helm Chart — Ingress and Setup Job

- [x] 5.1 Write `charts/steward/templates/ingress.yaml`: single `Ingress` with `traefik` class; path `/api` (Prefix) → api service; path `/` (Prefix) → web service; TLS block always emits `hosts`; emits `secretName` only when `ingress.tlsSecretName` is non-empty
- [x] 5.2 Write `charts/steward/templates/setup-job.yaml`: Job with hook annotations `post-install,pre-upgrade`; `hook-delete-policy: hook-succeeded,before-hook-creation`; `backoffLimit: 6`; `restartPolicy: Never`; initContainer using `steward.initContainers.waitForDatabase` helper; main container uses API image with `args: ["--setup"]` and same env as API Deployment; gated by `setupJob.enabled`
- [x] 5.3 Verify `helm template` renders setup job with correct hook annotations, env vars from PGO secret, and `--setup` arg

## 6. Per-Environment Values Files

- [x] 6.1 Write `charts/steward/values-test.yaml` overriding: `ingress.host=steward-test.biglernet.com`, `api.image.pullPolicy=Always`, reduced resource limits appropriate for test
- [x] 6.2 Write `charts/steward/values-prod.yaml` overriding: `ingress.host=steward.biglernet.com`, `api.image.pullPolicy=IfNotPresent`, production-appropriate resource requests/limits
- [x] 6.3 Run `helm template -f charts/steward/values-test.yaml` and `values-prod.yaml` and confirm hostname differences render correctly

## 7. GitHub Actions — CI Workflow

- [x] 7.1 Write `.github/workflows/ci.yml`: trigger on `push` (all branches) and `pull_request` targeting `main`; job: checkout, setup dotnet (version from `global.json`), `dotnet build`, `dotnet test tests/Steward.UnitTests`; setup Node 22, `npm ci` in `src/Steward.Web`, `npm run build`, `npm test`

## 8. GitHub Actions — Build and Push Workflow

- [x] 8.1 Write `.github/workflows/build-push.yml`: trigger on push to `main`; login to GHCR using `GITHUB_TOKEN`; build and push API image (context: `.`, dockerfile: `src/Steward.Api/Dockerfile`) tagged `ghcr.io/biglernet/steward/api:sha-${{ github.sha }}` and `:latest`; build and push Web image (context: `src/Steward.Web`) tagged `ghcr.io/biglernet/steward/web:sha-${{ github.sha }}` and `:latest`
- [x] 8.2 Confirm the workflow outputs the SHA tag as a job output for use by downstream deploy workflows

## 9. GitHub Actions — Deploy Workflows

- [x] 9.1 Write `.github/workflows/deploy-test.yml`: trigger on workflow_run completion of `build-push.yml` on `main`; run `helm upgrade --install steward charts/steward -n steward-test -f charts/steward/values-test.yaml --set api.image.tag=sha-<sha> --set web.image.tag=sha-<sha>`; kubeconfig sourced from org secret
- [x] 9.2 Write `.github/workflows/deploy-prod.yml`: trigger on `release.published`; determine SHA from release tag's commit; retag API and Web images from `sha-<sha>` to the release tag (e.g., `v1.3.0`) and push; run `helm upgrade --install steward charts/steward -n steward-prod -f charts/steward/values-prod.yaml --set api.image.tag=<release-tag> --set web.image.tag=<release-tag>`

## 10. release-please Configuration

- [x] 10.1 Write `.github/release-please-config.json`: single package at repo root, `release-type: simple`, changelog sections for `feat`, `fix`, `chore`, `docs`, `refactor`
- [x] 10.2 Write `.github/release-please-manifest.json`: initial version `0.1.0`
- [x] 10.3 Add release-please GitHub Actions workflow (or confirm release-please app is installed on the `biglernet` org and no workflow is needed)

## 11. Resolve Open Questions (Pre-Deploy)

- [x] 11.1 Confirm kubeconfig or service account token for ARC runners is available as a GitHub org secret; document the secret name in `deploy-test.yml` and `deploy-prod.yml`
- [x] 11.2 Confirm release-please token mechanism (PAT vs GitHub App) for the `biglernet` org; configure accordingly
- [x] 11.3 Confirm cluster-default storage class is appropriate for data PVC (or add explicit `storageClassName: nfs-csi-slow` to data volume in `database.yaml`)
- [x] 11.4 Create `steward-test` and `steward-prod` namespaces on the cluster if not already present
