## REMOVED Requirements

### Requirement: Create service record
**Reason**: `ServiceRecord` is replaced by `MaintenanceItem`, which supports the same "log a completed service" use case plus a planning lifecycle, checklists, and parts tracking.
**Migration**: Use `POST /api/households/{householdId}/assets/{assetId}/maintenance-items` (see the `maintenance-items` capability) with `status: "Done"` to reproduce the old one-step "log a completed service" behavior.

### Requirement: List service records for an asset
**Reason**: Superseded by the `maintenance-items` capability's list endpoint.
**Migration**: Use `GET /api/households/{householdId}/assets/{assetId}/maintenance-items`.

### Requirement: Update service record
**Reason**: Superseded by the `maintenance-items` capability's granular update endpoints.
**Migration**: Use the field-level `PATCH` endpoints under `maintenance-items`.

### Requirement: Delete service record
**Reason**: Superseded by the `maintenance-items` capability's delete endpoint.
**Migration**: Use `DELETE /api/households/{householdId}/assets/{assetId}/maintenance-items/{id}`.
