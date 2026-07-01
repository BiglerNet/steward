## Context

Change 1 established the project skeleton, all domain entities, and EF Core infrastructure. The database schema is complete. This change adds the first runnable API surface: authentication endpoints, household management, and the membership/invite system. All subsequent feature changes depend on this layer being correct and the authorization pipeline being active.

## Goals / Non-Goals

**Goals:**
- Email/password registration and login with JWT issuance
- OAuth social login (Google, Facebook, Apple) with SPA-safe callback flow
- Household CRUD with ownership rules
- Membership invite, accept, and revoke flow (no email delivery yet)
- Resource-based authorization active and enforced on all household endpoints
- PlatformAdmin role seeded on startup

**Non-Goals:**
- Password reset and email verification — deferred to a dedicated auth-flows change
- Refresh token backend — deferred; short-lived access tokens (15 min) are the only credential for now
- Email delivery for invites — invite codes are exchanged out-of-band until the email service is added
- Household public view — Change 7
- Asset-level endpoints — Change 3
- Household transfer of ownership — not in scope

## Decisions

### 1. OAuth callback flow — exchange code pattern

**Problem:** OAuth providers redirect back to the API with an authorization code. For an SPA frontend, the API needs to hand off a JWT without putting it in the URL (where it leaks into browser history and referrer headers).

**Decision:** Use a short-lived server-side exchange code:

```
  Frontend                   API                      Provider (Google, etc.)
  ────────                   ───                      ───────────────────────
  GET /api/auth/oauth/       ──redirect──────────────▶ Authorization endpoint
    google/login
                             ◀──callback with code──── Provider redirects back
  ◀──redirect to──           GET /api/auth/oauth/
    /auth/callback             google/callback
    ?code=<exchange-code>       1. Exchange code with provider
                                2. Get user info
                                3. Find or create ApplicationUser
                                4. Store random exchange-code → userId
                                   in IMemoryCache (TTL: 60s)
                                5. Redirect to frontend

  POST /api/auth/oauth/      ──lookup exchange-code──▶ IMemoryCache
    exchange                 ◀──userId────────────────
    { code }                   Issue JWT, return AuthResponse
```

**Rationale:** JWT never touches the URL. Exchange code is single-use, 60-second TTL. IMemoryCache is sufficient since exchange codes are ephemeral and single-instance (distributed cache can be swapped in later if needed).

**Alternative considered:** Redirect with JWT in URL fragment — simpler but leaks the token into browser history. Rejected.

---

### 2. Household delete — reject if assets exist

**Decision:** `DELETE /api/households/{id}` returns HTTP 409 if the household has any associated assets. The owner must delete all assets before deleting the household.

**Rationale:** Silent cascade-deletion of all asset history (service records, fuel logs, registrations) would be catastrophic and unrecoverable. Explicit user intent required.

**Alternative considered:** Soft delete (`DeletedAt` column) — adds complexity everywhere (query filters). Not warranted for the first pass.

---

### 3. Invite flow — invite code on the membership record

**Decision:** When an owner invites a user by email, a `HouseholdMembership` record is created with `Status = Pending` and a random `InviteCode` (Guid, stored on the membership row). The invitee presents this code to `POST /api/auth/invites/{code}/accept` while authenticated to link the membership to their account.

```
  Owner                 API                    Invitee
  ─────                 ───                    ───────
  POST /households/     Create Pending
    {id}/members/       membership record
    invite              InviteCode = <guid>
  { email, role }       (returned in response)

  [shares code          ──────────────────────▶ POST /auth/invites/
   out of band]                                   {code}/accept
                        Link membership to
                        authenticated user;
                        Status = Active
```

**Rationale:** No email service dependency. When email delivery is added (future change), it just sends the existing `InviteCode` in the email body — no model changes required.

---

### 4. PlatformAdmin seeding — startup hosted service

**Decision:** Seed the `PlatformAdmin` Identity role using `WebApplication.Services.CreateScope()` in `Program.cs` after `app.Build()`, before `app.Run()`. Uses `RoleManager<IdentityRole>` to create the role if absent.

**Rationale:** Idempotent on every startup. No migration dependency. Consistent with my-marina patterns.

---

### 5. IDistributedCache vs IMemoryCache for exchange codes

**Decision:** Use `IMemoryCache` (in-process) for OAuth exchange codes in this change. Register `services.AddMemoryCache()`.

**Rationale:** Exchange codes are ephemeral (60s TTL), single-use, and don't need to survive restarts. IMemoryCache is zero-dependency. When the app scales to multiple instances, swap to `IDistributedCache` backed by Redis — the `IOAuthExchangeService` interface abstracts this away.

### 6. Pre-registration invites — separate HouseholdInvitations table

**Problem:** An owner should be able to invite someone who hasn't registered yet — a primary growth mechanism. The previous design created a `HouseholdMembership` immediately with a nullable `UserId`, which is semantically wrong (a membership without a member).

**Decision:** Introduce a separate `HouseholdInvitation` entity. A `HouseholdMembership` is only created when the invite is accepted. The invitation has its own lifecycle independent of membership.

```
HouseholdInvitation
  Id (Guid), HouseholdId (FK), InvitedByUserId (FK)
  Email (string)               ← the invited address, pre-registration safe
  Role (Contributor|Viewer)    ← Owner not invitable
  InviteCode (string, unique)  ← random Guid, URL-safe
  ExpiresAt (DateTimeOffset)   ← default: 7 days from creation
  Status (Pending|Accepted|Revoked|Expired)
  AcceptedByUserId (FK, null)  ← set on accept
  AcceptedAt (DateTimeOffset, null)
  CreatedAt (DateTimeOffset)

Lifecycle:
  Owner invites email ──▶ HouseholdInvitation (Pending)
  Invitee registers   ──▶ Registration checks email → surfaces pending invites
  Invitee accepts     ──▶ HouseholdMembership created, Invitation → Accepted
  No action taken     ──▶ Invitation → Expired (after ExpiresAt)
  Owner cancels       ──▶ Invitation → Revoked
```

**Expiry cleanup:** Filter-on-read (`Status = Pending AND ExpiresAt > NOW()`) is sufficient for correctness. A lightweight `IHostedService` runs at startup and every 24 hours to flip stale Pending invitations to Expired — no Hangfire dependency needed.

**At registration:** `AuthService.RegisterAsync` checks `HouseholdInvitations` for any Pending, non-expired rows matching the new user's email and returns them in the `AuthResponse` as `pendingInvites: [{ inviteCode, householdName, role }]`. The frontend then prompts acceptance before routing the user.

**Rationale:** Clean lifecycle separation. `HouseholdMembership` always has a real `UserId`. Invitation expiry prevents unbounded accumulation of stale records without complex infrastructure. Matching on email at registration is safe since email is verified by Identity.

---

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| No refresh tokens — users re-authenticate every 15 min | Acceptable for early development; refresh tokens are a planned follow-up change |
| Invite code shared out-of-band (no email) | Acceptable until email service change; InviteCode is on the membership record and returned to the owner in the API response |
| IMemoryCache for exchange codes breaks with multiple API instances | `IOAuthExchangeService` interface — swap to Redis-backed implementation without API changes |
| Household delete requires manual asset cleanup | By design; prevents accidental data loss |

## Resolved Questions

### Registration does not auto-create a household

**Decision:** `POST /api/auth/register` creates only the `ApplicationUser`. No household is created automatically. The frontend routes the user into one of two flows based on context:

**Flow 1 — New user starting fresh:**
```
Register → POST /api/households (create "My Garage") → dashboard
```
The frontend presents a "create your garage" step after registration. This user is likely to later invite others.

**Flow 2 — Invited user joining an existing household:**
```
Register → POST /api/auth/invites/{code}/accept → household dashboard
```
The frontend detects a pending `inviteCode` in the URL or session (passed when the owner shared the link) and skips household creation entirely. The user lands directly in the shared household. They can always create their own household later.

**Rationale:** No assumption about intent. Both flows compose cleanly from the same API primitives. The frontend (not the backend) owns the onboarding routing decision.

## Open Questions

*(none — all design questions resolved)*
