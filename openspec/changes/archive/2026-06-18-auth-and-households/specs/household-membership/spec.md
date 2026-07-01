## ADDED Requirements

### Requirement: Invite member by email (pre-registration safe)
The system SHALL provide `POST /api/households/{id}/members/invite` (Owner only) accepting `{ email, role }` where `role` is `Contributor` or `Viewer`. The endpoint SHALL create a `HouseholdInvitation` with `Status = Pending`, a unique `InviteCode`, and `ExpiresAt` set to 7 days from now. The invited user does NOT need to have an existing account. The response SHALL include the `inviteCode` so the owner can share it. If a Pending, non-expired invitation already exists for that email in this household, HTTP 409 SHALL be returned. Owner role SHALL NOT be assignable via invite.

#### Scenario: Owner invites an unregistered email
- **WHEN** an Owner POSTs to `/api/households/{id}/members/invite` with an email that has no account
- **THEN** HTTP 201 is returned with a `HouseholdInvitation` response including `inviteCode` and `expiresAt`; no `ApplicationUser` or `HouseholdMembership` is created

#### Scenario: Owner invites an existing user
- **WHEN** an Owner invites an email that belongs to an existing `ApplicationUser`
- **THEN** the same behavior — a `HouseholdInvitation` is created, not a membership; the user sees it on next login via `pendingInvites`

#### Scenario: Duplicate pending invite rejected
- **WHEN** a Pending, non-expired invitation for that email already exists in the household
- **THEN** HTTP 409 is returned

#### Scenario: Owner role invite rejected
- **WHEN** an Owner attempts to invite with `role = Owner`
- **THEN** HTTP 400 is returned

#### Scenario: Contributor cannot invite
- **WHEN** a Contributor calls the invite endpoint
- **THEN** HTTP 403 is returned

---

### Requirement: Pending invites surfaced at registration
When a new user registers, the system SHALL check `HouseholdInvitations` for any Pending, non-expired rows matching the registered email address. If any exist, their summary (`inviteCode`, `householdName`, `role`, `expiresAt`) SHALL be included in the `AuthResponse` as `pendingInvites`. The frontend uses this to prompt acceptance before routing the user.

#### Scenario: New user registers with pending invites
- **WHEN** a user registers with an email that has two pending household invitations
- **THEN** the `AuthResponse` includes `pendingInvites` with two entries containing `inviteCode`, `householdName`, and `role`

#### Scenario: New user with no pending invites
- **WHEN** a user registers with an email that has no pending invitations
- **THEN** `pendingInvites` in `AuthResponse` is an empty array

---

### Requirement: Accept membership invite
The system SHALL provide `POST /api/auth/invites/{code}/accept` (requires authentication). The endpoint SHALL look up the `HouseholdInvitation` by `InviteCode` where `Status = Pending` and `ExpiresAt > NOW()`. On match it SHALL create a `HouseholdMembership` with the authenticated user's ID and the invitation's role, set `AcceptedByUserId` and `AcceptedAt` on the invitation, and set `Status = Accepted`. If the code is not found, expired, or not Pending, HTTP 400 SHALL be returned. If the user already has an Active membership in that household, HTTP 409 SHALL be returned.

#### Scenario: Authenticated user accepts valid invite
- **WHEN** an authenticated user POSTs to `/api/auth/invites/{code}/accept` with a valid, non-expired code
- **THEN** HTTP 200 is returned, a `HouseholdMembership` is created with the user's ID and the invite's role, and the invitation `Status` is set to `Accepted`

#### Scenario: Expired invite rejected
- **WHEN** a user attempts to accept a code whose `ExpiresAt` is in the past
- **THEN** HTTP 400 is returned

#### Scenario: Already-accepted code rejected
- **WHEN** the same invite code is submitted a second time
- **THEN** HTTP 400 is returned (invitation is no longer Pending)

#### Scenario: User already a member
- **WHEN** a user who already has an Active membership in that household accepts an invite
- **THEN** HTTP 409 is returned

---

### Requirement: Invitation expiry
The system SHALL consider a `HouseholdInvitation` expired when `ExpiresAt < NOW()` regardless of its `Status` field. A background `IHostedService` SHALL run at startup and every 24 hours to set `Status = Expired` on all Pending invitations whose `ExpiresAt` has passed. All queries for Pending invitations SHALL additionally filter `ExpiresAt > NOW()` as a safety net.

#### Scenario: Expired invitation not returned in pending list
- **WHEN** a user's email has a `HouseholdInvitation` with `Status = Pending` but `ExpiresAt` in the past
- **THEN** it is NOT included in `pendingInvites` in the `AuthResponse`

#### Scenario: Cleanup service marks stale invitations
- **WHEN** the expiry `IHostedService` runs
- **THEN** all Pending invitations with `ExpiresAt < NOW()` have their `Status` set to `Expired`

---

### Requirement: Revoke invitation
The system SHALL provide `DELETE /api/households/{id}/invitations/{inviteCode}` (Owner only) that sets the `HouseholdInvitation.Status = Revoked`. Only Pending invitations may be revoked; attempting to revoke an Accepted or already Expired invitation SHALL return HTTP 400.

#### Scenario: Owner revokes a pending invitation
- **WHEN** an Owner calls the revoke endpoint for a Pending invitation
- **THEN** HTTP 204 is returned and the invitation `Status` is `Revoked`

#### Scenario: Revoking non-pending invitation rejected
- **WHEN** an Owner attempts to revoke an invitation that is Accepted or Expired
- **THEN** HTTP 400 is returned

---

### Requirement: List household members
The system SHALL provide `GET /api/households/{id}/members` (any Active member or PlatformAdmin) returning all Active `HouseholdMembership` records with user display info, plus all Pending (non-expired) `HouseholdInvitation` records — so owners can see outstanding invites alongside confirmed members.

#### Scenario: Member lists household members and pending invites
- **WHEN** a user with any Active role calls `GET /api/households/{id}/members`
- **THEN** HTTP 200 is returned with `members` (Active memberships) and `pendingInvites` (non-expired Pending invitations) as separate arrays

---

### Requirement: Revoke membership
The system SHALL provide `DELETE /api/households/{id}/members/{userId}` (Owner only) that sets the target `HouseholdMembership.Status = Revoked`. An Owner SHALL NOT be able to revoke their own membership. Revocation takes effect immediately — subsequent requests from the revoked user return HTTP 403.

#### Scenario: Owner revokes a Contributor
- **WHEN** an Owner calls `DELETE /api/households/{id}/members/{userId}` for a Contributor
- **THEN** HTTP 204 is returned and the membership status is Revoked

#### Scenario: Owner cannot revoke themselves
- **WHEN** an Owner calls the revoke endpoint targeting their own userId
- **THEN** HTTP 400 is returned

#### Scenario: Revoked member loses access immediately
- **WHEN** a member's status is set to Revoked
- **THEN** subsequent requests to household-scoped endpoints from that user return HTTP 403
