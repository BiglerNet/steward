## Context

The app currently ships with a `docker-compose.yml` for local development and per-service Dockerfiles. There is no path to deploy it anywhere. The target environment is an on-prem k3s cluster already running: Crunchy PGO (cluster-wide), Traefik ingress, cert-manager with a `*.biglernet.com` wildcard, and GitHub ARC runners scoped to the `biglernet` GitHub org.

The peer project `my-marina` (`ghcr.io/biglernet/my-marina`) already uses this same cluster with the same toolchain and serves as the primary reference for conventions.

## Goals / Non-Goals

**Goals:**
- Helm chart that deploys the full stack (API, Web UI, Postgres) to k8s
- Automated migrations on every install/upgrade — zero manual steps
- CI on every push (build + test), deploy to test on merge to main, deploy to prod on release
- Automated changelog and semver via release-please + conventional commits
- All config (images, hostnames, secrets) overridable per environment without changing chart templates

**Non-Goals:**
- Database backups (structure present but disabled — enable later)
- Horizontal scaling (HPA scaffold present but disabled)
- Database replication
- Integration test execution in CI (requires postgres sidecar — future work)
- PlatformAdmin seeding (handled by the separate `platform-admin-bootstrap` change)

## Decisions

### D1: Helm chart structure — single chart, per-env values files
**Decision:** One chart (`charts/steward/`) with `values.yaml` base defaults and `values-test.yaml` / `values-prod.yaml` overrides. Deploy with `helm upgrade --install -f values-prod.yaml`.

**Alternatives considered:** Separate charts per environment (rejected — doubles maintenance burden for no benefit at this scale); Helmfile or Kustomize overlay (rejected — adds toolchain complexity, Helm alone is sufficient).

### D2: Postgres — Crunchy PGO PostgresCluster CRD, no chart dependency on operator
**Decision:** Emit a `PostgresCluster` CRD resource from the chart. The chart does NOT install the operator (it is already cluster-wide). `database.enabled: true` gates the resource; set to `false` to use an external DB.

Backups: pgbackrest config block is present in the template but gated by `database.backups.enabled: false` default. Flipping the value enables scheduled full + differential backups to an NFS-backed PVC.

**Storage class:** `nfs-csi-slow` for backup volume. Data volume uses cluster default (no explicit `storageClassName` on the data PVC — mirrors my-marina behavior).

**PGO credential secret naming:** PGO creates `<cluster-name>-pguser-<db-username>`. The `_helpers.tpl` encodes this as `{{ include "steward.database.credentialSecret" . }}` so all templates reference it consistently.

**Connection string assembly:** The API reads five env vars (`DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`) sourced from the PGO secret via `secretKeyRef`, then the `ConnectionStrings__DefaultConnection` env var is assembled from them inline. This avoids storing the full URI in a chart-managed secret while keeping config transparent.

### D3: Database migrations — SetupHostedService with `--setup` flag
**Decision:** A new `SetupHostedService : IHostedService` in `Steward.Infrastructure/Setup/` runs when `args` contains `--setup`. It calls `db.Database.MigrateAsync()` (idempotent), logs result, then calls `IHostApplicationLifetime.StopApplication()`. The normal application startup path is unchanged.

**Hook timing:** `post-install,pre-upgrade`
- `post-install`: Runs after PGO has had time to provision the cluster. The `backoffLimit: 6` and initContainer (`pg_isready` loop) handle the 30–90s PGO provisioning window on first install. Pod may cycle a few times while the PGO-created secret materializes — this is expected and recoverable.
- `pre-upgrade`: Runs before the new API Deployment rolls out, ensuring schema is up-to-date before new code goes live. Safe because the DB and its secret already exist on upgrade.

`hook-delete-policy: hook-succeeded,before-hook-creation` — failed jobs persist for debugging; succeeded jobs are cleaned up; stale jobs from prior runs are removed before re-execution.

**Why not init-container in the API Deployment?** Coupling migration to every pod restart creates risk: a crashing pod retries migrations in a tight loop. The hook Job runs once per deploy, has a clear pass/fail signal to Helm, and can be inspected independently.

**Why not pre-install?** On first install, the PGO secret does not exist yet — the pod would fail to schedule before the initContainer even runs.

### D4: Ingress — single hostname, path-prefix routing, optional TLS secret
**Decision:** One `Ingress` resource per release. Routes: `/api/*` → API service, `/*` → Web service. The API controllers already use `[Route("api/...")]` so no prefix stripping is needed.

TLS block always emits the `hosts:` entry. `secretName` is only emitted if `ingress.tlsSecretName` is non-empty, allowing the cluster-default wildcard (`*.biglernet.com`) to be used without specifying a secret — which is the default for this project.

### D5: CI/CD pipeline structure — four focused workflows
**Decision:**

```
ci.yml             push (any branch)   → dotnet build, dotnet test, npm build, npm test
build-push.yml     push to main        → docker build API + Web, push to ghcr.io/biglernet/
deploy-test.yml    push to main        → helm upgrade --install steward-test
deploy-prod.yml    release published   → retag image to semver, helm upgrade --install steward-prod
```

Images are tagged with the full git SHA on every main push. On release, the SHA image is re-tagged with the semver version — no re-build, so prod gets the exact artifact tested in test.

GHCR org: `ghcr.io/biglernet/steward/api` and `.../web`. Requires `packages: write` permission on the workflow and the repo to be under (or granted access by) the `biglernet` org.

### D6: Semver and changelog — release-please
**Decision:** release-please bot watches `main` for conventional commits, maintains `CHANGELOG.md`, and creates a Release PR. Merging the Release PR creates a GitHub Release and tag, which triggers `deploy-prod.yml`.

**Why release-please over GitVersion?** release-please unifies semver calculation and changelog generation in one tool with zero configuration beyond a JSON file. GitVersion is powerful but requires branching strategy configuration and produces versions at build time (not release time). For a trunk-based flow with GitHub releases as the deployment gate, release-please is the simpler fit.

Conventional commit types surfaced in changelog: `feat`, `fix`, `chore`, `docs`, `refactor`. Breaking changes (`feat!`) trigger a major bump.

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| PGO secret not ready when setup Job pod starts on first install | `backoffLimit: 6` gives ~6 retries with exponential backoff; initContainer confirms DB readiness before migrations run |
| New code live briefly before pre-upgrade migrations complete (edge: if pre-upgrade hook times out) | Hook timeout defaults to 5 min; migrations are fast; additive-only migration discipline eliminates the breakage window |
| Helm deploy-to-test runs concurrently with CI on a busy branch | `deploy-test.yml` depends on `build-push.yml` completing; GitHub Actions job ordering prevents race |
| GHCR image push requires org-level package write access | Repo must be in or granted access from `biglernet` org; ARC runners must use a token with `packages:write` |
| release-please needs a GitHub App or PAT with `contents:write` to create Release PRs | Standard GITHUB_TOKEN does not have sufficient permissions for cross-workflow triggers; a PAT or GitHub App is required |

## Open Questions

- **Kubeconfig secret for ARC runners:** ARC runners need a `KUBECONFIG` or service account token to run `helm upgrade`. Where is this stored (GitHub org secret, repo secret, Vault)? Needs to be established before deploy workflows can be tested.
- **release-please token:** GITHUB_TOKEN cannot trigger other workflows. A PAT or GitHub App needs to be set up for release-please to create PRs that trigger CI. Confirm which mechanism the org uses.
- **Storage class for data PVC:** My-marina omits `storageClassName` on the data volume (uses cluster default). Confirm the cluster default is appropriate, or set `nfs-csi-slow` explicitly.
