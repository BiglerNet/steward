### Requirement: Create household
The frontend SHALL provide a "Create household" flow calling `POST /api/households`, after which the new household becomes the active one.

#### Scenario: Creating a first household
- **WHEN** a user with no households submits a household name on the creation flow
- **THEN** the app creates it, makes it the active household, and navigates into the authenticated shell

#### Scenario: Creating an additional household
- **WHEN** an existing user creates another household from the switcher
- **THEN** the new household appears in the switcher and becomes the active selection

---

### Requirement: Household settings — rename
The frontend SHALL provide a settings page for Owners/Contributors to rename the active household via `PUT /api/households/{id}`, respecting the API's authorization response.

#### Scenario: Owner renames household
- **WHEN** an Owner submits a new name on the household settings page
- **THEN** the app calls `PUT /api/households/{id}` and reflects the new name in the switcher and layout header

#### Scenario: Viewer attempts to access settings
- **WHEN** a Viewer-role user navigates to the household settings page
- **THEN** the app either hides the edit controls or shows a disabled state, consistent with the API's `403` response if attempted

---

### Requirement: Member list
The frontend SHALL display the active household's members and roles via `GET /api/households/{id}/members`.

#### Scenario: Viewing members
- **WHEN** any household member opens the settings page
- **THEN** the app lists all members with their display name/email and role

---

### Requirement: Invite member by email
The frontend SHALL allow Owners/Contributors to invite a new member by email via `POST /api/households/{id}/members/invite`, and show pending invitations with the ability to revoke them via `DELETE /api/households/{id}/invitations/{code}`.

#### Scenario: Sending an invite
- **WHEN** an Owner/Contributor submits an email and role on the invite form
- **THEN** the app calls the invite endpoint and the new pending invite appears in the list

#### Scenario: Revoking a pending invite
- **WHEN** an Owner/Contributor revokes a pending invitation
- **THEN** the app calls the revoke endpoint and removes it from the list

---

### Requirement: Remove member
The frontend SHALL allow Owners/Contributors to remove a member from the household via `DELETE /api/households/{id}/members/{userId}`.

#### Scenario: Removing a member
- **WHEN** an Owner/Contributor removes another member from the list
- **THEN** the app calls the removal endpoint and the member disappears from the list

#### Scenario: Removal forbidden
- **WHEN** the API rejects a removal attempt (e.g. insufficient role, or attempting to remove the last Owner)
- **THEN** the app surfaces the error via the global toast pattern and the member remains in the list

---

### Requirement: Household location settings
The frontend SHALL let Owners set or clear the household's `country` and `region` on the household settings page via `PUT /api/households/{id}`. The selectors SHALL be populated from `GET /api/regions` through a `useRegionRegistry()` TanStack Query hook fetched once per session (`staleTime: Infinity`, mirroring the asset-type registry hook); the region selector SHALL list only regions of the selected country and SHALL clear when the country changes. Non-Owner roles SHALL see the location read-only or hidden, consistent with the API's authorization.

#### Scenario: Owner sets the household location
- **WHEN** an Owner selects "United States" and "Wisconsin" on the settings page and saves
- **THEN** the app submits `country: "US"`, `region: "US-WI"` via `PUT /api/households/{id}` and reflects the saved location

#### Scenario: Changing country resets region
- **WHEN** an Owner switches the country from "United States" to "Canada"
- **THEN** the region selector clears and offers only Canadian provinces and territories

#### Scenario: Region registry fetched once per session
- **WHEN** a user visits household settings and registration forms repeatedly in one session
- **THEN** `GET /api/regions` is called at most once

---

### Requirement: Storage usage display
The frontend SHALL show the household's storage consumption on the household settings page — used bytes against the effective quota from the household detail response, rendered as a human-readable summary (e.g. "412 MB of 1 GB") with a progress indicator. The display SHALL be visible to all members and SHALL offer no controls to change the quota.

#### Scenario: Member sees storage usage
- **WHEN** any household member opens the household settings page
- **THEN** the current usage and effective quota are shown with a progress indicator

#### Scenario: Near-full households are highlighted
- **WHEN** usage exceeds 90% of the effective quota
- **THEN** the progress indicator switches to a warning treatment
