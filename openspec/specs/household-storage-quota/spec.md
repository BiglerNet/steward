# household-storage-quota Specification

## Purpose
TBD - created by archiving change asset-photos. Update Purpose after archive.
## Requirements
### Requirement: Household storage usage accounting
The system SHALL maintain `Household.StorageUsedBytes` as a transactional counter of all bytes stored for the household: asset photo variants and registration/warranty document attachments. Every file write SHALL increment the counter by the stored size (for photos, the sum of both variants after processing — not the upload size), every delete SHALL decrement it, and a document replace SHALL adjust by the difference, all within the same database transaction as the owning entity change. All file-writing services SHALL route through a shared storage-quota service; no write path may bypass it.

#### Scenario: Photo upload increases usage by stored bytes
- **WHEN** a Contributor uploads a 12 MB photo whose stored variants total 900 KB
- **THEN** the household's `storageUsedBytes` increases by 900 KB (not 12 MB)

#### Scenario: Deletion returns capacity
- **WHEN** a photo or an attached document is deleted
- **THEN** `storageUsedBytes` decreases by exactly the bytes that were stored for it

#### Scenario: Document replacement adjusts by the difference
- **WHEN** a Contributor replaces a 2 MB registration document with a 1 MB one
- **THEN** `storageUsedBytes` decreases by 1 MB

### Requirement: Quota enforcement on upload
The system SHALL enforce an effective storage quota per household: `StorageQuotaOverrideBytes` when set, otherwise the configured default (`Storage:HouseholdQuotaBytes`, default 1 GB). Any photo or document upload that would push `StorageUsedBytes` past the effective quota SHALL be rejected with HTTP 400 and a message identifying the quota as the cause, storing nothing.

#### Scenario: Upload over quota rejected
- **WHEN** a household's usage is within 100 KB of its effective quota and a Contributor uploads a file storing more than 100 KB
- **THEN** HTTP 400 is returned with a quota-exceeded message, and usage is unchanged

#### Scenario: Quota applies to documents too
- **WHEN** a household at its quota ceiling uploads a registration document
- **THEN** HTTP 400 is returned with the same quota-exceeded semantics

### Requirement: Storage usage visible to household members
`GET /api/households/{id}` SHALL include `storageUsedBytes` and the effective `storageQuotaBytes` so members can see consumption; whether the quota comes from the default or an override SHALL NOT be distinguishable in the member-facing payload.

#### Scenario: Member sees usage and quota
- **WHEN** an Active member fetches their household detail
- **THEN** the response includes current `storageUsedBytes` and the effective `storageQuotaBytes`

### Requirement: PlatformAdmin storage quota override
The system SHALL provide `PUT /api/admin/households/{householdId}/storage-quota` (PlatformAdmin role only) accepting `{ quotaBytes: number | null }`, where a value sets the household's override and null clears it (reverting to the configured default). Non-positive values SHALL return HTTP 400.

#### Scenario: PlatformAdmin raises a household's quota
- **WHEN** a PlatformAdmin PUTs `{ quotaBytes: 5368709120 }` for a household
- **THEN** HTTP 200 is returned and the household's effective quota becomes 5 GB

#### Scenario: Clearing the override restores the default
- **WHEN** a PlatformAdmin PUTs `{ quotaBytes: null }` for a household with an override
- **THEN** HTTP 200 is returned and the effective quota reverts to the configured default

#### Scenario: Household Owner cannot change quotas
- **WHEN** a household Owner (without the PlatformAdmin role) calls the storage-quota endpoint
- **THEN** HTTP 403 is returned
