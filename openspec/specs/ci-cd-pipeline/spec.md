# ci-cd-pipeline Specification

## Purpose
Defines the GitHub Actions CI/CD workflow that builds, tests, and deploys the application on push.

## Requirements
### Requirement: CI workflow builds and tests on every push
The system SHALL provide a GitHub Actions workflow (`.github/workflows/ci.yml`) that triggers on push to any branch and on pull requests to `main`. It SHALL run `dotnet build`, `dotnet test` (unit tests only — no external database required), `npm ci`, `npm run build`, and `npm test` in sequence. The workflow SHALL fail fast if any step fails.

#### Scenario: CI passes on a green codebase
- **WHEN** a commit is pushed to any branch with no build or test failures
- **THEN** the CI workflow completes successfully with all steps green

#### Scenario: CI fails on a broken build
- **WHEN** a commit is pushed that introduces a compilation error
- **THEN** the CI workflow fails at the build step and subsequent steps do not run

#### Scenario: CI fails on failing unit tests
- **WHEN** a commit is pushed that causes a unit test to fail
- **THEN** the CI workflow fails at the test step

---

### Requirement: Docker images are built and pushed to GHCR on merge to main
The system SHALL provide a GitHub Actions workflow (`.github/workflows/build-push.yml`) that triggers on push to `main` (contingent on CI passing). It SHALL build two Docker images: the API image (build context: repo root, Dockerfile: `src/Steward.Api/Dockerfile`) and the Web UI image (build context: `src/Steward.Web/`, Dockerfile: `src/Steward.Web/Dockerfile`). Both images SHALL be tagged with the full git SHA and pushed to `ghcr.io/biglernet/steward/api` and `ghcr.io/biglernet/steward/web`. The `latest` tag SHALL also be updated on every successful push.

#### Scenario: Both images are pushed with SHA tag on merge to main
- **WHEN** a PR is merged to `main` and CI passes
- **THEN** `ghcr.io/biglernet/steward/api:sha-<full-sha>` and `.../web:sha-<full-sha>` are available in GHCR

#### Scenario: latest tag is updated
- **WHEN** images are pushed after a merge to main
- **THEN** `ghcr.io/biglernet/steward/api:latest` points to the newly built image

---

### Requirement: Test environment is deployed automatically on merge to main
The system SHALL provide a GitHub Actions workflow (`.github/workflows/deploy-test.yml`) that triggers after `build-push.yml` completes successfully on `main`. It SHALL run `helm upgrade --install` targeting the `steward-test` namespace with the SHA-tagged images from the build job.

#### Scenario: Test environment reflects every merge to main
- **WHEN** a PR is merged to `main` and the build-push workflow succeeds
- **THEN** `helm upgrade --install` runs against `steward-test` using the SHA-tagged image, updating the test deployment

#### Scenario: Helm deploy failure blocks the workflow
- **WHEN** the Helm upgrade to test fails (e.g., setup job migration fails)
- **THEN** the deploy-test workflow exits with a non-zero code and the failure is visible in GitHub Actions

---

### Requirement: Production environment is deployed only on GitHub release publish
The system SHALL provide a GitHub Actions workflow (`.github/workflows/deploy-prod.yml`) that triggers on the `release.published` event. It SHALL re-tag the Docker images from the release commit SHA to the release version tag (e.g., `v1.3.0`) and push the versioned tags to GHCR. It SHALL then run `helm upgrade --install` targeting the `steward-prod` namespace using the versioned image tags.

#### Scenario: Prod deploy uses the same image artifact as test
- **WHEN** a GitHub release is published for commit SHA `abc123` at tag `v1.3.0`
- **THEN** the images `ghcr.io/biglernet/steward/api:sha-abc123` and `.../web:sha-abc123` are re-tagged as `:v1.3.0`, and the prod Helm release is upgraded to use `:v1.3.0` — no re-build occurs

#### Scenario: Production is not affected by merges to main
- **WHEN** a PR is merged to `main` with no corresponding GitHub release
- **THEN** only the test environment is updated; the prod environment is unchanged

---

### Requirement: Semver and changelog are managed by release-please
The system SHALL include a release-please configuration (`.github/release-please-config.json` and `.github/release-please-manifest.json`) that watches `main` for conventional commits and automatically maintains `CHANGELOG.md` and creates a Release PR. Merging the Release PR SHALL cause release-please to create a GitHub release with the calculated semver tag, which triggers `deploy-prod.yml`. Changelog sections SHALL be generated from: `feat` (minor bump), `fix` (patch bump), `feat!` or `fix!` (major bump), `chore`, `docs`, `refactor` (no version bump, included in changelog).

#### Scenario: Feature commit creates a Release PR with minor version bump
- **WHEN** a commit with message `feat: add bulk service record import` is merged to `main`
- **THEN** release-please opens or updates a Release PR bumping the minor version and adding the feature to `CHANGELOG.md`

#### Scenario: Merging the Release PR creates a GitHub release and triggers prod deploy
- **WHEN** the release-please Release PR is merged
- **THEN** release-please creates a GitHub release with the calculated semver tag, and `deploy-prod.yml` is triggered by the `release.published` event
