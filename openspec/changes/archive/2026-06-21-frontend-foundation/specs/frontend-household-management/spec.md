## ADDED Requirements

### Requirement: Create household
The frontend SHALL provide a "Create household" flow calling `POST /api/households`, after which the new household becomes the active one.

#### Scenario: Creating a first household
- **WHEN** a user with no households submits a household name on the creation flow
- **THEN** the app creates it, makes it the active household, and navigates into the authenticated shell

#### Scenario: Creating an additional household
- **WHEN** an existing user creates another household from the switcher
- **THEN** the new household appears in the switcher and becomes the active selection

### Requirement: Household settings — rename
The frontend SHALL provide a settings page for Owners/Contributors to rename the active household via `PUT /api/households/{id}`, respecting the API's authorization response.

#### Scenario: Owner renames household
- **WHEN** an Owner submits a new name on the household settings page
- **THEN** the app calls `PUT /api/households/{id}` and reflects the new name in the switcher and layout header

#### Scenario: Viewer attempts to access settings
- **WHEN** a Viewer-role user navigates to the household settings page
- **THEN** the app either hides the edit controls or shows a disabled state, consistent with the API's `403` response if attempted

### Requirement: Member list
The frontend SHALL display the active household's members and roles via `GET /api/households/{id}/members`.

#### Scenario: Viewing members
- **WHEN** any household member opens the settings page
- **THEN** the app lists all members with their display name/email and role

### Requirement: Invite member by email
The frontend SHALL allow Owners/Contributors to invite a new member by email via `POST /api/households/{id}/members/invite`, and show pending invitations with the ability to revoke them via `DELETE /api/households/{id}/invitations/{code}`.

#### Scenario: Sending an invite
- **WHEN** an Owner/Contributor submits an email and role on the invite form
- **THEN** the app calls the invite endpoint and the new pending invite appears in the list

#### Scenario: Revoking a pending invite
- **WHEN** an Owner/Contributor revokes a pending invitation
- **THEN** the app calls the revoke endpoint and removes it from the list

### Requirement: Remove member
The frontend SHALL allow Owners/Contributors to remove a member from the household via `DELETE /api/households/{id}/members/{userId}`.

#### Scenario: Removing a member
- **WHEN** an Owner/Contributor removes another member from the list
- **THEN** the app calls the removal endpoint and the member disappears from the list

#### Scenario: Removal forbidden
- **WHEN** the API rejects a removal attempt (e.g. insufficient role, or attempting to remove the last Owner)
- **THEN** the app surfaces the error via the global toast pattern and the member remains in the list
