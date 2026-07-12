# k8s-deployment Specification

## Purpose
Defines the Helm chart and Kubernetes deployment topology for the API and Web UI.

## Requirements
### Requirement: Helm chart deploys API and Web UI to Kubernetes
The system SHALL provide a Helm chart (`charts/steward/`) that deploys the API and Web UI as separate Kubernetes Deployments, each backed by a ClusterIP Service, to a target namespace. The chart SHALL use the image repositories `ghcr.io/biglernet/steward/api` and `ghcr.io/biglernet/steward/web` with the image tag configurable via `api.image.tag` and `web.image.tag` values.

#### Scenario: Chart installs cleanly to a new namespace
- **WHEN** `helm upgrade --install steward charts/steward -n steward-test` is run against a cluster with PGO installed
- **THEN** the API pod, Web UI pod, PostgresCluster, and setup Job all reach a healthy/completed state within 5 minutes

#### Scenario: Image tag is overridable per environment
- **WHEN** the chart is deployed with `--set api.image.tag=sha-abc123`
- **THEN** the API Deployment uses image `ghcr.io/biglernet/steward/api:sha-abc123`

---

### Requirement: Crunchy PGO PostgresCluster is provisioned by the chart
The chart SHALL include a `PostgresCluster` CRD resource (apiVersion `postgres-operator.crunchydata.com/v1beta1`) when `database.enabled` is `true` (the default). The cluster SHALL run Postgres 17 with a single replica, using the cluster-default storage class for the data volume and `nfs-csi-slow` for the pgbackrest backup volume. The chart SHALL NOT install the PGO operator — it assumes the operator is already present cluster-wide.

#### Scenario: Database enabled by default
- **WHEN** the chart is installed with default values
- **THEN** a `PostgresCluster` resource named `<release>-database` exists in the release namespace and PGO provisions a running Postgres pod

#### Scenario: Database can be disabled for external DB usage
- **WHEN** the chart is installed with `database.enabled=false`
- **THEN** no `PostgresCluster` resource is created and the API is configured to use a separately-supplied connection string

#### Scenario: PGO credential secret is wired into the API
- **WHEN** PGO has provisioned the cluster and created the secret `<release>-database-pguser-<db-username>`
- **THEN** the API pod reads `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, and `DB_PASSWORD` from that secret and constructs its connection string without any chart-managed credentials

---

### Requirement: Database backups are disabled by default but trivially enableable
The chart SHALL include pgbackrest backup schedules (full weekly, differential daily) in the `PostgresCluster` template, gated by `database.backups.enabled`. When `false` (default), no backup schedule is emitted. When `true`, scheduled full and differential backups are configured using the storage class and size specified in `database.storageClassName` and `database.backupStorage`.

#### Scenario: Backups disabled by default
- **WHEN** the chart is installed with default values
- **THEN** the `PostgresCluster` resource contains no pgbackrest repo schedule configuration

#### Scenario: Backups enabled with a single values change
- **WHEN** the chart is installed with `database.backups.enabled=true`
- **THEN** the `PostgresCluster` resource contains a pgbackrest repo with full and differential backup schedules

---

### Requirement: Single-hostname ingress routes /api to API and / to Web UI
The chart SHALL create a single Kubernetes `Ingress` resource using the `traefik` ingress class. Path `/api` (prefix) SHALL route to the API service. Path `/` (prefix) SHALL route to the Web UI service. Both routes SHALL share the same hostname configured by `ingress.host`.

#### Scenario: API requests reach the API service
- **WHEN** a request is made to `https://<ingress.host>/api/v1/assets`
- **THEN** the request is routed to the API service (no path rewriting needed — the API controllers use `/api/...` routes natively)

#### Scenario: All other requests reach the Web UI
- **WHEN** a request is made to `https://<ingress.host>/households`
- **THEN** the request is routed to the Web UI service which serves `index.html` for client-side routing

---

### Requirement: TLS uses cluster wildcard cert when no secret is specified
The chart's `Ingress` resource SHALL always include a `tls.hosts` entry for `ingress.host`. When `ingress.tlsSecretName` is non-empty, the `secretName` field SHALL be emitted and Traefik will use the named certificate. When `ingress.tlsSecretName` is empty (the default), the `secretName` field SHALL be omitted, causing Traefik to use its configured default certificate store (the cluster wildcard `*.biglernet.com`).

#### Scenario: Cluster wildcard cert used by default
- **WHEN** the chart is installed with `ingress.tlsSecretName` unset
- **THEN** the `Ingress` resource's `tls` block contains `hosts` but no `secretName`, and HTTPS is served using the cluster wildcard cert

#### Scenario: Custom cert secret can be specified
- **WHEN** the chart is installed with `ingress.tlsSecretName=my-tls-secret`
- **THEN** the `Ingress` resource's `tls` block contains `secretName: my-tls-secret`

---

### Requirement: Per-environment values files override defaults for test and prod
The chart SHALL include `values-test.yaml` and `values-prod.yaml` files that override environment-specific settings (hostname, image pull policy, resource limits). The base `values.yaml` SHALL contain safe, non-environment-specific defaults. Deploying with `-f values-prod.yaml` SHALL be the only change required to target the production environment.

#### Scenario: Test environment uses the test hostname
- **WHEN** the chart is deployed with `-f values-test.yaml`
- **THEN** `ingress.host` resolves to `steward-test.biglernet.com`

#### Scenario: Prod environment uses the production hostname
- **WHEN** the chart is deployed with `-f values-prod.yaml`
- **THEN** `ingress.host` resolves to `steward.biglernet.com`
