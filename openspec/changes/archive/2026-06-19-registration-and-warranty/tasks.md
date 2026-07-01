## 1. Domain — Registration Renewal History

- [x] 1.1 [Domain] Add `Cost` (`decimal?`) and `RenewedOn` (`DateOnly?`) properties to the `Registration` entity.
- [x] 1.2 [Infrastructure] Update `RegistrationConfiguration` to map the new columns and add `builder.HasIndex(r => r.ExpiresOn)` to support future reminder queries (design.md Goals).
- [x] 1.3 [Infrastructure] Generate EF Core migration `AddRegistrationCostAndRenewedOn` adding the `Cost`/`RenewedOn` columns and `ExpiresOn` index to the `Registrations` table; apply it against the local docker-compose PostgreSQL instance and verify via `psql \d`.

## 2. Application — Storage Abstraction

- [x] 2.1 [Application] Define `Steward.Application.Storage.IFileStorageService` with `SaveAsync(Stream content, string contentType, string entityType, Guid entityId, CancellationToken) → string storageKey`, `OpenReadAsync(string storageKey, CancellationToken) → (Stream, string contentType)`, `DeleteAsync(string storageKey, CancellationToken)`.
- [x] 2.2 [Application] Define `FileUploadOptions` (allowed content types, max size bytes) bound from `Storage:MaxUploadSizeBytes` configuration, used by both controllers' upload validation.

## 3. Application — Registrations

- [x] 3.1 [Application] Create `Steward.Application.Tracking.Registrations` namespace with `RegistrationResponse` (includes `registrationNumber`, `issuingAuthority`, `renewedOn`, `cost`, `expiresOn`, `notes`, `hasDocument: bool`, `documentUrl: string?` pointing at the download endpoint), `CreateRegistrationRequest`, `UpdateRegistrationRequest`.
- [x] 3.2 [Application] Create `CreateRegistrationRequestValidator`/`UpdateRegistrationRequestValidator` — `registrationNumber` required, `cost` non-negative when present.
- [x] 3.3 [Application] Define `IRegistrationService` with `CreateAsync`, `ListAsync` (ordered by `expiresOn` descending), `UpdateAsync`, `DeleteAsync`, `SetDocumentAsync(registrationId, storageKey)`, `RemoveDocumentAsync(registrationId)`, `GetDocumentStorageKeyAsync(registrationId)` — all scoped by `assetId`.

## 4. Application — Warranties

- [x] 4.1 [Application] Create `Steward.Application.Tracking.Warranties` namespace with `WarrantyResponse`, `CreateWarrantyRequest`, `UpdateWarrantyRequest` (same `hasDocument`/`documentUrl` shape as Registration).
- [x] 4.2 [Application] Create `CreateWarrantyRequestValidator`/`UpdateWarrantyRequestValidator` — `provider` required.
- [x] 4.3 [Application] Define `IWarrantyService` with `CreateAsync`, `ListAsync`, `UpdateAsync`, `DeleteAsync`, `SetDocumentAsync`, `RemoveDocumentAsync`, `GetDocumentStorageKeyAsync` — scoped by `assetId`.

## 5. Infrastructure — Storage Implementation

- [x] 5.1 [Infrastructure] Implement `LocalFileStorageService : IFileStorageService` resolving keys under a configured `Storage:RootPath`, generating keys as `{entityType}/{entityId}/{guid}{extension}`, creating directories as needed.
- [x] 5.2 [Infrastructure] Add `Storage:RootPath` and `Storage:MaxUploadSizeBytes` to `appsettings.json`/`appsettings.Development.json`; add a local bind-mount volume to `docker-compose.yml` for dev parity with the planned NFS-backed Kubernetes mount. (No API container exists yet in docker-compose.yml — only Postgres is dockerized for dev; `RootPath` is a local relative folder used when running the API directly. A real bind mount needs an API service/Dockerfile, which is out of scope here.)
- [x] 5.3 [Infrastructure] Register `IFileStorageService` → `LocalFileStorageService` in DI.

## 6. Infrastructure — Service Implementations

- [x] 6.1 [Infrastructure] Implement `RegistrationService : IRegistrationService` and `WarrantyService : IWarrantyService`; `SetDocumentAsync` saves the file via `IFileStorageService` first, then updates the entity's `DocumentUrl` (storage key) — in that order, per design.md's orphaned-file risk mitigation — and deletes the old file (if any) only after the new key is committed.
- [x] 6.2 [Infrastructure] Ensure `DeleteAsync` on both services deletes the attached file (if any) via `IFileStorageService` before removing the database row.
- [x] 6.3 [Infrastructure] Register `IRegistrationService`/`IWarrantyService` in DI alongside the existing tracking service registrations.

## 7. Api — Controllers

- [x] 7.1 [Api] Create `RegistrationsController` at `api/households/{householdId}/assets/{assetId}/registrations` with Create/List/Update/Delete mirroring the tracking-controller pattern (Delete requires `HouseholdOperations.Edit`, not `Delete`, per design.md Decision 5).
- [x] 7.2 [Api] Create `WarrantiesController` at `api/households/{householdId}/assets/{assetId}/warranties` following the same pattern.
- [x] 7.3 [Api] Add `POST/GET/DELETE .../document` nested actions to both controllers: upload validates content-type/size against `FileUploadOptions` before calling the service, download streams via `OpenReadAsync` with the stored content-type, delete calls `RemoveDocumentAsync`.
- [x] 7.4 [Api] Map `RegistrationResponse.documentUrl`/`WarrantyResponse.documentUrl` to the controller's own download route (not the storage key) when `hasDocument` is true; `null` otherwise.

## 8. Tests

- [x] 8.1 [IntegrationTests] CRUD happy-path tests for Registration and Warranty, verifying role enforcement (Viewer 403 on write, Contributor can delete).
- [x] 8.2 [IntegrationTests] Registration history tests — creating multiple renewal records for one asset preserves all of them; list endpoint returns them ordered by `expiresOn` descending; deleting one record doesn't affect the others.
- [x] 8.3 [IntegrationTests] Document upload/download/delete round-trip test — upload a PDF, download it back and verify bytes/content-type match, delete it, verify subsequent download returns 404.
- [x] 8.4 [IntegrationTests] Upload rejection tests — unsupported content type and oversized file both return HTTP 400.
- [x] 8.5 [IntegrationTests] Re-upload replaces the prior document — verify only the latest file is retrievable and the old file is removed from disk.
- [x] 8.6 [IntegrationTests] Cross-household isolation test — a registration/warranty under Household A's asset returns 404 when queried via Household B's route.
- [x] 8.7 [UnitTests] `LocalFileStorageService` unit tests for key generation, save/read/delete round-trip against a temp directory.
- [x] 8.8 [UnitTests] `CreateRegistrationRequestValidator`/`UpdateRegistrationRequestValidator` rule tests for `cost` non-negative constraint.
