## Why

Registration and Warranty are the last two cross-cutting entities from the original domain model that have no service or controller yet. They also introduce the project's first real file upload (registration cards, warranty receipts as PDFs/photos), which every prior change has deferred. This change resolves that deferral with a filesystem-backed storage abstraction suited to the project's planned self-hosted Kubernetes deployment (NFS-mounted volume), designed so a future move to an S3-compatible provider (e.g. Cloudflare R2, if the project scales) is a swap of one implementation, not a rewrite.

## What Changes

- Add `Cost` and `RenewedOn` properties to the `Registration` entity, turning it into an append-only renewal-history record (one row per registration/renewal cycle: when it was renewed, what it cost, when it's next due) instead of a single mutable "current registration" row — matching the event-log pattern already used for `ServiceRecord`/`MileageLog`/`FuelLog`. Requires a new EF Core migration (the only entity change in this batch that does).
- Add CRUD endpoints for `Registration` (`/api/households/{householdId}/assets/{assetId}/registrations`) — number, issuing authority, renewed-on date, cost, expiry (due date), optional document attachment. List results are ordered by `expiresOn` descending so the most recent renewal surfaces first.
- Add CRUD endpoints for `Warranty` (`/api/households/{householdId}/assets/{assetId}/warranties`) — provider, description, start/expiry dates, optional document attachment.
- Add a document upload/download capability: `POST .../registrations/{id}/document` and `POST .../warranties/{id}/document` accept a single file upload, store it via a new `IFileStorageService` abstraction, and replace `DocumentUrl` with a server-generated reference; `GET .../document` streams the file back. A `DELETE .../document` removes the attachment without deleting the parent record.
- Implement `IFileStorageService` with a `LocalFileStorageService` backing it by a configured root directory (intended to be an NFS mount in the Kubernetes deployment), keeping the interface storage-agnostic so an S3/R2 implementation can be added later without touching controllers or services.
- Enforce the existing household role capability matrix: Viewer can read/download, Contributor/Owner can create/edit/upload/delete (matching the tracking-record precedent of Contributor-level delete, since correcting a registration/warranty entry is routine data maintenance, not a structural household decision).
- **Non-goal, explicitly noted for future design**: renewal/expiry reminder notifications (e.g. "Registration due in 14 days for your Mazda 3") are out of scope for this change, but `ExpiresOn` is indexed on both `Registration` and `Warranty` specifically so a future notifications change can query "what's expiring soon across all households" without a schema change.

## Capabilities

### New Capabilities
- `registration-tracking`: Household-scoped CRUD for vehicle/asset registration records, including document attachment.
- `warranty-tracking`: Household-scoped CRUD for warranty records, including document attachment.
- `document-storage`: File upload/download/delete for attachments on Registration and Warranty records, backed by a swappable storage abstraction.

### Modified Capabilities
- (none)

## Impact

- **Domain**: `Registration` and `Warranty` already exist from `core-solution-structure`; no new entities, but `Registration` gains `Cost` (`decimal?`) and `RenewedOn` (`DateOnly?`) properties to support renewal history. `DocumentUrl` on both is repurposed as an opaque storage key set by the server, not a client-supplied URL.
- **Application**: New `Steward.Application.Tracking.Registrations` and `...Warranties` namespaces (DTOs, validators, `IRegistrationService`/`IWarrantyService`); new `Steward.Application.Storage.IFileStorageService` abstraction.
- **Infrastructure**: New `RegistrationService`/`WarrantyService` implementations; new `LocalFileStorageService` implementation reading a configured root path from settings (`Storage:RootPath`), with content-type/size validation at the upload boundary.
- **Api**: New `RegistrationsController`, `WarrantiesController`, both exposing nested document upload/download/delete actions; `[Authorize]` + resource-based household authorization via the existing `GetHouseholdIdForAssetAsync` pattern.
- **Configuration**: New `Storage:RootPath` setting in `appsettings.json`; the docker-compose/Kubernetes deployment mounts this path to a volume (NFS share in the planned k8s deployment, a local bind mount for docker-compose dev).
- **Dependencies**: None new for storage (filesystem I/O is built into .NET); reuses `HouseholdOperations`/`HouseholdResource` authorization.
